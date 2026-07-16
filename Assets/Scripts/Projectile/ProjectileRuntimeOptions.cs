public readonly struct ProjectileRuntimeOptions
{
    public bool CanPierce { get; }
    public int MaxPierceHitCount { get; }
    public bool IsBounceChild { get; }
    public bool IsInitialRelease => !IsBounceChild;
    public TowerUpgradeDefinition ExplosiveShellSourceUpgrade { get; }
    public EffectDefinition ExplosiveShellEffect { get; }

    public ProjectileRuntimeOptions(
        bool canPierce,
        int maxPierceHitCount,
        bool isBounceChild = false,
        TowerUpgradeDefinition explosiveShellSourceUpgrade = null,
        EffectDefinition explosiveShellEffect = null)
    {
        CanPierce = canPierce;
        MaxPierceHitCount = maxPierceHitCount;
        IsBounceChild = isBounceChild;
        ExplosiveShellSourceUpgrade = explosiveShellSourceUpgrade;
        ExplosiveShellEffect = explosiveShellEffect;
    }
}
