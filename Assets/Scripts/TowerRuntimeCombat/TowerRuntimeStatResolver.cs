using System;
using System.Collections.Generic;
using UnityEngine;

public static class TowerRuntimeStatResolver
{
    public const float MinimumAttackCycleDuration = 0f;
    public const float MinimumDroneBurstCooldown = 0f;

    public static event Action<TowerOwnedDamageResolutionObservation>
        OnTowerOwnedDamageResolutionObserved;
    public static event Action<TowerOwnedDamageApplicationObservation>
        OnTowerOwnedDamageApplicationObserved;

    public static ResolvedTowerCombatStats Resolve(
        TowerInstance towerInstance,
        TowerCombatBaseStats baseStats,
        TowerLevelConfig levelConfigOverride = null)
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

        TowerLevelConfig levelConfig = ResolveLevelConfig(
            towerInstance,
            levelConfigOverride);
        int levelBasicDamage = levelConfig != null ? levelConfig.BasicDamage : 0;
        float resolvedBasicDamage = levelBasicDamage + damageBonus;
        int resolvedDamageBonus = Mathf.RoundToInt(damageBonus);

        return new ResolvedTowerCombatStats(
            Mathf.Max(0f, baseStats.AttackRange + attackRangeDelta),
            Mathf.Max(
                MinimumAttackCycleDuration,
                baseStats.AttackCycleDuration + attackCycleDurationDelta),
            levelBasicDamage,
            resolvedBasicDamage,
            resolvedDamageBonus,
            Mathf.Max(0f, baseStats.MagicOrbRotationSpeed + magicOrbRotationSpeedDelta),
            Mathf.Max(MinimumDroneBurstCooldown, baseStats.DroneBurstCooldown + droneBurstCooldownDelta)
        );
    }

    public static bool TryResolveTowerOwnedDamage(
        TowerInstance towerInstance,
        TowerDamageSourceIdentity damageSourceIdentity,
        float damageScale,
        out TowerOwnedDamageResolution resolution)
    {
        resolution = default;

        if (towerInstance == null)
        {
            PublishRejectedDamageResolution(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.MissingSourceTower);
            return false;
        }

        if (towerInstance.TowerDefinition == null)
        {
            PublishRejectedDamageResolution(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.MissingTowerDefinition);
            return false;
        }

        if (!damageSourceIdentity.IsValid)
        {
            PublishRejectedDamageResolution(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.InvalidDamageSourceIdentity);
            return false;
        }

        if (!IsFinitePositive(damageScale))
        {
            PublishRejectedDamageResolution(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.InvalidDamageScale);
            return false;
        }

        TowerLevelConfig levelConfig = towerInstance.CurrentLevelConfig;

        if (levelConfig == null || !levelConfig.IsValid())
        {
            PublishRejectedDamageResolution(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.InvalidLevelConfiguration);
            return false;
        }

        float rawDamageBonus = ResolveRawDamageBonus(towerInstance);
        float resolvedBasicDamage = levelConfig.BasicDamage + rawDamageBonus;
        float rawProduct = resolvedBasicDamage * damageScale;

        if (!IsFinitePositive(resolvedBasicDamage))
        {
            PublishRejectedDamageResolution(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.InvalidResolvedBasicDamage);
            return false;
        }

        if (!IsFinitePositive(rawProduct))
        {
            PublishRejectedDamageResolution(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.InvalidRawProduct);
            return false;
        }

        int finalDamage = Mathf.RoundToInt(rawProduct);

        if (finalDamage <= 0)
        {
            PublishRejectedDamageResolution(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.NonPositiveFinalDamage);
            return false;
        }

        resolution = new TowerOwnedDamageResolution(
            towerInstance,
            towerInstance.TowerDefinition.TowerFamily,
            towerInstance.CurrentLevel,
            levelConfig.BasicDamage,
            rawDamageBonus,
            resolvedBasicDamage,
            damageSourceIdentity,
            damageScale,
            rawProduct,
            finalDamage);
        PublishDamageResolution(
            new TowerOwnedDamageResolutionObservation(
                towerInstance,
                damageSourceIdentity,
                damageScale,
                TowerOwnedDamageFailureReason.None,
                resolution));
        return true;
    }

    public static void PublishTowerOwnedDamageApplication(
        TowerOwnedDamageResolution resolution,
        int successfulApplicationCount)
    {
        if (successfulApplicationCount <= 0)
        {
            return;
        }

        Action<TowerOwnedDamageApplicationObservation> observers =
            OnTowerOwnedDamageApplicationObserved;

        if (observers == null)
        {
            return;
        }

        TowerOwnedDamageApplicationObservation observation =
            new TowerOwnedDamageApplicationObservation(
                resolution,
                successfulApplicationCount);
        Delegate[] invocationList = observers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<TowerOwnedDamageApplicationObservation>)invocationList[i])(
                    observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static void PublishRejectedDamageResolution(
        TowerInstance sourceTower,
        TowerDamageSourceIdentity damageSourceIdentity,
        float damageScale,
        TowerOwnedDamageFailureReason failureReason)
    {
        PublishDamageResolution(
            new TowerOwnedDamageResolutionObservation(
                sourceTower,
                damageSourceIdentity,
                damageScale,
                failureReason,
                default));
    }

    private static void PublishDamageResolution(
        TowerOwnedDamageResolutionObservation observation)
    {
        Action<TowerOwnedDamageResolutionObservation> observers =
            OnTowerOwnedDamageResolutionObserved;

        if (observers == null)
        {
            return;
        }

        Delegate[] invocationList = observers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<TowerOwnedDamageResolutionObservation>)invocationList[i])(
                    observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static TowerLevelConfig ResolveLevelConfig(
        TowerInstance towerInstance,
        TowerLevelConfig levelConfigOverride)
    {
        return levelConfigOverride != null
            ? levelConfigOverride
            : towerInstance != null
                ? towerInstance.CurrentLevelConfig
                : null;
    }

    private static float ResolveRawDamageBonus(TowerInstance towerInstance)
    {
        float damageBonus = 0f;
        float unusedAttackRange = 0f;
        float unusedAttackCycle = 0f;
        float unusedOrbRotation = 0f;
        float unusedDroneCooldown = 0f;

        IReadOnlyList<TowerUpgradeDefinition> appliedUpgrades =
            towerInstance != null ? towerInstance.AppliedUpgrades : null;

        if (appliedUpgrades == null)
        {
            return damageBonus;
        }

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            AccumulateBasicStatDeltas(
                appliedUpgrades[i],
                ref unusedAttackRange,
                ref unusedAttackCycle,
                ref damageBonus,
                ref unusedOrbRotation,
                ref unusedDroneCooldown);
        }

        return damageBonus;
    }

    private static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) &&
               !float.IsInfinity(value) &&
               value > 0f;
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
