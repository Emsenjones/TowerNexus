using UnityEngine;

public struct BuffApplyRequest
{
    public BuffApplyRequest(
        BuffDefinition buffDefinition,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        bool hasTriggerPosition,
        Vector3 triggerPosition)
    {
        BuffDefinition = buffDefinition;
        SourceTower = sourceTower;
        SourceUpgrade = sourceUpgrade;
        HasTriggerPosition = hasTriggerPosition;
        TriggerPosition = triggerPosition;
    }

    public BuffDefinition BuffDefinition { get; }
    public TowerInstance SourceTower { get; }
    public TowerUpgradeDefinition SourceUpgrade { get; }
    public bool HasTriggerPosition { get; }
    public Vector3 TriggerPosition { get; }
}
