public readonly struct DroneRuntimeOptions
{
    public TowerUpgradeDefinition BlastRoundsSourceUpgrade { get; }
    public EffectDefinition BlastRoundsEffect { get; }

    public DroneRuntimeOptions(
        TowerUpgradeDefinition blastRoundsSourceUpgrade,
        EffectDefinition blastRoundsEffect)
    {
        BlastRoundsSourceUpgrade = blastRoundsSourceUpgrade;
        BlastRoundsEffect = blastRoundsEffect;
    }
}
