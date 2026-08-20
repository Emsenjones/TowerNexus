using UnityEngine;

public struct ProjectileImpactContext
{
    public ProjectileImpactContext(
        TowerInstance sourceTower,
        MonsterBehaviour targetMonster,
        Vector3 impactPosition,
        TowerOwnedDamageResolution damageResolution,
        EffectDefinition impactEffectDefinition)
    {
        SourceTower = sourceTower;
        TargetMonster = targetMonster;
        ImpactPosition = impactPosition;
        DamageResolution = damageResolution;
        ImpactEffectDefinition = impactEffectDefinition;
    }

    public TowerInstance SourceTower { get; }
    public MonsterBehaviour TargetMonster { get; }
    public Vector3 ImpactPosition { get; }
    public TowerOwnedDamageResolution DamageResolution { get; }
    public EffectDefinition ImpactEffectDefinition { get; }
}

#if UNITY_EDITOR
public enum ProjectileRuntimeObservationType
{
    Released = 0,
    Impacted = 1,
    EndedWithoutImpact = 2
}

public enum ProjectileArcImpactResolutionType
{
    NotApplicable = 0,
    IntendedTarget = 1,
    FallbackTarget = 2,
    PositionOnlyIntendedInvalid = 3,
    PositionOnlyIntendedOutOfRange = 4,
    PositionOnlyWithoutIntendedTarget = 5
}

public enum ProjectileArcMemberType
{
    NotApplicable = 0,
    PrimaryInitial = 1,
    AdditionalInitial = 2,
    BounceChild = 3
}

public readonly struct ProjectileArcTargetRelationObservation
{
    public ProjectileArcTargetRelationObservation(
        ProjectileArcMemberType memberType,
        MonsterBehaviour intendedTarget,
        MonsterBehaviour resolvedTarget,
        MonsterBehaviour nearestOtherTarget,
        float confirmationToReleaseSeconds,
        float releaseToImpactSeconds,
        float plannedTravelTimeSeconds,
        float hitDistanceThreshold,
        bool hasIntendedAtRelease,
        float landingToIntendedDistanceAtRelease,
        float intendedMoveSpeedAtRelease,
        bool hasIntendedAtImpact,
        float landingToIntendedDistanceAtImpact,
        float intendedMoveSpeedAtImpact,
        bool hasResolvedTargetAtImpact,
        float landingToResolvedTargetDistanceAtImpact,
        float resolvedTargetMoveSpeedAtImpact,
        bool hasNearestOtherTargetAtImpact,
        float landingToNearestOtherTargetDistanceAtImpact,
        float nearestOtherTargetMoveSpeedAtImpact)
    {
        MemberType = memberType;
        IntendedTarget = intendedTarget;
        ResolvedTarget = resolvedTarget;
        NearestOtherTarget = nearestOtherTarget;
        ConfirmationToReleaseSeconds = confirmationToReleaseSeconds;
        ReleaseToImpactSeconds = releaseToImpactSeconds;
        PlannedTravelTimeSeconds = plannedTravelTimeSeconds;
        HitDistanceThreshold = hitDistanceThreshold;
        HasIntendedAtRelease = hasIntendedAtRelease;
        LandingToIntendedDistanceAtRelease =
            landingToIntendedDistanceAtRelease;
        IntendedMoveSpeedAtRelease = intendedMoveSpeedAtRelease;
        HasIntendedAtImpact = hasIntendedAtImpact;
        LandingToIntendedDistanceAtImpact =
            landingToIntendedDistanceAtImpact;
        IntendedMoveSpeedAtImpact = intendedMoveSpeedAtImpact;
        HasResolvedTargetAtImpact = hasResolvedTargetAtImpact;
        LandingToResolvedTargetDistanceAtImpact =
            landingToResolvedTargetDistanceAtImpact;
        ResolvedTargetMoveSpeedAtImpact = resolvedTargetMoveSpeedAtImpact;
        HasNearestOtherTargetAtImpact = hasNearestOtherTargetAtImpact;
        LandingToNearestOtherTargetDistanceAtImpact =
            landingToNearestOtherTargetDistanceAtImpact;
        NearestOtherTargetMoveSpeedAtImpact =
            nearestOtherTargetMoveSpeedAtImpact;
    }

    public ProjectileArcMemberType MemberType { get; }
    public MonsterBehaviour IntendedTarget { get; }
    public MonsterBehaviour ResolvedTarget { get; }
    public MonsterBehaviour NearestOtherTarget { get; }
    public float ConfirmationToReleaseSeconds { get; }
    public float ReleaseToImpactSeconds { get; }
    public float PlannedTravelTimeSeconds { get; }
    public float HitDistanceThreshold { get; }
    public bool HasIntendedAtRelease { get; }
    public float LandingToIntendedDistanceAtRelease { get; }
    public float IntendedMoveSpeedAtRelease { get; }
    public bool HasIntendedAtImpact { get; }
    public float LandingToIntendedDistanceAtImpact { get; }
    public float IntendedMoveSpeedAtImpact { get; }
    public bool HasResolvedTargetAtImpact { get; }
    public float LandingToResolvedTargetDistanceAtImpact { get; }
    public float ResolvedTargetMoveSpeedAtImpact { get; }
    public bool HasNearestOtherTargetAtImpact { get; }
    public float LandingToNearestOtherTargetDistanceAtImpact { get; }
    public float NearestOtherTargetMoveSpeedAtImpact { get; }
}

public readonly struct ProjectileRuntimeObservation
{
    private ProjectileRuntimeObservation(
        ProjectileRuntimeObservationType observationType,
        TowerInstance sourceTower,
        ProjectileFlightType flightType,
        bool isChildProjectile,
        bool hasTargetMonster,
        ProjectileArcImpactResolutionType arcImpactResolutionType,
        ProjectileArcTargetRelationObservation arcTargetRelation)
    {
        ObservationType = observationType;
        SourceTower = sourceTower;
        FlightType = flightType;
        IsChildProjectile = isChildProjectile;
        HasTargetMonster = hasTargetMonster;
        ArcImpactResolutionType = arcImpactResolutionType;
        ArcTargetRelation = arcTargetRelation;
    }

    public ProjectileRuntimeObservationType ObservationType { get; }
    public TowerInstance SourceTower { get; }
    public ProjectileFlightType FlightType { get; }
    public bool IsChildProjectile { get; }
    public bool HasTargetMonster { get; }
    public ProjectileArcImpactResolutionType ArcImpactResolutionType { get; }
    public ProjectileArcTargetRelationObservation ArcTargetRelation { get; }

    public static ProjectileRuntimeObservation CreateReleased(
        TowerInstance sourceTower,
        ProjectileFlightType flightType,
        bool isChildProjectile)
    {
        return new ProjectileRuntimeObservation(
            ProjectileRuntimeObservationType.Released,
            sourceTower,
            flightType,
            isChildProjectile,
            hasTargetMonster: false,
            arcImpactResolutionType:
                ProjectileArcImpactResolutionType.NotApplicable,
            arcTargetRelation: default);
    }

    public static ProjectileRuntimeObservation CreateImpact(
        TowerInstance sourceTower,
        ProjectileFlightType flightType,
        bool isChildProjectile,
        bool hasTargetMonster,
        ProjectileArcImpactResolutionType arcImpactResolutionType,
        ProjectileArcTargetRelationObservation arcTargetRelation)
    {
        return new ProjectileRuntimeObservation(
            ProjectileRuntimeObservationType.Impacted,
            sourceTower,
            flightType,
            isChildProjectile,
            hasTargetMonster,
            arcImpactResolutionType,
            arcTargetRelation);
    }

    public static ProjectileRuntimeObservation CreateEndedWithoutImpact(
        TowerInstance sourceTower,
        ProjectileFlightType flightType,
        bool isChildProjectile)
    {
        return new ProjectileRuntimeObservation(
            ProjectileRuntimeObservationType.EndedWithoutImpact,
            sourceTower,
            flightType,
            isChildProjectile,
            hasTargetMonster: false,
            arcImpactResolutionType:
                ProjectileArcImpactResolutionType.NotApplicable,
            arcTargetRelation: default);
    }
}
#endif
