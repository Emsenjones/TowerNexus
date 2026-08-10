using System.Collections.Generic;
using UnityEngine;

public class TowerUpgradeSystem : MonoBehaviour
{
    public const int SupportedMaximumTowerLevel = 3;

    private readonly Dictionary<TowerFamily, int> stageMaximumTowerLevels =
        new Dictionary<TowerFamily, int>();

    public bool HasStageLevelRules { get; private set; }

    public bool TryBindStageLevelRules(
        IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions,
        out string failureReason)
    {
        ClearStageLevelRules();

        if (!TryResolveStageMaximumTowerLevels(
                upgradeDefinitions,
                stageMaximumTowerLevels,
                out failureReason))
        {
            return false;
        }

        HasStageLevelRules = true;
        failureReason = string.Empty;
        return true;
    }

    public void ClearStageLevelRules()
    {
        stageMaximumTowerLevels.Clear();
        HasStageLevelRules = false;
    }

    public int GetStageMaximumTowerLevel(TowerFamily towerFamily)
    {
        if (!HasStageLevelRules)
        {
            return 0;
        }

        return stageMaximumTowerLevels.TryGetValue(
            towerFamily,
            out int maximumTowerLevel)
            ? maximumTowerLevel
            : 1;
    }

    public static bool TryResolveStageMaximumTowerLevels(
        IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions,
        Dictionary<TowerFamily, int> resolvedMaximumLevels,
        out string failureReason)
    {
        if (resolvedMaximumLevels == null)
        {
            failureReason = "Resolved maximum-level output is missing.";
            return false;
        }

        resolvedMaximumLevels.Clear();

        if (upgradeDefinitions == null)
        {
            failureReason = "Tower Upgrade Draft pool is null.";
            return false;
        }

        Dictionary<TowerFamily, HashSet<int>> requiredLevelsByFamily =
            new Dictionary<TowerFamily, HashSet<int>>();

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[i];

            if (upgradeDefinition == null)
            {
                failureReason = $"Tower Upgrade Draft pool entry {i} is missing.";
                resolvedMaximumLevels.Clear();
                return false;
            }

            if (!upgradeDefinition.IsValid())
            {
                failureReason =
                    $"Tower Upgrade definition '{upgradeDefinition.name}' failed owner validation.";
                resolvedMaximumLevels.Clear();
                return false;
            }

            int requiredTowerLevel = upgradeDefinition.RequiredTowerLevel;

            if (!IsSupportedRequiredTowerLevel(requiredTowerLevel))
            {
                failureReason =
                    $"Tower Upgrade definition '{upgradeDefinition.name}' requires " +
                    $"Tower Level {requiredTowerLevel}, outside the supported range " +
                    $"1-{SupportedMaximumTowerLevel}.";
                resolvedMaximumLevels.Clear();
                return false;
            }

            TowerFamily towerFamily = upgradeDefinition.TowerFamily;

            if (!requiredLevelsByFamily.TryGetValue(
                    towerFamily,
                    out HashSet<int> requiredLevels))
            {
                requiredLevels = new HashSet<int>();
                requiredLevelsByFamily.Add(towerFamily, requiredLevels);
            }

            requiredLevels.Add(requiredTowerLevel);

            if (!resolvedMaximumLevels.TryGetValue(
                    towerFamily,
                    out int currentMaximum) ||
                requiredTowerLevel > currentMaximum)
            {
                resolvedMaximumLevels[towerFamily] = requiredTowerLevel;
            }
        }

        foreach (KeyValuePair<TowerFamily, int> pair in resolvedMaximumLevels)
        {
            HashSet<int> requiredLevels = requiredLevelsByFamily[pair.Key];

            for (int level = 2; level <= pair.Value; level++)
            {
                if (requiredLevels.Contains(level))
                {
                    continue;
                }

                failureReason =
                    $"TowerFamily '{pair.Key}' reaches Stage Tower Level {pair.Value} " +
                    $"but no Stage Upgrade first becomes eligible at Level {level}.";
                resolvedMaximumLevels.Clear();
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    public bool CanLevelUpTower(
        TowerInstance targetTower,
        TowerDefinition draftTowerDefinition,
        out int nextLevel)
    {
        nextLevel = 0;

        if (targetTower == null || draftTowerDefinition == null)
        {
            return false;
        }

        TowerDefinition targetDefinition = targetTower.TowerDefinition;

        if (targetDefinition == null)
        {
            return false;
        }

        if (targetDefinition.TowerFamily != draftTowerDefinition.TowerFamily)
        {
            return false;
        }

        if (!HasStageLevelRules)
        {
            return false;
        }

        int candidateNextLevel = targetTower.CurrentLevel + 1;
        int maxAllowedLevel = Mathf.Min(
            targetTower.GetMaxConfiguredLevel(),
            SupportedMaximumTowerLevel,
            GetStageMaximumTowerLevel(targetDefinition.TowerFamily));

        if (maxAllowedLevel <= 0 ||
            candidateNextLevel > maxAllowedLevel ||
            !targetTower.CanSetLevel(candidateNextLevel))
        {
            return false;
        }

        nextLevel = candidateNextLevel;
        return true;
    }

    public bool TryLevelUpTower(
        TowerInstance targetTower,
        TowerDefinition draftTowerDefinition,
        out int nextLevel)
    {
        if (!CanLevelUpTower(targetTower, draftTowerDefinition, out nextLevel))
        {
            return false;
        }

        return targetTower.TrySetLevel(nextLevel);
    }

    public bool CanApplyUpgrade(
        TowerInstance targetTower,
        TowerUpgradeDefinition upgradeDefinition,
        out string failureReason)
    {
        if (targetTower == null)
        {
            failureReason = "Target tower is missing.";
            return false;
        }

        if (upgradeDefinition == null)
        {
            failureReason = "Upgrade definition is missing.";
            return false;
        }

        int requiredTowerLevel = upgradeDefinition.RequiredTowerLevel;

        if (!IsSupportedRequiredTowerLevel(requiredTowerLevel))
        {
            failureReason = $"Required Tower Level {requiredTowerLevel} is not supported by v1 upgrade rules.";
            return false;
        }

        TowerDefinition targetDefinition = targetTower.TowerDefinition;

        if (targetDefinition == null)
        {
            failureReason = "Target tower definition is missing.";
            return false;
        }

        if (targetDefinition.TowerFamily != upgradeDefinition.TowerFamily)
        {
            failureReason = $"Upgrade TowerFamily '{upgradeDefinition.TowerFamily}' does not match target tower TowerFamily '{targetDefinition.TowerFamily}'.";
            return false;
        }

        if (!upgradeDefinition.IsValid())
        {
            failureReason = $"Upgrade definition '{upgradeDefinition.name}' failed validation.";
            return false;
        }

        if (targetTower.CurrentLevel < requiredTowerLevel)
        {
            failureReason = $"Target tower level {targetTower.CurrentLevel} does not satisfy Required Tower Level {requiredTowerLevel}.";
            return false;
        }

        if (targetTower.HasUpgrade(upgradeDefinition))
        {
            failureReason = $"Target tower already has upgrade '{upgradeDefinition.name}'.";
            return false;
        }

        if (upgradeDefinition.UpgradeLayer == TowerUpgradeLayer.Behaviour &&
            upgradeDefinition.BehaviourPackageType != TowerBehaviourPackageType.None &&
            targetTower.HasBehaviourPackage(upgradeDefinition.BehaviourPackageType))
        {
            failureReason = $"Target tower already has Behaviour package '{upgradeDefinition.BehaviourPackageType}'.";
            return false;
        }

        if (upgradeDefinition.UpgradeLayer == TowerUpgradeLayer.Elemental &&
            targetTower.HasElementalUpgrade())
        {
            failureReason = "Target tower already has an Elemental Layer upgrade.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool TryApplyUpgrade(
        TowerInstance targetTower,
        TowerUpgradeDefinition upgradeDefinition,
        out string failureReason)
    {
        if (!CanApplyUpgrade(targetTower, upgradeDefinition, out failureReason))
        {
            return false;
        }

        if (!targetTower.TryRecordUpgrade(upgradeDefinition))
        {
            failureReason = $"Target tower failed to record upgrade '{upgradeDefinition.name}'.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public static bool IsSupportedRequiredTowerLevel(int requiredTowerLevel)
    {
        return requiredTowerLevel >= 1 &&
               requiredTowerLevel <= SupportedMaximumTowerLevel;
    }
}
