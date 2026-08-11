public readonly struct ProjectileRuntimeOptions
{
    public bool CanPierce { get; }
    public int MaxPierceHitCount { get; }
    public bool IsBounceChild { get; }
    public bool IsInitialRelease => !IsBounceChild;
    public bool LocksDirectDamage { get; }
    public int DirectDamageBonus { get; }
    public TowerUpgradeDefinition ExplosiveArrowSourceUpgrade { get; }
    public EffectDefinition ExplosiveArrowEffect { get; }
    public TowerUpgradeDefinition ExplosiveShellSourceUpgrade { get; }
    public EffectDefinition ExplosiveShellEffect { get; }
    public float BounceSearchRadius { get; }
    public int RemainingBounceCount { get; }
    public float BounceArcHeight { get; }
    public TargetSelectionType BounceTargetSelectionType { get; }
    public int BounceDamage { get; }
    public TowerUpgradeDefinition BlastRoundsSourceUpgrade { get; }
    public EffectDefinition BlastRoundsEffect { get; }

    public ProjectileRuntimeOptions(
        bool canPierce,
        int maxPierceHitCount,
        bool isBounceChild = false,
        bool locksDirectDamage = false,
        int directDamageBonus = 0,
        TowerUpgradeDefinition explosiveArrowSourceUpgrade = null,
        EffectDefinition explosiveArrowEffect = null,
        TowerUpgradeDefinition explosiveShellSourceUpgrade = null,
        EffectDefinition explosiveShellEffect = null,
        float bounceSearchRadius = 0f,
        int remainingBounceCount = 0,
        float bounceArcHeight = 0f,
        TargetSelectionType bounceTargetSelectionType = TargetSelectionType.Nearest,
        int bounceDamage = 0,
        TowerUpgradeDefinition blastRoundsSourceUpgrade = null,
        EffectDefinition blastRoundsEffect = null)
    {
        CanPierce = canPierce;
        MaxPierceHitCount = maxPierceHitCount;
        IsBounceChild = isBounceChild;
        LocksDirectDamage = locksDirectDamage;
        DirectDamageBonus = directDamageBonus;
        ExplosiveArrowSourceUpgrade = explosiveArrowSourceUpgrade;
        ExplosiveArrowEffect = explosiveArrowEffect;
        ExplosiveShellSourceUpgrade = explosiveShellSourceUpgrade;
        ExplosiveShellEffect = explosiveShellEffect;
        BounceSearchRadius = bounceSearchRadius;
        RemainingBounceCount = remainingBounceCount;
        BounceArcHeight = bounceArcHeight;
        BounceTargetSelectionType = bounceTargetSelectionType;
        BounceDamage = bounceDamage;
        BlastRoundsSourceUpgrade = blastRoundsSourceUpgrade;
        BlastRoundsEffect = blastRoundsEffect;
    }
}
