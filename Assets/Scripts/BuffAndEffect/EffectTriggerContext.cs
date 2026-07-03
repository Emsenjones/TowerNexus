using UnityEngine;

public struct EffectTriggerContext
{
    public EffectTriggerContext(
        EffectTriggerType triggerType,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        MonsterBehaviour targetMonster,
        bool hasTriggerPosition,
        Vector3 triggerPosition,
        int resolvedDamage,
        bool canApplyElementalStack)
    {
        TriggerType = triggerType;
        SourceTower = sourceTower;
        SourceUpgrade = sourceUpgrade;
        TargetMonster = targetMonster;
        HasTriggerPosition = hasTriggerPosition;
        TriggerPosition = triggerPosition;
        ResolvedDamage = resolvedDamage;
        CanApplyElementalStack = canApplyElementalStack;
    }

    public EffectTriggerType TriggerType { get; }
    public TowerInstance SourceTower { get; }
    public TowerUpgradeDefinition SourceUpgrade { get; }
    public MonsterBehaviour TargetMonster { get; }
    public bool HasTriggerPosition { get; }
    // TriggerPosition is valid only when HasTriggerPosition is true. OnHit uses the hit/target position, OnImpact uses the impact position.
    public Vector3 TriggerPosition { get; }
    public int ResolvedDamage { get; }
    public bool CanApplyElementalStack { get; }
}
