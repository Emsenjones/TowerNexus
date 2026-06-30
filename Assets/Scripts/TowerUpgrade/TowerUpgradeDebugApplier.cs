#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEngine;

public class TowerUpgradeDebugApplier : MonoBehaviour
{
    [SerializeField] private TowerUpgradeSystem towerUpgradeSystem;
    [SerializeField] private TowerInstance targetTower;
    [SerializeField] private TowerUpgradeDefinition upgradeDefinition;

    [Button("Apply Configured Upgrade")]
    [ContextMenu("Apply Configured Upgrade")]
    private void ApplyConfiguredUpgrade()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Tower upgrade debug applier can only apply upgrades during Play Mode.", this);
            return;
        }

        TowerUpgradeSystem resolvedUpgradeSystem = towerUpgradeSystem != null
            ? towerUpgradeSystem
            : FindFirstObjectByType<TowerUpgradeSystem>();

        if (resolvedUpgradeSystem == null)
        {
            Debug.LogWarning("Tower upgrade debug applier cannot apply upgrade: TowerUpgradeSystem is missing.", this);
            return;
        }

        if (resolvedUpgradeSystem.TryApplyUpgrade(targetTower, upgradeDefinition, out string failureReason))
        {
            Debug.Log(
                $"Tower upgrade debug applier applied '{GetUpgradeName(upgradeDefinition)}' to '{GetTowerName(targetTower)}'.",
                this);
            return;
        }

        Debug.LogWarning(
            $"Tower upgrade debug applier failed to apply '{GetUpgradeName(upgradeDefinition)}' to '{GetTowerName(targetTower)}': {failureReason}",
            this);
    }

    private string GetUpgradeName(TowerUpgradeDefinition definition)
    {
        return definition != null ? definition.name : "None";
    }

    private string GetTowerName(TowerInstance towerInstance)
    {
        return towerInstance != null ? towerInstance.name : "None";
    }
}
#endif
