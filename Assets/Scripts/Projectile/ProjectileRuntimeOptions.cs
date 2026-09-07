public readonly struct ProjectileRuntimeOptions
{
    public bool AllowsElementalApplication { get; }
    public bool CanPierce { get; }
    public int MaxPierceHitCount { get; }
    public bool IsBounceChild { get; }
    public bool IsInitialRelease => !IsBounceChild;
    public TowerUpgradeDefinition ExplosiveArrowSourceUpgrade { get; }
    public EffectDefinition ExplosiveArrowEffect { get; }
    public TowerUpgradeDefinition ExplosiveShellSourceUpgrade { get; }
    public EffectDefinition ExplosiveShellEffect { get; }
    public float BounceSearchRadius { get; }
    public int RemainingBounceCount { get; }
    public float BounceArcHeight { get; }
    public TargetSelectionType BounceTargetSelectionType { get; }
    public float BounceDamageScale { get; }
    public TowerUpgradeDefinition BlastRoundsSourceUpgrade { get; }
    public EffectDefinition BlastRoundsEffect { get; }
    public int SourceDroneInstanceId { get; }
    public long DroneBurstId { get; }
    public bool IsAdditionalDrone { get; }
    public bool IsOpeningShotSlot { get; }
    public ElementalOpportunityProvenance ElementalOpportunityProvenance { get; }
    public ElementalOpportunityMemberIdentity ElementalOpportunityMemberIdentity { get; }
    public int ElementalResultOrdinal { get; }

    public ProjectileRuntimeOptions(
        bool allowsElementalApplication,
        bool canPierce,
        int maxPierceHitCount,
        bool isBounceChild = false,
        TowerUpgradeDefinition explosiveArrowSourceUpgrade = null,
        EffectDefinition explosiveArrowEffect = null,
        TowerUpgradeDefinition explosiveShellSourceUpgrade = null,
        EffectDefinition explosiveShellEffect = null,
        float bounceSearchRadius = 0f,
        int remainingBounceCount = 0,
        float bounceArcHeight = 0f,
        TargetSelectionType bounceTargetSelectionType = TargetSelectionType.Nearest,
        float bounceDamageScale = 0f,
        TowerUpgradeDefinition blastRoundsSourceUpgrade = null,
        EffectDefinition blastRoundsEffect = null,
        int sourceDroneInstanceId = 0,
        long droneBurstId = 0,
        bool isAdditionalDrone = false,
        bool isOpeningShotSlot = false,
        ElementalOpportunityProvenance elementalOpportunityProvenance =
            ElementalOpportunityProvenance.None,
        ElementalOpportunityMemberIdentity elementalOpportunityMemberIdentity =
            ElementalOpportunityMemberIdentity.NotApplicable,
        int elementalResultOrdinal = 0)
    {
        AllowsElementalApplication = allowsElementalApplication;
        CanPierce = canPierce;
        MaxPierceHitCount = maxPierceHitCount;
        IsBounceChild = isBounceChild;
        ExplosiveArrowSourceUpgrade = explosiveArrowSourceUpgrade;
        ExplosiveArrowEffect = explosiveArrowEffect;
        ExplosiveShellSourceUpgrade = explosiveShellSourceUpgrade;
        ExplosiveShellEffect = explosiveShellEffect;
        BounceSearchRadius = bounceSearchRadius;
        RemainingBounceCount = remainingBounceCount;
        BounceArcHeight = bounceArcHeight;
        BounceTargetSelectionType = bounceTargetSelectionType;
        BounceDamageScale = bounceDamageScale;
        BlastRoundsSourceUpgrade = blastRoundsSourceUpgrade;
        BlastRoundsEffect = blastRoundsEffect;
        SourceDroneInstanceId = sourceDroneInstanceId;
        DroneBurstId = droneBurstId;
        IsAdditionalDrone = isAdditionalDrone;
        IsOpeningShotSlot = isOpeningShotSlot;
        ElementalOpportunityProvenance = elementalOpportunityProvenance;
        ElementalOpportunityMemberIdentity =
            elementalOpportunityMemberIdentity;
        ElementalResultOrdinal = elementalResultOrdinal;
    }
}
