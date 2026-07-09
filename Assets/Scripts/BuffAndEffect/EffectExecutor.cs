using System.Collections.Generic;
using UnityEngine;

public static class EffectExecutor
{
    private static readonly List<MonsterBehaviour> ResolvedTargets = new List<MonsterBehaviour>();

    public static void Execute(EffectDefinition effectDefinition, EffectTriggerContext triggerContext)
    {
        if (effectDefinition == null)
        {
            return;
        }

        if (!effectDefinition.IsValid())
        {
            return;
        }

        if (!EffectTargetResolver.TryResolveTargets(effectDefinition, triggerContext, ResolvedTargets))
        {
            return;
        }

        List<MonsterBehaviour> executionTargets = new List<MonsterBehaviour>(ResolvedTargets);

        IReadOnlyList<EffectAction> actions = effectDefinition.Actions;

        for (int i = 0; i < actions.Count; i++)
        {
            ExecuteAction(actions[i], triggerContext, executionTargets);
        }
    }

    private static void ExecuteAction(
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        if (action == null)
        {
            return;
        }

        switch (action.ActionType)
        {
            case EffectActionType.DealDamage:
                ExecuteDealDamage(action, triggerContext, targets);
                break;
            case EffectActionType.ApplyBuff:
                ExecuteApplyBuff(action, triggerContext, targets);
                break;
            default:
                Debug.LogWarning($"Effect executor cannot execute unsupported action type '{action.ActionType}'.");
                break;
        }
    }

    private static void ExecuteDealDamage(
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        int damage = action != null && action.DamageAmount > 0
            ? action.DamageAmount
            : triggerContext.ResolvedDamage;

        if (damage <= 0)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (target == null || target.IsDead())
            {
                continue;
            }

            target.TakeDamage(damage);
        }
    }

    private static void ExecuteApplyBuff(
        EffectAction action,
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        BuffDefinition buffDefinition = action.BuffDefinition;

        if (buffDefinition == null)
        {
            return;
        }

        if (buffDefinition.ElementType != ElementType.None &&
            !triggerContext.CanApplyElementalStack)
        {
            return;
        }

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

            ExecuteBuffApplyFollowUps(outcome, triggerContext);
        }
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

        if (buffInstance.TryEnterProtectionPhase())
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
}
