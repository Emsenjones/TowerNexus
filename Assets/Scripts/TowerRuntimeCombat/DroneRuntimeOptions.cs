public readonly struct DroneRuntimeOptions
{
    public TowerUpgradeDefinition BlastRoundsSourceUpgrade { get; }
    public EffectDefinition BlastRoundsEffect { get; }
    public TowerUpgradeDefinition FinalDiveSourceUpgrade { get; }
    public float FinalDiveHitThreshold { get; }
    public EffectDefinition FinalDiveExplosionEffect { get; }
    public bool IsFinalDiveEnabled =>
        FinalDiveSourceUpgrade != null &&
        FinalDiveHitThreshold > 0f &&
        FinalDiveExplosionEffect != null;

    public DroneRuntimeOptions(
        TowerUpgradeDefinition blastRoundsSourceUpgrade,
        EffectDefinition blastRoundsEffect,
        TowerUpgradeDefinition finalDiveSourceUpgrade,
        float finalDiveHitThreshold,
        EffectDefinition finalDiveExplosionEffect)
    {
        BlastRoundsSourceUpgrade = blastRoundsSourceUpgrade;
        BlastRoundsEffect = blastRoundsEffect;
        FinalDiveSourceUpgrade = finalDiveSourceUpgrade;
        FinalDiveHitThreshold = finalDiveHitThreshold;
        FinalDiveExplosionEffect = finalDiveExplosionEffect;
    }
}
