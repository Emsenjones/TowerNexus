using System.Collections.Generic;

public class TowerUpgradeState
{
    private readonly List<TowerUpgradeDefinition> appliedUpgrades = new List<TowerUpgradeDefinition>();

    public IReadOnlyList<TowerUpgradeDefinition> AppliedUpgrades => appliedUpgrades;

    public void Reset()
    {
        appliedUpgrades.Clear();
    }

    public bool HasUpgrade(TowerUpgradeDefinition upgradeDefinition)
    {
        return upgradeDefinition != null && appliedUpgrades.Contains(upgradeDefinition);
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
