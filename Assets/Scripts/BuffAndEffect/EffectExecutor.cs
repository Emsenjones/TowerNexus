using System.Collections.Generic;
using UnityEngine;

public static class EffectExecutor
{
    private static readonly List<MonsterBehaviour> ResolvedTargets = new List<MonsterBehaviour>();

    public static void Execute(EffectDefinition effectDefinition, EffectTriggerContext triggerContext)
    {
        ExecuteWithResolvedTargets(effectDefinition, triggerContext, ResolvedTargets);
    }

    public static bool ExecuteWithResolvedTargets(
        EffectDefinition effectDefinition,
        EffectTriggerContext triggerContext,
        List<MonsterBehaviour> resolvedTargets)
    {
        if (effectDefinition == null || resolvedTargets == null)
        {
            return false;
        }

        if (!effectDefinition.IsValid())
        {
            return false;
        }

        if (!EffectTargetResolver.TryResolveTargets(effectDefinition, triggerContext, resolvedTargets))
        {
            return false;
        }

        List<MonsterBehaviour> executionTargets = new List<MonsterBehaviour>(resolvedTargets);

        IReadOnlyList<EffectAction> actions = effectDefinition.Actions;
        bool executedAnyAction = false;

        for (int i = 0; i < actions.Count; i++)
        {
            executedAnyAction |= ExecuteAction(actions[i], triggerContext, executionTargets);
        }

        if (executedAnyAction)
        {
            SpawnExecutionVfx(effectDefinition, triggerContext, executionTargets);
        }

        return true;
    }

    private static bool ExecuteAction(
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
                return ExecuteDealDamage(action, triggerContext, targets);
            case EffectActionType.ApplyBuff:
                return ExecuteApplyBuff(action, triggerContext, targets);
            case EffectActionType.SetMoveSpeedMultiplier:
                return ExecuteSetMoveSpeedMultiplier(action, targets);
            case EffectActionType.ClearMoveSpeedMultiplier:
                return ExecuteClearMoveSpeedMultiplier(targets);
            case EffectActionType.SetMovementLock:
                return ExecuteSetMovementLock(action, targets);
            default:
                Debug.LogWarning($"Effect executor cannot execute unsupported action type '{action.ActionType}'.");
                return false;
        }
    }

    private static bool ExecuteDealDamage(
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        int damage = action != null && action.DamageAmount > 0
            ? action.DamageAmount
            : triggerContext.ResolvedDamage;

        if (damage <= 0)
        {
            return false;
        }

        bool dealtDamage = false;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (target == null || target.IsDead())
            {
                continue;
            }

            target.TakeDamage(damage);
            dealtDamage = true;
        }

        return dealtDamage;
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

            if (target == null || target.IsDead())
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
        IReadOnlyList<MonsterBehaviour> targets)
    {
        bool updatedMoveSpeed = false;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (target != null && !target.IsDead() && target.SetMoveSpeedMultiplier(action.MoveSpeedMultiplier))
            {
                updatedMoveSpeed = true;
            }
        }

        return updatedMoveSpeed;
    }

    private static bool ExecuteClearMoveSpeedMultiplier(IReadOnlyList<MonsterBehaviour> targets)
    {
        bool clearedMoveSpeed = false;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (target == null || target.IsDead())
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
        IReadOnlyList<MonsterBehaviour> targets)
    {
        bool updatedMovementLock = false;

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (target == null || target.IsDead())
            {
                continue;
            }

            target.SetMovementLock(action.IsMovementLocked);
            updatedMovementLock = true;
        }

        return updatedMovementLock;
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

        if (triggerContext.TargetMonster != null)
        {
            position = GetMonsterHitPosition(triggerContext.TargetMonster);
            return true;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (target != null)
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
        Transform hitAnchor = monster.HitAnchor;
        return hitAnchor != null ? hitAnchor.position : monster.transform.position;
    }
}
