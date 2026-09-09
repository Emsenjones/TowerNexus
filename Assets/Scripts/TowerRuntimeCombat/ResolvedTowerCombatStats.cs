public readonly struct ResolvedTowerCombatStats
{
    public ResolvedTowerCombatStats(
        float attackRange,
        float attackCycleDuration,
        int levelBasicDamage,
        float resolvedBasicDamage,
        int damageBonus,
        float magicOrbRotationSpeed,
        float droneBurstCooldown)
    {
        AttackRange = attackRange;
        AttackCycleDuration = attackCycleDuration;
        LevelBasicDamage = levelBasicDamage;
        ResolvedBasicDamage = resolvedBasicDamage;
        DamageBonus = damageBonus;
        MagicOrbRotationSpeed = magicOrbRotationSpeed;
        DroneBurstCooldown = droneBurstCooldown;
    }

    public float AttackRange { get; }
    public float AttackCycleDuration { get; }
    public int LevelBasicDamage { get; }
    public float ResolvedBasicDamage { get; }
    public int DamageBonus { get; }
    public float MagicOrbRotationSpeed { get; }
    public float DroneBurstCooldown { get; }
}

public enum TowerDamageSourceType
{
    PrimaryDirect = 0,
    AdditionalDirect = 1,
    BounceDirect = 2,
    FinalDiveDirect = 3,
    BehaviourEffect = 4
}

public readonly struct TowerDamageSourceIdentity
{
    private TowerDamageSourceIdentity(
        TowerDamageSourceType sourceType,
        EffectDefinition effectDefinition,
        int actionOrdinal)
    {
        SourceType = sourceType;
        EffectDefinition = effectDefinition;
        ActionOrdinal = actionOrdinal;
    }

    public TowerDamageSourceType SourceType { get; }
    public EffectDefinition EffectDefinition { get; }
    public int ActionOrdinal { get; }
    public bool IsValid =>
        SourceType != TowerDamageSourceType.BehaviourEffect ||
        (EffectDefinition != null && ActionOrdinal >= 0);

    public static TowerDamageSourceIdentity PrimaryDirect =>
        new TowerDamageSourceIdentity(
            TowerDamageSourceType.PrimaryDirect,
            null,
            -1);

    public static TowerDamageSourceIdentity AdditionalDirect =>
        new TowerDamageSourceIdentity(
            TowerDamageSourceType.AdditionalDirect,
            null,
            -1);

    public static TowerDamageSourceIdentity BounceDirect =>
        new TowerDamageSourceIdentity(
            TowerDamageSourceType.BounceDirect,
            null,
            -1);

    public static TowerDamageSourceIdentity FinalDiveDirect =>
        new TowerDamageSourceIdentity(
            TowerDamageSourceType.FinalDiveDirect,
            null,
            -1);

    public static TowerDamageSourceIdentity BehaviourEffect(
        EffectDefinition effectDefinition,
        int actionOrdinal)
    {
        return new TowerDamageSourceIdentity(
            TowerDamageSourceType.BehaviourEffect,
            effectDefinition,
            actionOrdinal);
    }
}

public readonly struct TowerOwnedDamageResolution
{
    internal TowerOwnedDamageResolution(
        TowerInstance sourceTower,
        TowerFamily towerFamily,
        int level,
        int levelBasicDamage,
        float rawDamageBonus,
        float resolvedBasicDamage,
        TowerDamageSourceIdentity damageSourceIdentity,
        float damageScale,
        float rawProduct,
        int finalDamage)
    {
        SourceTower = sourceTower;
#if UNITY_EDITOR
        DiagnosticSource = CombatDiagnosticScope.Enabled(CombatDiagnosticScope.CurrentIdentity)
            ? CombatDiagnosticScope.Capture(CombatDiagnosticScope.CurrentIdentity,
                () => new CombatDiagnosticSource(sourceTower, damageSourceIdentity.EffectDefinition)) : default;
#endif
        TowerFamily = towerFamily;
        Level = level;
        LevelBasicDamage = levelBasicDamage;
        RawDamageBonus = rawDamageBonus;
        ResolvedBasicDamage = resolvedBasicDamage;
        DamageSourceIdentity = damageSourceIdentity;
        DamageScale = damageScale;
        RawProduct = rawProduct;
        FinalDamage = finalDamage;
    }

#if UNITY_EDITOR
    internal CombatDiagnosticSource DiagnosticSource { get; }
#endif
    public TowerInstance SourceTower { get; }
    public TowerFamily TowerFamily { get; }
    public int Level { get; }
    public int LevelBasicDamage { get; }
    public float RawDamageBonus { get; }
    public float ResolvedBasicDamage { get; }
    public TowerDamageSourceIdentity DamageSourceIdentity { get; }
    public float DamageScale { get; }
    public float RawProduct { get; }
    public int FinalDamage { get; }
}

public enum TowerOwnedDamageFailureReason
{
    None = 0,
    MissingSourceTower = 1,
    MissingTowerDefinition = 2,
    InvalidDamageSourceIdentity = 3,
    InvalidDamageScale = 4,
    InvalidLevelConfiguration = 5,
    InvalidResolvedBasicDamage = 6,
    InvalidRawProduct = 7,
    NonPositiveFinalDamage = 8
}

public readonly struct TowerOwnedDamageResolutionObservation
{
    internal TowerOwnedDamageResolutionObservation(
        TowerInstance sourceTower,
        TowerDamageSourceIdentity damageSourceIdentity,
        float damageScale,
        TowerOwnedDamageFailureReason failureReason,
        TowerOwnedDamageResolution resolution)
    {
        SourceTower = sourceTower;
#if UNITY_EDITOR
        DiagnosticSource = CombatDiagnosticScope.Enabled(CombatDiagnosticScope.CurrentIdentity)
            ? CombatDiagnosticScope.Capture(CombatDiagnosticScope.CurrentIdentity,
                () => new CombatDiagnosticSource(sourceTower, damageSourceIdentity.EffectDefinition)) : default;
#endif
        DamageSourceIdentity = damageSourceIdentity;
        DamageScale = damageScale;
        FailureReason = failureReason;
        Resolution = resolution;
    }

#if UNITY_EDITOR
    internal CombatDiagnosticSource DiagnosticSource { get; }
#endif
    public TowerInstance SourceTower { get; }
    public TowerDamageSourceIdentity DamageSourceIdentity { get; }
    public float DamageScale { get; }
    public TowerOwnedDamageFailureReason FailureReason { get; }
    public TowerOwnedDamageResolution Resolution { get; }
    public bool IsResolved => FailureReason == TowerOwnedDamageFailureReason.None;
}

public readonly struct TowerOwnedDamageApplicationObservation
{
    internal TowerOwnedDamageApplicationObservation(
        TowerOwnedDamageResolution resolution,
        int successfulApplicationCount)
    {
        Resolution = resolution;
        SuccessfulApplicationCount = successfulApplicationCount;
    }

    public TowerOwnedDamageResolution Resolution { get; }
    public int SuccessfulApplicationCount { get; }
}

public readonly struct TowerOwnedTargetDamageObservation
{
    internal TowerOwnedTargetDamageObservation(
        TowerOwnedDamageResolution resolution,
        MonsterBehaviour target,
        int appliedDamage,
        bool killingBlow)
    {
        Resolution = resolution;
        Target = target;
        AppliedDamage = appliedDamage;
        KillingBlow = killingBlow;
    }

    public TowerOwnedDamageResolution Resolution { get; }
    public MonsterBehaviour Target { get; }
    public int AppliedDamage { get; }
    public bool KillingBlow { get; }
}
