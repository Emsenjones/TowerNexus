using UnityEngine;

public class TowerUpgradeSystem : MonoBehaviour
{
    private const int CurrentMaxTowerLevel = 3;

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

        int candidateNextLevel = targetTower.CurrentLevel + 1;
        int maxAllowedLevel = Mathf.Min(targetTower.GetMaxConfiguredLevel(), CurrentMaxTowerLevel);

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

    private bool IsSupportedRequiredTowerLevel(int requiredTowerLevel)
    {
        return requiredTowerLevel >= 1 && requiredTowerLevel <= CurrentMaxTowerLevel;
    }
}
