using System.Collections.Generic;
using UnityEngine;

public static class TowerRuntimeStatResolver
{
    public const float MinimumAttackInterval = 0f;
    public const float MinimumDroneBatteryDuration = 0.01f;
    public const float MinimumDroneBurstCooldown = 0f;

    public static ResolvedTowerCombatStats Resolve(
        TowerInstance towerInstance,
        TowerCombatBaseStats baseStats)
    {
        float attackRangeDelta = 0f;
        float attackIntervalDelta = 0f;
        float damageBonus = 0f;
        float magicOrbRotationSpeedDelta = 0f;
        float droneBatteryDurationDelta = 0f;
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
                    ref attackIntervalDelta,
                    ref damageBonus,
                    ref magicOrbRotationSpeedDelta,
                    ref droneBatteryDurationDelta,
                    ref droneBurstCooldownDelta
                );
            }
        }

        int basicDamage = towerInstance != null ? towerInstance.BasicDamage : 0;

        return new ResolvedTowerCombatStats(
            Mathf.Max(0f, baseStats.AttackRange + attackRangeDelta),
            Mathf.Max(MinimumAttackInterval, baseStats.AttackInterval + attackIntervalDelta),
            Mathf.Max(0, basicDamage + Mathf.RoundToInt(damageBonus)),
            Mathf.Max(0f, baseStats.MagicOrbRotationSpeed + magicOrbRotationSpeedDelta),
            Mathf.Max(1, baseStats.MagicOrbMaxHitCount),
            Mathf.Max(MinimumDroneBatteryDuration, baseStats.DroneBatteryDuration + droneBatteryDurationDelta),
            Mathf.Max(MinimumDroneBurstCooldown, baseStats.DroneBurstCooldown + droneBurstCooldownDelta)
        );
    }

    private static void AccumulateBasicStatDeltas(
        TowerUpgradeDefinition upgradeDefinition,
        ref float attackRangeDelta,
        ref float attackIntervalDelta,
        ref float damageBonus,
        ref float magicOrbRotationSpeedDelta,
        ref float droneBatteryDurationDelta,
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
                case TowerUpgradeBasicStatType.AttackInterval:
                    attackIntervalDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeBasicStatType.DamageBonus:
                    damageBonus += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeBasicStatType.MagicOrbRotationSpeed:
                    magicOrbRotationSpeedDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeBasicStatType.DroneBatteryDuration:
                    droneBatteryDurationDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeBasicStatType.DroneBurstCooldown:
                    droneBurstCooldownDelta += statDelta.AdditiveValue;
                    break;
            }
        }
    }
}
