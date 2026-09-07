using System.Collections.Generic;
using UnityEngine;

public static class EffectTargetResolver
{
    public static bool IsValidMonsterTarget(MonsterBehaviour monster)
    {
        return monster != null && monster.IsGameplayTargetable;
    }

    public static bool IsValidEffectContextTarget(
        MonsterBehaviour monster,
        EffectTriggerContext triggerContext)
    {
        return IsValidMonsterTarget(monster) ||
               (triggerContext.AllowsLifecycleOwnerTarget &&
                monster != null &&
                monster == triggerContext.TargetMonster);
    }

    public static Vector3 GetMonsterHitPosition(MonsterBehaviour monster)
    {
        if (monster == null)
        {
            return default;
        }

        Transform hitAnchor = monster.HitAnchor;
        return hitAnchor != null ? hitAnchor.position : monster.transform.position;
    }

    public static int CollectValidTargetsInRadius(
        IReadOnlyList<MonsterBehaviour> aliveMonsters,
        Vector3 center,
        float radius,
        MonsterBehaviour excludedMonster,
        List<MonsterBehaviour> targets)
    {
        if (targets == null)
        {
            return 0;
        }

        targets.Clear();

        if (aliveMonsters == null || radius < 0f)
        {
            return 0;
        }

        float radiusSqr = radius * radius;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!IsValidMonsterTarget(monster) || monster == excludedMonster)
            {
                continue;
            }

            if ((GetMonsterHitPosition(monster) - center).sqrMagnitude > radiusSqr || targets.Contains(monster))
            {
                continue;
            }

            targets.Add(monster);
        }

        return targets.Count;
    }

    public static bool TryResolveTargets(
        EffectDefinition effectDefinition,
        EffectTriggerContext triggerContext,
        List<MonsterBehaviour> targets)
    {
        if (targets == null)
        {
            Debug.LogWarning("Effect target resolver cannot resolve targets: target list is null.");
            return false;
        }

        targets.Clear();

        if (effectDefinition == null)
        {
            Debug.LogWarning("Effect target resolver cannot resolve targets: effect definition is null.");
            return false;
        }

        if (effectDefinition.Radius <= 0f)
        {
            return TryResolveSingleTarget(triggerContext, targets);
        }

        return TryResolveRadiusTargets(effectDefinition, triggerContext, targets);
    }

    private static bool TryResolveSingleTarget(
        EffectTriggerContext triggerContext,
        List<MonsterBehaviour> targets)
    {
        MonsterBehaviour targetMonster = triggerContext.TargetMonster;

        if (!IsValidEffectContextTarget(targetMonster, triggerContext))
        {
            Debug.LogWarning("Effect target resolver cannot resolve single-target effect: target monster is missing or invalid.");
            return false;
        }

        targets.Add(targetMonster);
        return true;
    }

    private static bool TryResolveRadiusTargets(
        EffectDefinition effectDefinition,
        EffectTriggerContext triggerContext,
        List<MonsterBehaviour> targets)
    {
        if (!TryGetRadiusCenter(triggerContext, out Vector3 center))
        {
            Debug.LogWarning("Effect target resolver cannot resolve radius effect: no valid target monster or trigger position was provided.");
            return false;
        }

        MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();

        if (monsterManager == null)
        {
            Debug.LogWarning("Effect target resolver cannot resolve radius effect: monster manager was not found.");
            return false;
        }

        return CollectValidTargetsInRadius(
                   monsterManager.GetAliveMonsters(),
                   center,
                   effectDefinition.Radius,
                   null,
                   targets) > 0;
    }

    private static bool TryGetRadiusCenter(EffectTriggerContext triggerContext, out Vector3 center)
    {
        MonsterBehaviour targetMonster = triggerContext.TargetMonster;

        if (IsValidEffectContextTarget(targetMonster, triggerContext))
        {
            center = GetMonsterHitPosition(targetMonster);
            return true;
        }

        if (triggerContext.HasTriggerPosition)
        {
            center = triggerContext.TriggerPosition;
            return true;
        }

        center = default;
        return false;
    }

}
