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
            ExecuteBuffApplyFollowUps(outcome, triggerContext);
        }

        return appliedBuff;
    }

    private static void ExecuteBuffApplyFollowUps(
        BuffApplyOutcome outcome,
        EffectTriggerContext triggerContext)
    {
        MonsterBuffInstance buffInstance = outcome.BuffInstance;
        BuffDefinition buffDefinition = buffInstance != null ? buffInstance.Definition : null;
        MonsterBehaviour owner = buffInstance != null ? buffInstance.Owner : null;

        if (buffDefinition == null || owner == null)
        {
            return;
        }

        if (outcome.Result == BuffApplyResult.Stacked)
        {
            ExecuteBuffFollowUpEffect(
                buffDefinition.GetEffectDefinition(BuffEventType.StackApplied),
                EffectTriggerType.OnHit,
                triggerContext,
                owner);
        }

        if (!outcome.ReachedMaxStacks)
        {
            return;
        }

        ExecuteBuffFollowUpEffect(
            buffDefinition.GetEffectDefinition(BuffEventType.Overload),
            EffectTriggerType.OnMaxStack,
            triggerContext,
            owner);

        if (owner.TryEnterBuffProtection(buffDefinition))
        {
            return;
        }

        owner.RemoveBuff(buffDefinition);
    }

    private static void ExecuteBuffFollowUpEffect(
        EffectDefinition effectDefinition,
        EffectTriggerType triggerType,
        EffectTriggerContext sourceContext,
        MonsterBehaviour owner)
    {
        if (effectDefinition == null || owner == null)
        {
            return;
        }

        Transform hitAnchor = owner.HitAnchor;
        Vector3 triggerPosition = hitAnchor != null ? hitAnchor.position : owner.transform.position;

        Execute(
            effectDefinition,
            new EffectTriggerContext(
                triggerType,
                sourceContext.SourceTower,
                sourceContext.SourceUpgrade,
                owner,
                true,
                triggerPosition,
                0,
                false)
        );
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
