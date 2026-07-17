public readonly struct MagicOrbRuntimeOptions
{
    public TowerUpgradeDefinition ArcaneDetonationSourceUpgrade { get; }
    public EffectDefinition ArcaneDetonationEffect { get; }

    public MagicOrbRuntimeOptions(
        TowerUpgradeDefinition arcaneDetonationSourceUpgrade,
        EffectDefinition arcaneDetonationEffect)
    {
        ArcaneDetonationSourceUpgrade = arcaneDetonationSourceUpgrade;
        ArcaneDetonationEffect = arcaneDetonationEffect;
    }
}
