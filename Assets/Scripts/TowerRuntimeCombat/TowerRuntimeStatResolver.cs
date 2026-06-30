using System.Collections.Generic;
using UnityEngine;

public static class TowerRuntimeStatResolver
{
    public const float MinimumAttackInterval = 0f;
    public const float MinimumDroneBatteryDuration = 0.01f;
    public const float MinimumDroneBurstCooldown = 0f;

    public static ResolvedTowerCombatStats Resolve(TowerInstance towerInstance, AttackConfig attackConfig)
    {
        float attackRangeDelta = 0f;
        float attackIntervalDelta = 0f;
        float damageBonus = 0f;
        float magicOrbRotationSpeedDelta = 0f;
        float magicOrbMaxHitCountDelta = 0f;
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
                    ref magicOrbMaxHitCountDelta,
                    ref droneBatteryDurationDelta,
                    ref droneBurstCooldownDelta
                );
            }
        }

        float baseAttackRange = attackConfig != null ? attackConfig.AttackRange : 0f;
        float baseAttackInterval = attackConfig != null ? attackConfig.AttackInterval : 0f;
        float baseMagicOrbRotationSpeed = attackConfig != null ? attackConfig.MagicOrbRotationSpeed : 0f;
        int baseMagicOrbMaxHitCount = attackConfig != null ? attackConfig.MagicOrbMaxHitCount : 1;
        float baseDroneBatteryDuration = attackConfig != null ? attackConfig.DroneBatteryDuration : MinimumDroneBatteryDuration;
        float baseDroneBurstCooldown = attackConfig != null ? attackConfig.DroneBurstCooldown : 0f;
        int basicDamage = towerInstance != null ? towerInstance.BasicDamage : 0;

        return new ResolvedTowerCombatStats(
            Mathf.Max(0f, baseAttackRange + attackRangeDelta),
            Mathf.Max(MinimumAttackInterval, baseAttackInterval + attackIntervalDelta),
            Mathf.Max(0, basicDamage + Mathf.RoundToInt(damageBonus)),
            Mathf.Max(0f, baseMagicOrbRotationSpeed + magicOrbRotationSpeedDelta),
            Mathf.Max(1, baseMagicOrbMaxHitCount + Mathf.RoundToInt(magicOrbMaxHitCountDelta)),
            Mathf.Max(MinimumDroneBatteryDuration, baseDroneBatteryDuration + droneBatteryDurationDelta),
            Mathf.Max(MinimumDroneBurstCooldown, baseDroneBurstCooldown + droneBurstCooldownDelta)
        );
    }

    private static void AccumulateBasicStatDeltas(
        TowerUpgradeDefinition upgradeDefinition,
        ref float attackRangeDelta,
        ref float attackIntervalDelta,
        ref float damageBonus,
        ref float magicOrbRotationSpeedDelta,
        ref float magicOrbMaxHitCountDelta,
        ref float droneBatteryDurationDelta,
        ref float droneBurstCooldownDelta)
    {
        if (upgradeDefinition == null || upgradeDefinition.RequiredTowerLevel != 1)
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
                case TowerUpgradeStatType.AttackRange:
                    attackRangeDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeStatType.AttackInterval:
                    attackIntervalDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeStatType.DamageBonus:
                    damageBonus += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeStatType.MagicOrbRotationSpeed:
                    magicOrbRotationSpeedDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeStatType.MagicOrbMaxHitCount:
                    magicOrbMaxHitCountDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeStatType.DroneBatteryDuration:
                    droneBatteryDurationDelta += statDelta.AdditiveValue;
                    break;
                case TowerUpgradeStatType.DroneBurstCooldown:
                    droneBurstCooldownDelta += statDelta.AdditiveValue;
                    break;
            }
        }
    }
}
