using UnityEngine;

public readonly struct EffectTriggerContext
{
    public EffectTriggerContext(
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        MonsterBehaviour targetMonster,
        bool hasTriggerPosition,
        Vector3 triggerPosition,
        int resolvedDamage,
        bool allowsElementalApplication,
        bool allowsLifecycleOwnerTarget = false)
    {
        SourceTower = sourceTower;
        SourceUpgrade = sourceUpgrade;
        TargetMonster = targetMonster;
        HasTriggerPosition = hasTriggerPosition;
        TriggerPosition = triggerPosition;
        ResolvedDamage = resolvedDamage;
        AllowsElementalApplication = allowsElementalApplication;
        AllowsLifecycleOwnerTarget = allowsLifecycleOwnerTarget;
    }

    public TowerInstance SourceTower { get; }
    public TowerUpgradeDefinition SourceUpgrade { get; }
    public MonsterBehaviour TargetMonster { get; }
    public bool HasTriggerPosition { get; }
    // TriggerPosition is valid only when HasTriggerPosition is true.
    // Producers provide the relevant hit, impact, owner, or zone position.
    public Vector3 TriggerPosition { get; }
    public int ResolvedDamage { get; }
    public bool AllowsElementalApplication { get; }
    // Removed lifecycle Effects may release owner-bound state after the owner
    // has stopped being a gameplay target. This does not reopen combat targeting.
    public bool AllowsLifecycleOwnerTarget { get; }
}
