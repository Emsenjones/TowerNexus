using UnityEngine;

public enum ElementalOpportunityProvenance
{
    None = 0,
    ArcherArrow = 1,
    ArcherExplosiveArrow = 2,
    CannonShell = 3,
    CannonExplosiveShell = 4,
    MagicOrb = 5,
    MagicArcaneDetonation = 6,
    MagicArcaneField = 7,
    DroneOpeningProjectile = 8,
    DroneLaterProjectile = 9,
    DroneBlastRounds = 10,
    DroneFinalDive = 11
}

public enum ElementalOpportunityMemberIdentity
{
    NotApplicable = 0,
    Primary = 1,
    Additional = 2
}

public enum ElementalOpportunityResultRole
{
    None = 0,
    InitialDirect = 1,
    PiercingContinuation = 2,
    BounceChild = 3,
    AreaResult = 4,
    PersistentTick = 5,
    CompletionResult = 6,
    BlastArea = 7,
    FinalDiveDirect = 8,
    FinalDiveExplosion = 9
}

public readonly struct ElementalOpportunityDiagnosticContext
{
    public ElementalOpportunityDiagnosticContext(
        ElementalOpportunityProvenance provenance,
        ElementalOpportunityMemberIdentity memberIdentity,
        ElementalOpportunityResultRole resultRole,
        int resultOrdinal,
        bool topologyAuthorized,
        bool observeResolvedTargetsAsCandidates = false)
    {
        Provenance = provenance;
        MemberIdentity = memberIdentity;
        ResultRole = resultRole;
        ResultOrdinal = Mathf.Max(0, resultOrdinal);
        TopologyAuthorized = topologyAuthorized;
        ObserveResolvedTargetsAsCandidates =
            observeResolvedTargetsAsCandidates;
    }

    public ElementalOpportunityProvenance Provenance { get; }
    public ElementalOpportunityMemberIdentity MemberIdentity { get; }
    public ElementalOpportunityResultRole ResultRole { get; }
    public int ResultOrdinal { get; }
    public bool TopologyAuthorized { get; }
    public bool ObserveResolvedTargetsAsCandidates { get; }
    public bool IsValid =>
        Provenance != ElementalOpportunityProvenance.None &&
        ResultRole != ElementalOpportunityResultRole.None;

    public ElementalOpportunityDiagnosticContext WithResultOrdinal(
        int resultOrdinal)
    {
        return new ElementalOpportunityDiagnosticContext(
            Provenance,
            MemberIdentity,
            ResultRole,
            resultOrdinal,
            TopologyAuthorized,
            ObserveResolvedTargetsAsCandidates);
    }
}

public readonly struct EffectTriggerContext
{
    public EffectTriggerContext(
        BattleCombatBinding battleBinding,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade,
        MonsterBehaviour targetMonster,
        bool hasTriggerPosition,
        Vector3 triggerPosition,
        bool allowsElementalApplication,
        bool allowsLifecycleOwnerTarget = false,
        int requestedStackUnits = 0,
        ElementalOpportunityDiagnosticContext elementalOpportunityDiagnostics =
            default,
        ElementalApplicationTransaction elementalApplicationTransaction = null,
        bool requiresCommittedActionForExecutionVfx = false,
        BuffRemovalPermission removalPermission = null)
    {
        BattleBinding = battleBinding;
        RemovalPermission = removalPermission;
        SourceTower = sourceTower;
        SourceUpgrade = sourceUpgrade;
        TargetMonster = targetMonster;
        HasTriggerPosition = hasTriggerPosition;
        TriggerPosition = triggerPosition;
        AllowsElementalApplication = allowsElementalApplication;
        AllowsLifecycleOwnerTarget = allowsLifecycleOwnerTarget;
        RequestedStackUnits = requestedStackUnits;
        ElementalOpportunityDiagnostics = elementalOpportunityDiagnostics;
        ElementalApplicationTransaction = elementalApplicationTransaction;
        RequiresCommittedActionForExecutionVfx =
            requiresCommittedActionForExecutionVfx;
    }

    public BattleCombatBinding BattleBinding { get; }
    internal BuffRemovalPermission RemovalPermission { get; }
    public TowerInstance SourceTower { get; }
    public TowerUpgradeDefinition SourceUpgrade { get; }
    public MonsterBehaviour TargetMonster { get; }
    public bool HasTriggerPosition { get; }
    // TriggerPosition is valid only when HasTriggerPosition is true.
    // Producers provide the relevant hit, impact, owner, or zone position.
    public Vector3 TriggerPosition { get; }
    public bool AllowsElementalApplication { get; }
    public int RequestedStackUnits { get; }
    public ElementalOpportunityDiagnosticContext
        ElementalOpportunityDiagnostics { get; }
    public ElementalApplicationTransaction ElementalApplicationTransaction { get; }
    public bool RequiresCommittedActionForExecutionVfx { get; }
    // Removed lifecycle Effects may release owner-bound state after the owner
    // has stopped being a gameplay target. This does not reopen combat targeting.
    public bool AllowsLifecycleOwnerTarget { get; }
}
