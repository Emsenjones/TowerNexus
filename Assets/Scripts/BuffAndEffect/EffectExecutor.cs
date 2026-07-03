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

        IReadOnlyList<EffectAction> actions = effectDefinition.Actions;

        for (int i = 0; i < actions.Count; i++)
        {
            ExecuteAction(actions[i], triggerContext, ResolvedTargets);
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
                ExecuteDealDamage(triggerContext, targets);
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
        EffectTriggerContext triggerContext,
        IReadOnlyList<MonsterBehaviour> targets)
    {
        int damage = triggerContext.ResolvedDamage;

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

        for (int i = 0; i < targets.Count; i++)
        {
            MonsterBehaviour target = targets[i];

            if (target == null || target.IsDead())
            {
                continue;
            }

            target.ApplyBuff(
                new BuffApplyRequest(
                    buffDefinition,
                    triggerContext.SourceTower,
                    triggerContext.SourceUpgrade,
                    triggerContext.HasTriggerPosition,
                    triggerContext.TriggerPosition)
            );
        }
    }
}
