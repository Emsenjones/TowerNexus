using System.Collections.Generic;

public class TowerUpgradeState
{
    private readonly List<TowerUpgradeDefinition> appliedUpgrades = new List<TowerUpgradeDefinition>();
    private readonly List<string> activeBehaviourPackageIds = new List<string>();

    public IReadOnlyList<TowerUpgradeDefinition> AppliedUpgrades => appliedUpgrades;

    public void Reset()
    {
        appliedUpgrades.Clear();
    }

    public bool HasUpgrade(TowerUpgradeDefinition upgradeDefinition)
    {
        return upgradeDefinition != null && appliedUpgrades.Contains(upgradeDefinition);
    }

    public bool HasBehaviourPackage(string behaviourPackageId)
    {
        if (string.IsNullOrWhiteSpace(behaviourPackageId))
        {
            return false;
        }

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = appliedUpgrades[i];

            if (upgradeDefinition != null && upgradeDefinition.BehaviourPackageId == behaviourPackageId)
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<string> GetActiveBehaviourPackageIds()
    {
        activeBehaviourPackageIds.Clear();

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = appliedUpgrades[i];

            if (upgradeDefinition == null || string.IsNullOrWhiteSpace(upgradeDefinition.BehaviourPackageId))
            {
                continue;
            }

            activeBehaviourPackageIds.Add(upgradeDefinition.BehaviourPackageId);
        }

        return activeBehaviourPackageIds;
    }

    public bool TryRecordUpgrade(TowerUpgradeDefinition upgradeDefinition)
    {
        if (upgradeDefinition == null || HasUpgrade(upgradeDefinition))
        {
            return false;
        }

        appliedUpgrades.Add(upgradeDefinition);

        return true;
    }
}
