using UnityEngine;

public struct ProjectileImpactContext
{
    public ProjectileImpactContext(
        TowerInstance sourceTower,
        MonsterBehaviour targetMonster,
        Vector3 impactPosition,
        int attackDamage,
        EffectDefinition impactEffectDefinition)
    {
        SourceTower = sourceTower;
        TargetMonster = targetMonster;
        ImpactPosition = impactPosition;
        AttackDamage = attackDamage;
        ImpactEffectDefinition = impactEffectDefinition;
    }

    public TowerInstance SourceTower { get; }
    public MonsterBehaviour TargetMonster { get; }
    public Vector3 ImpactPosition { get; }
    public int AttackDamage { get; }
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

public readonly struct ProjectileRuntimeObservation
{
    private ProjectileRuntimeObservation(
        ProjectileRuntimeObservationType observationType,
        TowerInstance sourceTower,
        ProjectileFlightType flightType,
        bool isChildProjectile,
        bool hasTargetMonster,
        ProjectileArcImpactResolutionType arcImpactResolutionType)
    {
        ObservationType = observationType;
        SourceTower = sourceTower;
        FlightType = flightType;
        IsChildProjectile = isChildProjectile;
        HasTargetMonster = hasTargetMonster;
        ArcImpactResolutionType = arcImpactResolutionType;
    }

    public ProjectileRuntimeObservationType ObservationType { get; }
    public TowerInstance SourceTower { get; }
    public ProjectileFlightType FlightType { get; }
    public bool IsChildProjectile { get; }
    public bool HasTargetMonster { get; }
    public ProjectileArcImpactResolutionType ArcImpactResolutionType { get; }

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
                ProjectileArcImpactResolutionType.NotApplicable);
    }

    public static ProjectileRuntimeObservation CreateImpact(
        TowerInstance sourceTower,
        ProjectileFlightType flightType,
        bool isChildProjectile,
        bool hasTargetMonster,
        ProjectileArcImpactResolutionType arcImpactResolutionType)
    {
        return new ProjectileRuntimeObservation(
            ProjectileRuntimeObservationType.Impacted,
            sourceTower,
            flightType,
            isChildProjectile,
            hasTargetMonster,
            arcImpactResolutionType);
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
                ProjectileArcImpactResolutionType.NotApplicable);
    }
}
#endif
