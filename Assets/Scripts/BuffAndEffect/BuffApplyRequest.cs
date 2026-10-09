using UnityEngine;

public struct BuffApplyRequest
{
    public BuffApplyRequest(
        BuffDefinition buffDefinition,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        bool hasTriggerPosition,
        Vector3 triggerPosition,
        TowerKillSource? killSource = null)
        : this(
            buffDefinition,
            sourceTower,
            sourceUpgrade,
            hasTriggerPosition,
            triggerPosition,
            1, killSource)
    {
    }

    public BuffApplyRequest(
        BuffDefinition buffDefinition,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        bool hasTriggerPosition,
        Vector3 triggerPosition,
        int requestedStackUnits,
        TowerKillSource? killSource = null)
    {
        BuffDefinition = buffDefinition;
        SourceTower = sourceTower;
        KillSource = killSource ?? new TowerKillSource(sourceTower != null ? sourceTower.KillBattleBinding : null, sourceTower);
        SourceUpgrade = sourceUpgrade;
        HasTriggerPosition = hasTriggerPosition;
        TriggerPosition = triggerPosition;
        RequestedStackUnits = requestedStackUnits;
    }

    public BuffDefinition BuffDefinition { get; }
    public TowerInstance SourceTower { get; }
    public TowerKillSource KillSource { get; }
    public TowerUpgradeDefinition SourceUpgrade { get; }
    public bool HasTriggerPosition { get; }
    public Vector3 TriggerPosition { get; }
    public int RequestedStackUnits { get; }
}
