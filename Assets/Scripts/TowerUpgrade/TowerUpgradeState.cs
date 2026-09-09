using System.Collections.Generic;

public class TowerUpgradeState
{
    private readonly List<TowerUpgradeDefinition> appliedUpgrades = new List<TowerUpgradeDefinition>();
    private readonly List<TowerBehaviourPackageType> activeBehaviourPackageTypes = new List<TowerBehaviourPackageType>();

    public IReadOnlyList<TowerUpgradeDefinition> AppliedUpgrades => appliedUpgrades;

    public void Reset()
    {
        appliedUpgrades.Clear();
        activeBehaviourPackageTypes.Clear();
    }

    public bool HasUpgrade(TowerUpgradeDefinition upgradeDefinition)
    {
        return upgradeDefinition != null && appliedUpgrades.Contains(upgradeDefinition);
    }

    public bool HasBehaviourPackage(TowerBehaviourPackageType packageType)
    {
        return TryGetBehaviourPackageUpgrade(packageType, out _);
    }

    public bool HasElementalUpgrade()
    {
        return TryGetElementalUpgrade(out _);
    }

    public bool TryGetElementalUpgrade(out TowerUpgradeDefinition upgradeDefinition)
    {
        upgradeDefinition = null;

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            TowerUpgradeDefinition candidateUpgradeDefinition = appliedUpgrades[i];

            if (candidateUpgradeDefinition != null &&
                candidateUpgradeDefinition.UpgradeLayer == TowerUpgradeLayer.Elemental)
            {
                upgradeDefinition = candidateUpgradeDefinition;
                return true;
            }
        }

        return false;
    }

    public bool TryGetBehaviourPackageUpgrade(
        TowerBehaviourPackageType packageType,
        out TowerUpgradeDefinition upgradeDefinition)
    {
        upgradeDefinition = null;

        if (packageType == TowerBehaviourPackageType.None)
        {
            return false;
        }

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            TowerUpgradeDefinition candidateUpgradeDefinition = appliedUpgrades[i];

            if (candidateUpgradeDefinition != null &&
                candidateUpgradeDefinition.BehaviourPackageType == packageType)
            {
                upgradeDefinition = candidateUpgradeDefinition;
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<TowerBehaviourPackageType> GetActiveBehaviourPackageTypes()
    {
        activeBehaviourPackageTypes.Clear();

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = appliedUpgrades[i];

            if (upgradeDefinition == null ||
                upgradeDefinition.BehaviourPackageType == TowerBehaviourPackageType.None)
            {
                continue;
            }

            activeBehaviourPackageTypes.Add(upgradeDefinition.BehaviourPackageType);
        }

        return activeBehaviourPackageTypes;
    }

    internal void PrepareUpgradeCapacity()
    {
        // Allocation belongs to preflight, not the callback-free semantic commit.
        if (appliedUpgrades.Capacity == appliedUpgrades.Count)
            appliedUpgrades.Capacity = appliedUpgrades.Count + 1;
    }

    internal void CommitPreparedUpgrade(TowerUpgradeDefinition upgradeDefinition)
    {
        appliedUpgrades.Add(upgradeDefinition);
    }
}
