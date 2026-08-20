using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public readonly struct FixedBuffDamageObservation
{
    internal FixedBuffDamageObservation(
        EffectDefinition effectDefinition,
        int actionOrdinal,
        int fixedDamage,
        TowerInstance sourceTower,
        int resolvedTargetCount,
        int successfulApplicationCount)
    {
        EffectDefinition = effectDefinition;
        ActionOrdinal = actionOrdinal;
        FixedDamage = fixedDamage;
        SourceTower = sourceTower;
        ResolvedTargetCount = resolvedTargetCount;
        SuccessfulApplicationCount = successfulApplicationCount;
    }

    public EffectDefinition EffectDefinition { get; }
    public int ActionOrdinal { get; }
    public int FixedDamage { get; }
    public TowerInstance SourceTower { get; }
    public int ResolvedTargetCount { get; }
    public int SuccessfulApplicationCount { get; }
}

public static class EffectExecutor
{
    private static readonly List<MonsterBehaviour> ResolvedTargets = new List<MonsterBehaviour>();

    public static event Action<FixedBuffDamageObservation>
        OnFixedBuffDamageObserved;

    private struct EffectExecutionResult
    {
        public EffectExecutionResult(bool targetsResolved, bool executedAnyAction)
        {
            TargetsResolved = targetsResolved;
            ExecutedAnyAction = executedAnyAction;
        }

        public bool TargetsResolved { get; }
        public bool ExecutedAnyAction { get; }
    }

    public static bool Execute(
        EffectDefinition effectDefinition,
        EffectTriggerContext triggerContext)
    {
        return ExecuteInternal(
            effectDefinition,
            triggerContext,
            ResolvedTargets).ExecutedAnyAction;
    }

    public static bool ExecuteWithResolvedTargets(
        EffectDefinition effectDefinition,
        EffectTriggerContext triggerContext,
        List<MonsterBehaviour> resolvedTargets)
    {
        return ExecuteInternal(
            effectDefinition,
            triggerContext,
            resolvedTargets).ExecutedAnyAction;
    }

    private static EffectExecutionResult ExecuteInternal(
        EffectDefinition effectDefinition,
        EffectTriggerContext triggerContext,
        List<MonsterBehaviour> resolvedTargets)
    {
        if (resolvedTargets == null)
        {
            return default;
        }

        resolvedTargets.Clear();

        if (effectDefinition == null)
        {
            return default;
        }

        if (!effectDefinition.IsValid())
        {
            return default;
        }

        if (!EffectTargetResolver.TryResolveTargets(effectDefinition, triggerContext, resolvedTargets))
        {
            SpawnExecutionVfx(effectDefinition, triggerContext, resolvedTargets);
            return default;
        }

        List<MonsterBehaviour> executionTargets = new List<MonsterBehaviour>(resolvedTargets);

        IReadOnlyList<EffectAction> actions = effectDefinition.Actions;
        bool executedAnyAction = false;

        for (int i = 0; i < actions.Count; i++)
        {
            // Non-short-circuit aggregation preserves authored action order even after a successful action.
            executedAnyAction |= ExecuteAction(
                effectDefinition,
                i,
                actions[i],
                triggerContext,
                executionTargets);
        }

        SpawnExecutionVfx(effectDefinition, triggerContext, executionTargets);

        return new EffectExecutionResult(true, executedAnyAction);
    }

    private static bool ExecuteAction(
        EffectDefinition effectDefinition,
        int actionOrdinal,
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        if (action == null)
        {
            return false;
        }

        switch (action.ActionType)
        {
            case EffectActionType.DealDamage:
                return ExecuteDealDamage(
                    effectDefinition,
                    actionOrdinal,
                    action,
                    triggerContext,
                    targets);
            case EffectActionType.ApplyBuff:
                return ExecuteApplyBuff(action, triggerContext, targets);
            case EffectActionType.SetMoveSpeedMultiplier:
                return ExecuteSetMoveSpeedMultiplier(
                    action,
                    triggerContext,
                    targets);
            case EffectActionType.ClearMoveSpeedMultiplier:
                return ExecuteClearMoveSpeedMultiplier(
                    triggerContext,
                    targets);
            case EffectActionType.SetMovementLock:
                return ExecuteSetMovementLock(
                    action,
                    triggerContext,
                    targets);
            case EffectActionType.ExecuteMultiTargetEffect:
                return ExecuteMultiTargetEffect(action, triggerContext, targets);
            case EffectActionType.SpawnWindVortex:
                return ExecuteSpawnWindVortex(action, triggerContext);
            default:
                Debug.LogWarning($"Effect executor cannot execute unsupported action type '{action.ActionType}'.");
                return false;
        }
    }

    private static bool ExecuteDealDamage(
        EffectDefinition effectDefinition,
        int actionOrdinal,
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        int damage;
        TowerOwnedDamageResolution towerResolution = default;

        switch (action.DamageMode)
        {
            case EffectDamageMode.TowerScaled:
                if (!TowerRuntimeStatResolver.TryResolveTowerOwnedDamage(
                        triggerContext.SourceTower,
                        TowerDamageSourceIdentity.BehaviourEffect(
                            effectDefinition,
                            actionOrdinal),
                        action.DamageScale,
                        out towerResolution))
                {
                    return false;
                }

                damage = towerResolution.FinalDamage;
                break;
            case EffectDamageMode.FixedBuff:
                damage = action.FixedDamage;
                break;
            case EffectDamageMode.None:
            default:
                return false;
        }

        if (damage <= 0)
        {
            return false;
        }

        int successfulApplicationCount = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (!EffectTargetResolver.IsValidMonsterTarget(target))
            {
                continue;
            }

            target.TakeDamage(damage);
            successfulApplicationCount++;
        }

        if (action.DamageMode == EffectDamageMode.TowerScaled)
        {
            TowerRuntimeStatResolver.PublishTowerOwnedDamageApplication(
                towerResolution,
                successfulApplicationCount);
        }
        else
        {
            PublishFixedBuffDamageObservation(
                new FixedBuffDamageObservation(
                    effectDefinition,
                    actionOrdinal,
                    action.FixedDamage,
                    triggerContext.SourceTower,
                    targets.Count,
                    successfulApplicationCount));
        }

        return successfulApplicationCount > 0;
    }

    private static void PublishFixedBuffDamageObservation(
        FixedBuffDamageObservation observation)
    {
        Action<FixedBuffDamageObservation> observers =
            OnFixedBuffDamageObserved;

        if (observers == null)
        {
            return;
        }

        Delegate[] invocationList = observers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<FixedBuffDamageObservation>)invocationList[i])(
                    observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static bool ExecuteApplyBuff(
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        BuffDefinition buffDefinition = action.BuffDefinition;

        if (buffDefinition == null)
        {
            return false;
        }

        if (buffDefinition.ElementType != ElementType.None &&
            !triggerContext.AllowsElementalApplication)
        {
            return false;
        }

        bool appliedBuff = false;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (!EffectTargetResolver.IsValidMonsterTarget(target))
            {
                continue;
            }

            BuffApplyOutcome outcome = target.ApplyBuffWithOutcome(
                new BuffApplyRequest(
                    buffDefinition,
                    triggerContext.SourceTower,
                    triggerContext.SourceUpgrade,
                    triggerContext.HasTriggerPosition,
                    triggerContext.TriggerPosition)
            );

            if (!IsSuccessfulBuffApply(outcome.Result))
            {
                continue;
            }

            appliedBuff = true;
        }

        return appliedBuff;
    }

    private static bool ExecuteSetMoveSpeedMultiplier(
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        bool updatedMoveSpeed = false;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (EffectTargetResolver.IsValidEffectContextTarget(
                    target,
                    triggerContext) &&
                target.SetMoveSpeedMultiplier(action.MoveSpeedMultiplier))
            {
                updatedMoveSpeed = true;
            }
        }

        return updatedMoveSpeed;
    }

    private static bool ExecuteClearMoveSpeedMultiplier(
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        bool clearedMoveSpeed = false;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (!EffectTargetResolver.IsValidEffectContextTarget(
                    target,
                    triggerContext))
            {
                continue;
            }

            target.ClearMoveSpeedMultiplier();
            clearedMoveSpeed = true;
        }

        return clearedMoveSpeed;
    }

    private static bool ExecuteSetMovementLock(
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        bool updatedMovementLock = false;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (!EffectTargetResolver.IsValidEffectContextTarget(
                    target,
                    triggerContext))
            {
                continue;
            }

            target.SetMovementLock(action.IsMovementLocked);
            updatedMovementLock = true;
        }

        return updatedMovementLock;
    }

    private static bool ExecuteMultiTargetEffect(
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        if (action == null || action.MultiTargetEffectDefinition == null || targets == null)
        {
            return false;
        }

        List<MonsterBehaviour> candidateTargets = new List<MonsterBehaviour>();
        List<MonsterBehaviour> childResolvedTargets = new List<MonsterBehaviour>(1);

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (!EffectTargetResolver.IsValidMonsterTarget(target) ||
                (action.ExcludeTriggerContextTarget && target == triggerContext.TargetMonster) ||
                candidateTargets.Contains(target))
            {
                continue;
            }

            candidateTargets.Add(target);
        }

        bool executedAnyEffect = false;
        int remainingExecutionCount = action.TargetCount;

        while (remainingExecutionCount > 0 && candidateTargets.Count > 0)
        {
            int targetIndex = Random.Range(0, candidateTargets.Count);
            MonsterBehaviour target = candidateTargets[targetIndex];
            candidateTargets.RemoveAt(targetIndex);

            if (!EffectTargetResolver.IsValidMonsterTarget(target))
            {
                continue;
            }

            EffectExecutionResult childExecution = ExecuteInternal(
                action.MultiTargetEffectDefinition,
                new EffectTriggerContext(
                    sourceTower: triggerContext.SourceTower,
                    sourceUpgrade: triggerContext.SourceUpgrade,
                    targetMonster: target,
                    hasTriggerPosition: true,
                    triggerPosition: GetMonsterHitPosition(target),
                    allowsElementalApplication: false),
                childResolvedTargets);

            executedAnyEffect |= childExecution.ExecutedAnyAction;
            remainingExecutionCount--;
        }

        return executedAnyEffect;
    }

    private static bool ExecuteSpawnWindVortex(
        EffectAction action,
        EffectTriggerContext triggerContext)
    {
        GameObject windVortexPrefab = action != null ? action.WindVortexPrefab : null;

        if (windVortexPrefab == null ||
            !windVortexPrefab.TryGetComponent(out WindVortexBehaviour windVortexPrefabBehaviour) ||
            !windVortexPrefabBehaviour.IsValid() ||
            !TryGetWindVortexSpawnPosition(triggerContext, out Vector3 spawnPosition))
        {
            return false;
        }

        MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();

        if (monsterManager == null)
        {
            Debug.LogWarning("Effect executor cannot spawn WindVortex: MonsterManager was not found.");
            return false;
        }

        GameObject windVortexObject = Object.Instantiate(windVortexPrefab, spawnPosition, Quaternion.identity);

        if (!windVortexObject.TryGetComponent(out WindVortexBehaviour windVortexBehaviour))
        {
            Debug.LogWarning("Effect executor cannot spawn WindVortex: runtime prefab is missing WindVortexBehaviour.", windVortexObject);
            Object.Destroy(windVortexObject);
            return false;
        }

        windVortexBehaviour.Initialize(
            monsterManager,
            triggerContext.SourceTower,
            triggerContext.SourceUpgrade);

        return windVortexBehaviour.IsInitialized;
    }

    private static bool TryGetWindVortexSpawnPosition(
        EffectTriggerContext triggerContext,
        out Vector3 spawnPosition)
    {
        if (EffectTargetResolver.IsValidMonsterTarget(triggerContext.TargetMonster))
        {
            spawnPosition = triggerContext.TargetMonster.transform.position;
            return true;
        }

        if (triggerContext.HasTriggerPosition)
        {
            spawnPosition = triggerContext.TriggerPosition;
            return true;
        }

        spawnPosition = default;
        return false;
    }

    private static bool IsSuccessfulBuffApply(BuffApplyResult result)
    {
        return result == BuffApplyResult.Applied ||
               result == BuffApplyResult.Refreshed ||
               result == BuffApplyResult.Stacked;
    }

    private static void SpawnExecutionVfx(
        EffectDefinition effectDefinition,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        GameObject vfxPrefab = effectDefinition != null ? effectDefinition.ExecutionVfxPrefab : null;

        if (vfxPrefab == null || !TryGetExecutionVfxPosition(triggerContext, targets, out Vector3 position))
        {
            return;
        }

        Object.Instantiate(vfxPrefab, position, Quaternion.identity);
    }

    private static bool TryGetExecutionVfxPosition(
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets,
        out Vector3 position)
    {
        if (triggerContext.HasTriggerPosition)
        {
            position = triggerContext.TriggerPosition;
            return true;
        }

        if (EffectTargetResolver.IsValidMonsterTarget(triggerContext.TargetMonster))
        {
            position = GetMonsterHitPosition(triggerContext.TargetMonster);
            return true;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (EffectTargetResolver.IsValidMonsterTarget(target))
            {
                position = GetMonsterHitPosition(target);
                return true;
            }
        }

        position = default;
        return false;
    }

    private static Vector3 GetMonsterHitPosition(MonsterBehaviour monster)
    {
        return EffectTargetResolver.GetMonsterHitPosition(monster);
    }
}
