public readonly struct ProjectileRuntimeOptions
{
    public bool CanPierce { get; }
    public int MaxPierceHitCount { get; }
    public bool IsBounceChild { get; }
    public bool IsInitialRelease => !IsBounceChild;
    public TowerUpgradeDefinition ExplosiveShellSourceUpgrade { get; }
    public EffectDefinition ExplosiveShellEffect { get; }
    public float BounceSearchRadius { get; }
    public int RemainingBounceCount { get; }
    public float BounceArcHeight { get; }
    public TargetSelectionType BounceTargetSelectionType { get; }

    public ProjectileRuntimeOptions(
        bool canPierce,
        int maxPierceHitCount,
        bool isBounceChild = false,
        TowerUpgradeDefinition explosiveShellSourceUpgrade = null,
        EffectDefinition explosiveShellEffect = null,
        float bounceSearchRadius = 0f,
        int remainingBounceCount = 0,
        float bounceArcHeight = 0f,
        TargetSelectionType bounceTargetSelectionType = TargetSelectionType.Nearest)
    {
        CanPierce = canPierce;
        MaxPierceHitCount = maxPierceHitCount;
        IsBounceChild = isBounceChild;
        ExplosiveShellSourceUpgrade = explosiveShellSourceUpgrade;
        ExplosiveShellEffect = explosiveShellEffect;
        BounceSearchRadius = bounceSearchRadius;
        RemainingBounceCount = remainingBounceCount;
        BounceArcHeight = bounceArcHeight;
        BounceTargetSelectionType = bounceTargetSelectionType;
    }
}
