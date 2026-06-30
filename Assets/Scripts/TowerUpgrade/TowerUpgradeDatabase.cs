using System.Collections.Generic;
using UnityEngine;

public class TowerUpgradeDatabase : MonoBehaviour
{
    [SerializeField] private List<TowerUpgradeDefinition> upgradeDefinitions = new List<TowerUpgradeDefinition>();

    public IReadOnlyList<TowerUpgradeDefinition> GetAllUpgrades()
    {
        return upgradeDefinitions;
    }

    public List<TowerUpgradeDefinition> GetUpgradesByTowerFamily(TowerFamily towerFamily)
    {
        List<TowerUpgradeDefinition> results = new List<TowerUpgradeDefinition>();

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[i];

            if (upgradeDefinition != null && upgradeDefinition.TowerFamily == towerFamily)
            {
                results.Add(upgradeDefinition);
            }
        }

        return results;
    }

    public List<TowerUpgradeDefinition> GetUpgradesByRequiredTowerLevel(int requiredTowerLevel)
    {
        List<TowerUpgradeDefinition> results = new List<TowerUpgradeDefinition>();

        if (requiredTowerLevel < 1)
        {
            return results;
        }

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[i];

            if (upgradeDefinition != null && upgradeDefinition.RequiredTowerLevel == requiredTowerLevel)
            {
                results.Add(upgradeDefinition);
            }
        }

        return results;
    }

    public List<TowerUpgradeDefinition> GetUpgradesByTowerFamilyAndRequiredTowerLevel(
        TowerFamily towerFamily,
        int requiredTowerLevel)
    {
        List<TowerUpgradeDefinition> results = new List<TowerUpgradeDefinition>();

        if (requiredTowerLevel < 1)
        {
            return results;
        }

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[i];

            if (upgradeDefinition != null &&
                upgradeDefinition.TowerFamily == towerFamily &&
                upgradeDefinition.RequiredTowerLevel == requiredTowerLevel)
            {
                results.Add(upgradeDefinition);
            }
        }

        return results;
    }

    public List<TowerUpgradeDefinition> GetUpgradesByBehaviourPackageId(string behaviourPackageId)
    {
        List<TowerUpgradeDefinition> results = new List<TowerUpgradeDefinition>();

        if (string.IsNullOrWhiteSpace(behaviourPackageId))
        {
            return results;
        }

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[i];

            if (upgradeDefinition != null && upgradeDefinition.BehaviourPackageId == behaviourPackageId)
            {
                results.Add(upgradeDefinition);
            }
        }

        return results;
    }

    public bool IsValid()
    {
        return ValidateContent(true);
    }

    private bool ValidateContent(bool logWarnings)
    {
        if (upgradeDefinitions == null)
        {
            return true;
        }

        bool isValid = true;
        HashSet<TowerUpgradeDefinition> configuredDefinitions = new HashSet<TowerUpgradeDefinition>();

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition upgradeDefinition = upgradeDefinitions[i];

            if (upgradeDefinition == null)
            {
                Warn(logWarnings, $"Tower upgrade database is invalid: upgrade definition at index {i} is missing.");
                isValid = false;
                continue;
            }

            if (!configuredDefinitions.Add(upgradeDefinition))
            {
                Warn(logWarnings, $"Tower upgrade database is invalid: duplicate upgrade definition '{upgradeDefinition.name}'.");
                isValid = false;
            }

            if (!upgradeDefinition.IsValid())
            {
                Warn(logWarnings, $"Tower upgrade database is invalid: upgrade definition '{upgradeDefinition.name}' failed authoring validation.");
                isValid = false;
            }
        }

        return isValid;
    }

    private void Warn(bool logWarnings, string message)
    {
        if (logWarnings)
        {
            Debug.LogWarning(message, this);
        }
    }
}
