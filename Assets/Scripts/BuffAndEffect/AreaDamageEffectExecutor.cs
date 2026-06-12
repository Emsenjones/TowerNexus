using System.Collections.Generic;
using UnityEngine;

public static class AreaDamageEffectExecutor
{
    public static void Execute(ProjectileImpactContext impactContext)
    {
        EffectConfig effectConfig = impactContext.ImpactEffectConfig;

        if (effectConfig == null)
        {
            return;
        }

        Execute(
            impactContext.ImpactPosition,
            impactContext.AttackDamage,
            impactContext.SourceTower,
            effectConfig
        );
    }

    public static void Execute(
        Vector3 impactPosition,
        int attackDamage,
        TowerInstance sourceTower,
        EffectConfig effectConfig)
    {
        if (effectConfig == null)
        {
            Debug.LogWarning("Area damage effect cannot execute: effect config is null.");
            return;
        }

        if (attackDamage <= 0)
        {
            return;
        }

        if (!effectConfig.IsValid())
        {
            return;
        }

        MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();

        if (monsterManager == null)
        {
            Debug.LogWarning("Area damage effect cannot execute: monster manager was not found.");
            return;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!IsValidTarget(monster))
            {
                continue;
            }

            if (Vector3.Distance(impactPosition, monster.HitAnchor.position) > effectConfig.Radius)
            {
                continue;
            }

            monster.TakeDamage(attackDamage);
        }
    }

    private static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }
}
