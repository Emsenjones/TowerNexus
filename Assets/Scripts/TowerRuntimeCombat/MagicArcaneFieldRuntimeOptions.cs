using UnityEngine;

public readonly struct MagicArcaneFieldRuntimeOptions
{
    public MagicArcaneFieldRuntimeOptions(
        TowerUpgradeDefinition sourceUpgrade,
        float radius,
        float tickInterval,
        EffectDefinition tickEffect,
        GameObject vfxPrefab)
    {
        SourceUpgrade = sourceUpgrade;
        Radius = radius;
        TickInterval = tickInterval;
        TickEffect = tickEffect;
        VfxPrefab = vfxPrefab;
    }

    public TowerUpgradeDefinition SourceUpgrade { get; }
    public float Radius { get; }
    public float TickInterval { get; }
    public EffectDefinition TickEffect { get; }
    public GameObject VfxPrefab { get; }
}
