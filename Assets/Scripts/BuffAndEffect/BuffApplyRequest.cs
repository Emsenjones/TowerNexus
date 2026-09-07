using UnityEngine;

public struct BuffApplyRequest
{
    public BuffApplyRequest(
        BuffDefinition buffDefinition,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        bool hasTriggerPosition,
        Vector3 triggerPosition)
        : this(
            buffDefinition,
            sourceTower,
            sourceUpgrade,
            hasTriggerPosition,
            triggerPosition,
            1)
    {
    }

    public BuffApplyRequest(
        BuffDefinition buffDefinition,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        bool hasTriggerPosition,
        Vector3 triggerPosition,
        int requestedStackUnits)
    {
        BuffDefinition = buffDefinition;
        SourceTower = sourceTower;
        SourceUpgrade = sourceUpgrade;
        HasTriggerPosition = hasTriggerPosition;
        TriggerPosition = triggerPosition;
        RequestedStackUnits = requestedStackUnits;
    }

    public BuffDefinition BuffDefinition { get; }
    public TowerInstance SourceTower { get; }
    public TowerUpgradeDefinition SourceUpgrade { get; }
    public bool HasTriggerPosition { get; }
    public Vector3 TriggerPosition { get; }
    public int RequestedStackUnits { get; }
}
