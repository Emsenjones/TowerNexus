using System.Collections.Generic;
using UnityEngine;

public static class TowerRuntimeStatResolver
{
    public const float MinimumAttackCycleDuration = 0f;
    public const float MinimumDroneBurstCooldown = 0f;

    public static ResolvedTowerCombatStats Resolve(
        TowerInstance towerInstance,
        TowerCombatBaseStats baseStats)
    {
        float attackRangeDelta = 0f;
        float attackCycleDurationDelta = 0f;
        float damageBonus = 0f;
        float magicOrbRotationSpeedDelta = 0f;
        float droneBurstCooldownDelta = 0f;

        IReadOnlyList<TowerUpgradeDefinition> appliedUpgrades = towerInstance != null
            ? towerInstance.AppliedUpgrades
            : null;

        if (appliedUpgrades != null)
        {
            for (int i = 0; i < appliedUpgrades.Count; i++)
            {
                AccumulateBasicStatDeltas(
                    appliedUpgrades[i],
                    ref attackRangeDelta,
                    ref attackCycleDurationDelta,
                    ref damageBonus,
                    ref magicOrbRotationSpeedDelta,
                    ref droneBurstCooldownDelta
                );
            }
        }

        int resolvedDamageBonus = Mathf.RoundToInt(damageBonus);

        return new ResolvedTowerCombatStats(
            Mathf.Max(0f, baseStats.AttackRange + attackRangeDelta),
            Mathf.Max(
                MinimumAttackCycleDuration,
                baseStats.AttackCycleDuration + attackCycleDurationDelta),
            Mathf.Max(0, baseStats.AttackDamage + resolvedDamageBonus),
            resolvedDamageBonus,
            Mathf.Max(0f, baseStats.MagicOrbRotationSpeed + magicOrbRotationSpeedDelta),
            Mathf.Max(MinimumDroneBurstCooldown, baseStats.DroneBurstCooldown + droneBurstCooldownDelta)
        );
    }

    private static void AccumulateBasicStatDeltas(
        TowerUpgradeDefinition upgradeDefinition,
        ref float attackRangeDelta,
        ref float attackCycleDurationDelta,
        ref float damageBonus,
        ref float magicOrbRotationSpeedDelta,
        ref float droneBurstCooldownDelta)
    {
        if (upgradeDefinition == null || upgradeDefinition.UpgradeLayer != TowerUpgradeLayer.Basic)
        {
            return;
        }

        IReadOnlyList<TowerUpgradeStatDelta> statDeltas = upgradeDefinition.BasicStatDeltas;

        if (statDeltas == null)
        {
            return;
        }

        for (int i = 0; i < statDeltas.Count; i++)
        {
            TowerUpgradeStatDelta statDelta = statDeltas[i];

            if (statDelta == null)
            {
                continue;
            }

            switch (statDelta.StatType)
            {
                case TowerUpgradeBasicStatType.AttackRange:
                    attackRangeDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeBasicStatType.AttackCycleDuration:
                    attackCycleDurationDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeBasicStatType.DamageBonus:
                    damageBonus += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeBasicStatType.MagicOrbRotationSpeed:
                    magicOrbRotationSpeedDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeBasicStatType.DroneBurstCooldown:
                    droneBurstCooldownDelta += statDelta.AdditiveValue;
                    break;
            }
        }
    }
}
