#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
internal sealed class CombatEntityAccumulator
{
    private readonly Func<MonsterBehaviour,int> getSpawnOrdinal;
    internal CombatEntityAccumulator(Func<MonsterBehaviour,int> getSpawnOrdinal) { this.getSpawnOrdinal = getSpawnOrdinal; }
    private int GetSpawnOrdinal(MonsterBehaviour monster) => getSpawnOrdinal(monster);
    private enum SpawnOrdinalRelation { Unknown, ImmediateLaterSpawn, ImmediateEarlierSpawn, NonAdjacent }
    internal readonly Dictionary<int, ProjectileRuntimeAggregate>
        projectileRuntimeByTowerInstanceId =
            new Dictionary<int, ProjectileRuntimeAggregate>();
    internal readonly Dictionary<int, Dictionary<int, DroneRuntimeAggregate>>
        droneRuntimeByTowerInstanceId =
            new Dictionary<int, Dictionary<int, DroneRuntimeAggregate>>();
    internal sealed class FloatMetricAggregate
    {
        public int Samples { get; private set; }
        public float Sum { get; private set; }
        public float Minimum { get; private set; } = float.PositiveInfinity;
        public float Maximum { get; private set; } = float.NegativeInfinity;

        public void Add(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                return;
            }

            Samples++;
            Sum += value;
            Minimum = Mathf.Min(Minimum, value);
            Maximum = Mathf.Max(Maximum, value);
        }
    }
    internal sealed class ArcTargetRelationAggregate
    {
        public int SampledInitialImpacts { get; set; }
        public int PrimaryIntendedTargetImpacts { get; set; }
        public int PrimaryFallbackTargetImpacts { get; set; }
        public int PrimaryPositionOnlyImpacts { get; set; }
        public int AdditionalIntendedTargetImpacts { get; set; }
        public int AdditionalFallbackTargetImpacts { get; set; }
        public int AdditionalPositionOnlyImpacts { get; set; }
        public int FallbackImmediateLaterSpawnImpacts { get; set; }
        public int FallbackImmediateEarlierSpawnImpacts { get; set; }
        public int FallbackNonAdjacentImpacts { get; set; }
        public int FallbackUnknownRelationImpacts { get; set; }
        public int PositionOnlyNearestOtherAvailable { get; set; }
        public int PositionOnlyNearestOtherImmediateLaterSpawn { get; set; }
        public int PositionOnlyNearestOtherImmediateEarlierSpawn { get; set; }
        public int PositionOnlyNearestOtherNonAdjacent { get; set; }
        public int PositionOnlyNearestOtherUnknownRelation { get; set; }
        public int PositionOnlyWithoutNearestOther { get; set; }
        public float MinimumHitDistanceThreshold { get; set; } =
            float.PositiveInfinity;
        public float MaximumHitDistanceThreshold { get; set; }
        public FloatMetricAggregate ConfirmationToReleaseSeconds { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate ReleaseToImpactSeconds { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate PlannedTravelTimeSeconds { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate LandingToIntendedDistanceAtRelease { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate IntendedMoveSpeedAtRelease { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate LandingToIntendedDistanceAtImpact { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate IntendedMoveSpeedAtImpact { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate LandingToFallbackDistanceAtImpact { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate FallbackMoveSpeedAtImpact { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate LandingToPositionOnlyNearestOtherDistance { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate PositionOnlyNearestOtherMoveSpeedAtImpact { get; } =
            new FloatMetricAggregate();
        public List<CombatBalanceArcTargetRelationSampleJson> Samples { get; } =
            new List<CombatBalanceArcTargetRelationSampleJson>();
    }
    internal sealed class ProjectileRuntimeAggregate
    {
        public int ProjectilesReleased { get; set; }
        public int InitialProjectilesReleased { get; set; }
        public int ChildProjectilesReleased { get; set; }
        public int ArcProjectilesReleased { get; set; }
        public int ArcTargetResolvedImpacts { get; set; }
        public int ArcIntendedTargetImpacts { get; set; }
        public int ArcFallbackTargetImpacts { get; set; }
        public int ArcPositionOnlyImpacts { get; set; }
        public int ArcPositionOnlyIntendedInvalid { get; set; }
        public int ArcPositionOnlyIntendedOutOfRange { get; set; }
        public int ArcPositionOnlyWithoutIntendedTarget { get; set; }
        public int ArcEndedWithoutImpact { get; set; }
        public ArcTargetRelationAggregate ArcTargetRelation { get; } =
            new ArcTargetRelationAggregate();
    }
    internal sealed class DroneBurstAggregate
    {
        public DroneBurstAggregate(long burstId)
        {
            BurstId = burstId;
        }

        public long BurstId { get; }
        public int Started { get; set; }
        public int OpeningProjectilesReleased { get; set; }
        public int ElementalEligibleOpeningProjectilesReleased { get; set; }
        public int OpeningProjectileDirectHits { get; set; }
        public int ElementalEligibleOpeningProjectileDirectHits { get; set; }
        public int OpeningProjectilesEndedWithoutImpact { get; set; }
        public int LaterProjectilesReleased { get; set; }
        public int LaterProjectileDirectHits { get; set; }
        public int LaterProjectilesEndedWithoutImpact { get; set; }
    }
    internal sealed class DroneRuntimeAggregate
    {
        public DroneRuntimeAggregate(
            int sourceDroneInstanceId,
            bool isAdditionalAttackEntity)
        {
            SourceDroneInstanceId = sourceDroneInstanceId;
            IsAdditionalAttackEntity = isAdditionalAttackEntity;
        }

        public int SourceDroneInstanceId { get; }
        public bool IsAdditionalAttackEntity { get; }
        public bool DiagnosticsConsistent { get; set; } = true;
        public int InitializedCount { get; set; }
        public int LaunchCompletedCount { get; set; }
        public int InvalidTargetLossCount { get; set; }
        public int OutOfRangeTargetLossCount { get; set; }
        public int ImmediateRetargetCount { get; set; }
        public int HoldingEntryCount { get; set; }
        public int HoldingExitCount { get; set; }
        public int OrbitEntryCompletedCount { get; set; }
        public int BatteryDepletedCount { get; set; }
        public int FinalDiveEnteredCount { get; set; }
        public int FinalDiveCompletedCount { get; set; }
        public int CompletionCount { get; set; }
        public float InitializedAtTime { get; set; }
        public float HoldingEnteredAtTime { get; set; }
        public float CompletedAtTime { get; set; }
        public float HoldingTimeSeconds { get; set; }
        public bool IsHolding { get; set; }
        public bool HasCompletedOrbitEntry { get; set; }
        public bool HasPendingTargetLoss { get; set; }
        public bool IsCompleted { get; set; }
        public DroneRuntimeState CurrentState { get; set; } =
            DroneRuntimeState.Launching;
        public DroneCompletionReason CompletionReason { get; set; } =
            DroneCompletionReason.None;
        public Dictionary<long, DroneBurstAggregate> Bursts { get; } =
            new Dictionary<long, DroneBurstAggregate>();
    }
    internal void HandleProjectileRuntimeObserved(
        ProjectileRuntimeObservation observation)
    {
        if (CombatDiagnosticScope.Source(observation.SourceTower).Id == 0)
        {
            return;
        }

        int towerInstanceId = CombatDiagnosticScope.Source(observation.SourceTower).Id;

        if (!projectileRuntimeByTowerInstanceId.TryGetValue(
                towerInstanceId,
                out ProjectileRuntimeAggregate aggregate))
        {
            aggregate = new ProjectileRuntimeAggregate();
            projectileRuntimeByTowerInstanceId.Add(
                towerInstanceId,
                aggregate);
        }

        switch (observation.ObservationType)
        {
            case ProjectileRuntimeObservationType.Released:
                aggregate.ProjectilesReleased++;

                if (observation.IsChildProjectile)
                {
                    aggregate.ChildProjectilesReleased++;
                }
                else
                {
                    aggregate.InitialProjectilesReleased++;
                }

                if (observation.FlightType == ProjectileFlightType.Arc)
                {
                    aggregate.ArcProjectilesReleased++;
                }

                break;
            case ProjectileRuntimeObservationType.Impacted:
                if (observation.FlightType != ProjectileFlightType.Arc)
                {
                    break;
                }

                if (observation.HasTargetMonster)
                {
                    aggregate.ArcTargetResolvedImpacts++;
                }
                else
                {
                    aggregate.ArcPositionOnlyImpacts++;
                }

                switch (observation.ArcImpactResolutionType)
                {
                    case ProjectileArcImpactResolutionType.IntendedTarget:
                        aggregate.ArcIntendedTargetImpacts++;
                        break;
                    case ProjectileArcImpactResolutionType.FallbackTarget:
                        aggregate.ArcFallbackTargetImpacts++;
                        break;
                    case ProjectileArcImpactResolutionType.PositionOnlyIntendedInvalid:
                        aggregate.ArcPositionOnlyIntendedInvalid++;
                        break;
                    case ProjectileArcImpactResolutionType.PositionOnlyIntendedOutOfRange:
                        aggregate.ArcPositionOnlyIntendedOutOfRange++;
                        break;
                    case ProjectileArcImpactResolutionType.PositionOnlyWithoutIntendedTarget:
                        aggregate.ArcPositionOnlyWithoutIntendedTarget++;
                        break;
                }

                ConsumeArcTargetRelationObservation(
                    aggregate.ArcTargetRelation,
                    observation);

                break;
            case ProjectileRuntimeObservationType.EndedWithoutImpact:
                if (observation.FlightType == ProjectileFlightType.Arc)
                {
                    aggregate.ArcEndedWithoutImpact++;
                }

                break;
        }
    }

    private DroneRuntimeAggregate GetOrCreateDroneRuntimeAggregate(
        int towerInstanceId,
        int sourceDroneInstanceId,
        bool isAdditionalDrone)
    {


        if (!droneRuntimeByTowerInstanceId.TryGetValue(
                towerInstanceId,
                out Dictionary<int, DroneRuntimeAggregate> drones))
        {
            drones = new Dictionary<int, DroneRuntimeAggregate>();
            droneRuntimeByTowerInstanceId.Add(towerInstanceId, drones);
        }

        if (!drones.TryGetValue(
                sourceDroneInstanceId,
                out DroneRuntimeAggregate drone))
        {
            drone = new DroneRuntimeAggregate(
                sourceDroneInstanceId,
                isAdditionalDrone);
            drones.Add(sourceDroneInstanceId, drone);
        }
        else if (drone.IsAdditionalAttackEntity != isAdditionalDrone)
        {
            drone.DiagnosticsConsistent = false;
        }

        return drone;
    }

    internal void HandleDroneLifecycleRuntimeObserved(
        DroneLifecycleRuntimeObservation observation)
    {
        if (CombatDiagnosticScope.Source(observation.SourceTower).Id == 0 ||
            observation.SourceDroneInstanceId == 0)
        {
            return;
        }

        DroneRuntimeAggregate drone = GetOrCreateDroneRuntimeAggregate(
            CombatDiagnosticScope.Source(observation.SourceTower).Id,
            observation.SourceDroneInstanceId,
            observation.IsAdditionalDrone);

        switch (observation.ObservationType)
        {
            case DroneLifecycleRuntimeObservationType.Initialized:
                drone.InitializedCount++;
                drone.InitializedAtTime = observation.ObservedAtTime;
                drone.CurrentState = DroneRuntimeState.Launching;
                drone.DiagnosticsConsistent &=
                    drone.InitializedCount == 1 &&
                    observation.State == DroneRuntimeState.Launching &&
                    observation.CompletionReason == DroneCompletionReason.None;
                break;
            case DroneLifecycleRuntimeObservationType.LaunchCompleted:
                drone.LaunchCompletedCount++;
                drone.HasCompletedOrbitEntry = false;
                drone.DiagnosticsConsistent &=
                    drone.InitializedCount == 1 &&
                    !drone.IsCompleted &&
                    drone.LaunchCompletedCount == 1 &&
                    drone.CurrentState == DroneRuntimeState.Launching;
                break;
            case DroneLifecycleRuntimeObservationType.TargetLost:
                drone.HasPendingTargetLoss = true;
                drone.HasCompletedOrbitEntry = false;

                if (observation.TargetLossReason ==
                    DroneTargetLossReason.Invalid)
                {
                    drone.InvalidTargetLossCount++;
                }
                else if (observation.TargetLossReason ==
                         DroneTargetLossReason.OutOfRange)
                {
                    drone.OutOfRangeTargetLossCount++;
                }
                else
                {
                    drone.DiagnosticsConsistent = false;
                }

                drone.DiagnosticsConsistent &=
                    drone.InitializedCount == 1 &&
                    !drone.IsCompleted &&
                    !drone.IsHolding &&
                    (drone.CurrentState == DroneRuntimeState.Launching ||
                     drone.CurrentState == DroneRuntimeState.Orbiting);
                break;
            case DroneLifecycleRuntimeObservationType.ImmediateRetargeted:
                drone.ImmediateRetargetCount++;
                drone.DiagnosticsConsistent &=
                    drone.HasPendingTargetLoss &&
                    !drone.IsCompleted &&
                    !drone.IsHolding;
                drone.HasPendingTargetLoss = false;
                break;
            case DroneLifecycleRuntimeObservationType.HoldingEntered:
                drone.HoldingEntryCount++;
                drone.DiagnosticsConsistent &=
                    drone.HasPendingTargetLoss &&
                    !drone.IsCompleted &&
                    !drone.IsHolding &&
                    observation.State == DroneRuntimeState.Holding;
                drone.HasPendingTargetLoss = false;
                drone.IsHolding = true;
                drone.HasCompletedOrbitEntry = false;
                drone.HoldingEnteredAtTime = observation.ObservedAtTime;
                drone.CurrentState = DroneRuntimeState.Holding;
                break;
            case DroneLifecycleRuntimeObservationType.HoldingExited:
                drone.HoldingExitCount++;
                drone.DiagnosticsConsistent &=
                    drone.IsHolding &&
                    !drone.IsCompleted &&
                    observation.State == DroneRuntimeState.Orbiting;
                AccumulateDroneHoldingTime(drone, observation.ObservedAtTime);
                drone.IsHolding = false;
                drone.HasCompletedOrbitEntry = false;
                drone.CurrentState = DroneRuntimeState.Orbiting;
                break;
            case DroneLifecycleRuntimeObservationType.OrbitEntryCompleted:
                drone.OrbitEntryCompletedCount++;
                drone.DiagnosticsConsistent &=
                    !drone.IsCompleted &&
                    !drone.IsHolding &&
                    observation.State == DroneRuntimeState.Orbiting;
                drone.CurrentState = DroneRuntimeState.Orbiting;
                drone.HasCompletedOrbitEntry = true;
                break;
            case DroneLifecycleRuntimeObservationType.BatteryDepleted:
                drone.BatteryDepletedCount++;
                drone.DiagnosticsConsistent &=
                    drone.InitializedCount == 1 &&
                    !drone.IsCompleted &&
                    drone.BatteryDepletedCount == 1 &&
                    observation.BatteryRemaining <= 0f &&
                    (observation.State == DroneRuntimeState.Orbiting ||
                     observation.State == DroneRuntimeState.Holding);
                break;
            case DroneLifecycleRuntimeObservationType.FinalDiveEntered:
                drone.FinalDiveEnteredCount++;
                drone.DiagnosticsConsistent &=
                    drone.BatteryDepletedCount == 1 &&
                    !drone.IsCompleted &&
                    !drone.IsHolding &&
                    drone.FinalDiveEnteredCount == 1 &&
                    observation.State == DroneRuntimeState.FinalDiving;
                drone.CurrentState = DroneRuntimeState.FinalDiving;
                break;
            case DroneLifecycleRuntimeObservationType.FinalDiveCompleted:
                drone.FinalDiveCompletedCount++;
                drone.DiagnosticsConsistent &=
                    !drone.IsCompleted &&
                    drone.CurrentState == DroneRuntimeState.FinalDiving &&
                    drone.FinalDiveEnteredCount == 1 &&
                    drone.FinalDiveCompletedCount == 1;
                break;
            case DroneLifecycleRuntimeObservationType.Completed:
                drone.CompletionCount++;

                if (drone.IsHolding)
                {
                    AccumulateDroneHoldingTime(
                        drone,
                        observation.ObservedAtTime);
                    drone.IsHolding = false;
                }

                drone.CompletedAtTime = observation.ObservedAtTime;
                drone.CompletionReason = observation.CompletionReason;
                drone.DiagnosticsConsistent &=
                    drone.InitializedCount == 1 &&
                    !drone.IsCompleted &&
                    drone.CompletionCount == 1 &&
                    observation.CompletionReason !=
                        DroneCompletionReason.None &&
                    CompletionReasonMatchesLifecycle(drone);
                drone.IsCompleted = true;
                drone.HasPendingTargetLoss = false;
                break;
        }
    }

    private static void AccumulateDroneHoldingTime(
        DroneRuntimeAggregate drone,
        float observedAtTime)
    {
        drone.HoldingTimeSeconds += Mathf.Max(
            0f,
            observedAtTime - drone.HoldingEnteredAtTime);
        drone.HoldingEnteredAtTime = 0f;
    }

    private static bool CompletionReasonMatchesLifecycle(
        DroneRuntimeAggregate drone)
    {
        switch (drone.CompletionReason)
        {
            case DroneCompletionReason.BatteryAerialRetirement:
                return drone.BatteryDepletedCount == 1 &&
                       drone.FinalDiveEnteredCount == 0 &&
                       drone.FinalDiveCompletedCount == 0;
            case DroneCompletionReason.FinalDiveImpact:
                return drone.BatteryDepletedCount == 1 &&
                       drone.FinalDiveEnteredCount == 1 &&
                       drone.FinalDiveCompletedCount == 1;
            case DroneCompletionReason.TechnicalCleanup:
                return true;
            default:
                return false;
        }
    }

    internal void HandleDroneBurstRuntimeObserved(
        DroneBurstRuntimeObservation observation)
    {
        if (CombatDiagnosticScope.Source(observation.SourceTower).Id == 0 ||
            observation.SourceDroneInstanceId == 0 ||
            observation.Count <= 0)
        {
            return;
        }

        DroneRuntimeAggregate drone = GetOrCreateDroneRuntimeAggregate(
            CombatDiagnosticScope.Source(observation.SourceTower).Id,
            observation.SourceDroneInstanceId,
            observation.IsAdditionalDrone);

        if (drone.InitializedCount != 1)
        {
            drone.DiagnosticsConsistent = false;
        }

        if ((observation.ObservationType ==
                 DroneBurstRuntimeObservationType.BurstStarted ||
             observation.ObservationType ==
                 DroneBurstRuntimeObservationType.ProjectileReleased) &&
            (drone.IsHolding ||
             drone.IsCompleted ||
             !drone.HasCompletedOrbitEntry))
        {
            drone.DiagnosticsConsistent = false;
        }

        if ((observation.IsAdditionalDrone ||
             !observation.IsOpeningShotSlot) &&
            observation.AllowsElementalApplication)
        {
            drone.DiagnosticsConsistent = false;
        }

        if (observation.BurstId <= 0)
        {
            drone.DiagnosticsConsistent = false;
            return;
        }

        if (!drone.Bursts.TryGetValue(
                observation.BurstId,
                out DroneBurstAggregate burst))
        {
            burst = new DroneBurstAggregate(observation.BurstId);
            drone.Bursts.Add(observation.BurstId, burst);
        }

        switch (observation.ObservationType)
        {
            case DroneBurstRuntimeObservationType.BurstStarted:
                burst.Started += observation.Count;
                break;
            case DroneBurstRuntimeObservationType.ProjectileReleased:
                if (observation.IsOpeningShotSlot)
                {
                    burst.OpeningProjectilesReleased += observation.Count;

                    if (observation.AllowsElementalApplication)
                    {
                        burst.ElementalEligibleOpeningProjectilesReleased +=
                            observation.Count;
                    }
                }
                else
                {
                    burst.LaterProjectilesReleased += observation.Count;

                    if (observation.AllowsElementalApplication)
                    {
                        drone.DiagnosticsConsistent = false;
                    }
                }

                break;
            case DroneBurstRuntimeObservationType.ProjectileDirectHit:
                if (observation.IsOpeningShotSlot)
                {
                    burst.OpeningProjectileDirectHits += observation.Count;

                    if (observation.AllowsElementalApplication)
                    {
                        burst.ElementalEligibleOpeningProjectileDirectHits +=
                            observation.Count;
                    }
                }
                else
                {
                    burst.LaterProjectileDirectHits += observation.Count;
                }

                break;
            case DroneBurstRuntimeObservationType.ProjectileEndedWithoutImpact:
                if (observation.IsOpeningShotSlot)
                {
                    burst.OpeningProjectilesEndedWithoutImpact +=
                        observation.Count;
                }
                else
                {
                    burst.LaterProjectilesEndedWithoutImpact +=
                        observation.Count;
                }

                break;
        }
    }

    private void ConsumeArcTargetRelationObservation(
        ArcTargetRelationAggregate aggregate,
        ProjectileRuntimeObservation observation)
    {
        ProjectileArcTargetRelationObservation relation =
            observation.ArcTargetRelation;
        bool isPrimary =
            relation.MemberType == ProjectileArcMemberType.PrimaryInitial;
        bool isAdditional =
            relation.MemberType == ProjectileArcMemberType.AdditionalInitial;

        if (!isPrimary && !isAdditional)
        {
            return;
        }

        aggregate.SampledInitialImpacts++;
        aggregate.Samples.Add(CreateArcTargetRelationSampleJson(
            observation.ArcImpactResolutionType,
            relation));
        aggregate.MinimumHitDistanceThreshold = Mathf.Min(
            aggregate.MinimumHitDistanceThreshold,
            relation.HitDistanceThreshold);
        aggregate.MaximumHitDistanceThreshold = Mathf.Max(
            aggregate.MaximumHitDistanceThreshold,
            relation.HitDistanceThreshold);
        aggregate.ConfirmationToReleaseSeconds.Add(
            relation.ConfirmationToReleaseSeconds);
        aggregate.ReleaseToImpactSeconds.Add(
            relation.ReleaseToImpactSeconds);
        aggregate.PlannedTravelTimeSeconds.Add(
            relation.PlannedTravelTimeSeconds);

        if (relation.HasIntendedAtRelease)
        {
            aggregate.LandingToIntendedDistanceAtRelease.Add(
                relation.LandingToIntendedDistanceAtRelease);
            aggregate.IntendedMoveSpeedAtRelease.Add(
                relation.IntendedMoveSpeedAtRelease);
        }

        if (relation.HasIntendedAtImpact)
        {
            aggregate.LandingToIntendedDistanceAtImpact.Add(
                relation.LandingToIntendedDistanceAtImpact);
            aggregate.IntendedMoveSpeedAtImpact.Add(
                relation.IntendedMoveSpeedAtImpact);
        }

        switch (observation.ArcImpactResolutionType)
        {
            case ProjectileArcImpactResolutionType.IntendedTarget:
                if (isPrimary)
                {
                    aggregate.PrimaryIntendedTargetImpacts++;
                }
                else
                {
                    aggregate.AdditionalIntendedTargetImpacts++;
                }
                break;
            case ProjectileArcImpactResolutionType.FallbackTarget:
                if (isPrimary)
                {
                    aggregate.PrimaryFallbackTargetImpacts++;
                }
                else
                {
                    aggregate.AdditionalFallbackTargetImpacts++;
                }

                if (relation.HasResolvedTargetAtImpact)
                {
                    aggregate.LandingToFallbackDistanceAtImpact.Add(
                        relation.LandingToResolvedTargetDistanceAtImpact);
                    aggregate.FallbackMoveSpeedAtImpact.Add(
                        relation.ResolvedTargetMoveSpeedAtImpact);
                }

                ConsumeFallbackSpawnRelation(
                    aggregate,
                    ResolveSpawnOrdinalRelation(
                        relation.IntendedTarget,
                        relation.ResolvedTarget));
                break;
            case ProjectileArcImpactResolutionType.PositionOnlyIntendedInvalid:
            case ProjectileArcImpactResolutionType.PositionOnlyIntendedOutOfRange:
            case ProjectileArcImpactResolutionType.PositionOnlyWithoutIntendedTarget:
                if (isPrimary)
                {
                    aggregate.PrimaryPositionOnlyImpacts++;
                }
                else
                {
                    aggregate.AdditionalPositionOnlyImpacts++;
                }

                ConsumePositionOnlyNearestOther(
                    aggregate,
                    relation);
                break;
        }
    }

    private void ConsumeFallbackSpawnRelation(
        ArcTargetRelationAggregate aggregate,
        SpawnOrdinalRelation relation)
    {
        switch (relation)
        {
            case SpawnOrdinalRelation.ImmediateLaterSpawn:
                aggregate.FallbackImmediateLaterSpawnImpacts++;
                break;
            case SpawnOrdinalRelation.ImmediateEarlierSpawn:
                aggregate.FallbackImmediateEarlierSpawnImpacts++;
                break;
            case SpawnOrdinalRelation.NonAdjacent:
                aggregate.FallbackNonAdjacentImpacts++;
                break;
            default:
                aggregate.FallbackUnknownRelationImpacts++;
                break;
        }
    }

    private void ConsumePositionOnlyNearestOther(
        ArcTargetRelationAggregate aggregate,
        ProjectileArcTargetRelationObservation relation)
    {
        if (!relation.HasNearestOtherTargetAtImpact)
        {
            aggregate.PositionOnlyWithoutNearestOther++;
            return;
        }

        aggregate.PositionOnlyNearestOtherAvailable++;
        aggregate.LandingToPositionOnlyNearestOtherDistance.Add(
            relation.LandingToNearestOtherTargetDistanceAtImpact);
        aggregate.PositionOnlyNearestOtherMoveSpeedAtImpact.Add(
            relation.NearestOtherTargetMoveSpeedAtImpact);

        switch (ResolveSpawnOrdinalRelation(
                    relation.IntendedTarget,
                    relation.NearestOtherTarget))
        {
            case SpawnOrdinalRelation.ImmediateLaterSpawn:
                aggregate.PositionOnlyNearestOtherImmediateLaterSpawn++;
                break;
            case SpawnOrdinalRelation.ImmediateEarlierSpawn:
                aggregate.PositionOnlyNearestOtherImmediateEarlierSpawn++;
                break;
            case SpawnOrdinalRelation.NonAdjacent:
                aggregate.PositionOnlyNearestOtherNonAdjacent++;
                break;
            default:
                aggregate.PositionOnlyNearestOtherUnknownRelation++;
                break;
        }
    }

    private SpawnOrdinalRelation ResolveSpawnOrdinalRelation(
        MonsterBehaviour intendedTarget,
        MonsterBehaviour otherTarget)
    {
        int intendedOrdinal = GetSpawnOrdinal(intendedTarget);
        int otherOrdinal = GetSpawnOrdinal(otherTarget);

        if (intendedOrdinal <= 0 || otherOrdinal <= 0)
        {
            return SpawnOrdinalRelation.Unknown;
        }

        int delta = otherOrdinal - intendedOrdinal;

        if (delta == 1)
        {
            return SpawnOrdinalRelation.ImmediateLaterSpawn;
        }

        if (delta == -1)
        {
            return SpawnOrdinalRelation.ImmediateEarlierSpawn;
        }

        return SpawnOrdinalRelation.NonAdjacent;
    }



    private CombatBalanceArcTargetRelationSampleJson
        CreateArcTargetRelationSampleJson(
            ProjectileArcImpactResolutionType resolutionType,
            ProjectileArcTargetRelationObservation relation)
    {
        int intendedOrdinal = GetSpawnOrdinal(relation.IntendedTarget);
        int resolvedOrdinal = GetSpawnOrdinal(relation.ResolvedTarget);
        int nearestOtherOrdinal = GetSpawnOrdinal(
            relation.NearestOtherTarget);

        return new CombatBalanceArcTargetRelationSampleJson
        {
            memberType = relation.MemberType.ToString(),
            resolutionType = resolutionType.ToString(),
            intendedSpawnOrdinal = intendedOrdinal,
            resolvedSpawnOrdinal = resolvedOrdinal,
            nearestOtherSpawnOrdinal = nearestOtherOrdinal,
            resolvedOrdinalDelta = intendedOrdinal > 0 && resolvedOrdinal > 0
                ? resolvedOrdinal - intendedOrdinal
                : 0,
            nearestOtherOrdinalDelta =
                intendedOrdinal > 0 && nearestOtherOrdinal > 0
                    ? nearestOtherOrdinal - intendedOrdinal
                    : 0,
            confirmationToReleaseSeconds =
                relation.ConfirmationToReleaseSeconds,
            releaseToImpactSeconds = relation.ReleaseToImpactSeconds,
            plannedTravelTimeSeconds = relation.PlannedTravelTimeSeconds,
            hitDistanceThreshold = relation.HitDistanceThreshold,
            hasIntendedAtRelease = relation.HasIntendedAtRelease,
            landingToIntendedDistanceAtRelease =
                relation.LandingToIntendedDistanceAtRelease,
            intendedMoveSpeedAtRelease = relation.IntendedMoveSpeedAtRelease,
            hasIntendedAtImpact = relation.HasIntendedAtImpact,
            landingToIntendedDistanceAtImpact =
                relation.LandingToIntendedDistanceAtImpact,
            intendedMoveSpeedAtImpact = relation.IntendedMoveSpeedAtImpact,
            hasResolvedTargetAtImpact = relation.HasResolvedTargetAtImpact,
            landingToResolvedTargetDistanceAtImpact =
                relation.LandingToResolvedTargetDistanceAtImpact,
            resolvedTargetMoveSpeedAtImpact =
                relation.ResolvedTargetMoveSpeedAtImpact,
            hasNearestOtherTargetAtImpact =
                relation.HasNearestOtherTargetAtImpact,
            landingToNearestOtherTargetDistanceAtImpact =
                relation.LandingToNearestOtherTargetDistanceAtImpact,
            nearestOtherTargetMoveSpeedAtImpact =
                relation.NearestOtherTargetMoveSpeedAtImpact
        };
    }

    internal CombatBalanceProjectileRuntimeJson CreateProjectileRuntimeJson(
        int towerInstanceId)
    {
        if (!projectileRuntimeByTowerInstanceId.TryGetValue(
                towerInstanceId,
                out ProjectileRuntimeAggregate aggregate))
        {
            return new CombatBalanceProjectileRuntimeJson();
        }

        int resolvedArcOutcomes =
            aggregate.ArcTargetResolvedImpacts +
            aggregate.ArcPositionOnlyImpacts;
        int completedArcOutcomes =
            resolvedArcOutcomes + aggregate.ArcEndedWithoutImpact;

        return new CombatBalanceProjectileRuntimeJson
        {
            projectilesReleased = aggregate.ProjectilesReleased,
            initialProjectilesReleased =
                aggregate.InitialProjectilesReleased,
            childProjectilesReleased = aggregate.ChildProjectilesReleased,
            arcProjectilesReleased = aggregate.ArcProjectilesReleased,
            arcTargetResolvedImpacts =
                aggregate.ArcTargetResolvedImpacts,
            arcIntendedTargetImpacts =
                aggregate.ArcIntendedTargetImpacts,
            arcFallbackTargetImpacts = aggregate.ArcFallbackTargetImpacts,
            arcPositionOnlyImpacts = aggregate.ArcPositionOnlyImpacts,
            arcPositionOnlyIntendedInvalid =
                aggregate.ArcPositionOnlyIntendedInvalid,
            arcPositionOnlyIntendedOutOfRange =
                aggregate.ArcPositionOnlyIntendedOutOfRange,
            arcPositionOnlyWithoutIntendedTarget =
                aggregate.ArcPositionOnlyWithoutIntendedTarget,
            arcEndedWithoutImpact = aggregate.ArcEndedWithoutImpact,
            arcUnresolvedAtReport = Mathf.Max(
                0,
                aggregate.ArcProjectilesReleased - completedArcOutcomes),
            arcTargetResolutionRate = resolvedArcOutcomes > 0
                ? (float)aggregate.ArcTargetResolvedImpacts /
                  resolvedArcOutcomes
                : 0f,
            arcTargetRelation = CreateArcTargetRelationJson(
                aggregate.ArcTargetRelation)
        };
    }
    internal CombatBalanceDroneBurstRuntimeJson CreateDroneBurstRuntimeJson(
        int towerInstanceId)
    {
        CombatBalanceDroneBurstRuntimeJson result =
            new CombatBalanceDroneBurstRuntimeJson();

        if (!droneRuntimeByTowerInstanceId.TryGetValue(
                towerInstanceId,
                out Dictionary<int, DroneRuntimeAggregate> drones))
        {
            return result;
        }

        List<int> droneIds = new List<int>(drones.Keys);
        droneIds.Sort();

        for (int i = 0; i < droneIds.Count; i++)
        {
            DroneRuntimeAggregate drone = drones[droneIds[i]];
            CombatBalanceDroneRuntimeJson droneJson =
                new CombatBalanceDroneRuntimeJson
                {
                    sourceDroneInstanceId = drone.SourceDroneInstanceId,
                    isAdditionalAttackEntity =
                        drone.IsAdditionalAttackEntity
                };
            List<long> burstIds = new List<long>(drone.Bursts.Keys);
            burstIds.Sort();

            for (int burstIndex = 0;
                 burstIndex < burstIds.Count;
                 burstIndex++)
            {
                DroneBurstAggregate burst =
                    drone.Bursts[burstIds[burstIndex]];
                droneJson.burstsStarted += burst.Started;
                droneJson.openingProjectilesReleased +=
                    burst.OpeningProjectilesReleased;
                droneJson.elementalEligibleOpeningProjectilesReleased +=
                    burst.ElementalEligibleOpeningProjectilesReleased;
                droneJson.openingProjectileDirectHits +=
                    burst.OpeningProjectileDirectHits;
                droneJson.elementalEligibleOpeningProjectileDirectHits +=
                    burst.ElementalEligibleOpeningProjectileDirectHits;
                droneJson.openingProjectilesEndedWithoutImpact +=
                    burst.OpeningProjectilesEndedWithoutImpact;
                droneJson.laterProjectilesReleased +=
                    burst.LaterProjectilesReleased;
                droneJson.laterProjectileDirectHits +=
                    burst.LaterProjectileDirectHits;
                droneJson.laterProjectilesEndedWithoutImpact +=
                    burst.LaterProjectilesEndedWithoutImpact;
                droneJson.bursts.Add(new CombatBalanceDroneBurstJson
                {
                    burstId = burst.BurstId,
                    started = burst.Started,
                    openingProjectilesReleased =
                        burst.OpeningProjectilesReleased,
                    elementalEligibleOpeningProjectilesReleased =
                        burst.ElementalEligibleOpeningProjectilesReleased,
                    openingProjectileDirectHits =
                        burst.OpeningProjectileDirectHits,
                    elementalEligibleOpeningProjectileDirectHits =
                        burst.ElementalEligibleOpeningProjectileDirectHits,
                    openingProjectilesEndedWithoutImpact =
                        burst.OpeningProjectilesEndedWithoutImpact,
                    laterProjectilesReleased =
                        burst.LaterProjectilesReleased,
                    laterProjectileDirectHits =
                        burst.LaterProjectileDirectHits,
                    laterProjectilesEndedWithoutImpact =
                        burst.LaterProjectilesEndedWithoutImpact
                });
            }

            result.diagnosticsConsistent &=
                DroneRuntimeDiagnosticsAreConsistent(drone);
            result.drones.Add(droneJson);
        }

        return result;
    }
    internal CombatBalanceDroneLifecycleRuntimeJson
        CreateDroneLifecycleRuntimeJson(int towerInstanceId)
    {
        CombatBalanceDroneLifecycleRuntimeJson result =
            new CombatBalanceDroneLifecycleRuntimeJson();

        if (!droneRuntimeByTowerInstanceId.TryGetValue(
                towerInstanceId,
                out Dictionary<int, DroneRuntimeAggregate> drones))
        {
            return result;
        }

        List<int> droneIds = new List<int>(drones.Keys);
        droneIds.Sort();

        for (int i = 0; i < droneIds.Count; i++)
        {
            DroneRuntimeAggregate drone = drones[droneIds[i]];
            float activeTimeSeconds =
                drone.InitializedCount == 1 && drone.CompletionCount == 1
                    ? Mathf.Max(
                        0f,
                        drone.CompletedAtTime - drone.InitializedAtTime)
                    : 0f;

            result.diagnosticsConsistent &=
                DroneLifecycleDiagnosticsAreConsistent(drone);
            result.drones.Add(new CombatBalanceDroneLifecycleJson
            {
                sourceDroneInstanceId = drone.SourceDroneInstanceId,
                isAdditionalAttackEntity =
                    drone.IsAdditionalAttackEntity,
                initializedCount = drone.InitializedCount,
                launchCompletedCount = drone.LaunchCompletedCount,
                invalidTargetLossCount = drone.InvalidTargetLossCount,
                outOfRangeTargetLossCount =
                    drone.OutOfRangeTargetLossCount,
                immediateRetargetCount = drone.ImmediateRetargetCount,
                holdingEntryCount = drone.HoldingEntryCount,
                holdingExitCount = drone.HoldingExitCount,
                reacquisitionCount =
                    drone.ImmediateRetargetCount + drone.HoldingExitCount,
                orbitEntryCompletedCount =
                    drone.OrbitEntryCompletedCount,
                batteryDepletedCount = drone.BatteryDepletedCount,
                finalDiveEnteredCount = drone.FinalDiveEnteredCount,
                finalDiveCompletedCount = drone.FinalDiveCompletedCount,
                completionCount = drone.CompletionCount,
                activeTimeSeconds = activeTimeSeconds,
                holdingTimeSeconds = drone.HoldingTimeSeconds,
                completionReason = drone.CompletionReason.ToString()
            });
        }

        return result;
    }
    internal static CombatBalanceArcTargetRelationJson
        CreateArcTargetRelationJson(ArcTargetRelationAggregate aggregate)
    {
        return new CombatBalanceArcTargetRelationJson
        {
            sampledInitialImpacts = aggregate.SampledInitialImpacts,
            primaryIntendedTargetImpacts =
                aggregate.PrimaryIntendedTargetImpacts,
            primaryFallbackTargetImpacts =
                aggregate.PrimaryFallbackTargetImpacts,
            primaryPositionOnlyImpacts =
                aggregate.PrimaryPositionOnlyImpacts,
            additionalIntendedTargetImpacts =
                aggregate.AdditionalIntendedTargetImpacts,
            additionalFallbackTargetImpacts =
                aggregate.AdditionalFallbackTargetImpacts,
            additionalPositionOnlyImpacts =
                aggregate.AdditionalPositionOnlyImpacts,
            fallbackImmediateLaterSpawnImpacts =
                aggregate.FallbackImmediateLaterSpawnImpacts,
            fallbackImmediateEarlierSpawnImpacts =
                aggregate.FallbackImmediateEarlierSpawnImpacts,
            fallbackNonAdjacentImpacts =
                aggregate.FallbackNonAdjacentImpacts,
            fallbackUnknownRelationImpacts =
                aggregate.FallbackUnknownRelationImpacts,
            positionOnlyNearestOtherAvailable =
                aggregate.PositionOnlyNearestOtherAvailable,
            positionOnlyNearestOtherImmediateLaterSpawn =
                aggregate.PositionOnlyNearestOtherImmediateLaterSpawn,
            positionOnlyNearestOtherImmediateEarlierSpawn =
                aggregate.PositionOnlyNearestOtherImmediateEarlierSpawn,
            positionOnlyNearestOtherNonAdjacent =
                aggregate.PositionOnlyNearestOtherNonAdjacent,
            positionOnlyNearestOtherUnknownRelation =
                aggregate.PositionOnlyNearestOtherUnknownRelation,
            positionOnlyWithoutNearestOther =
                aggregate.PositionOnlyWithoutNearestOther,
            observedMinimumHitDistanceThreshold =
                aggregate.SampledInitialImpacts > 0
                    ? aggregate.MinimumHitDistanceThreshold
                    : 0f,
            observedMaximumHitDistanceThreshold =
                aggregate.SampledInitialImpacts > 0
                    ? aggregate.MaximumHitDistanceThreshold
                    : 0f,
            confirmationToReleaseSeconds = CreateMetricJson(
                aggregate.ConfirmationToReleaseSeconds),
            releaseToImpactSeconds = CreateMetricJson(
                aggregate.ReleaseToImpactSeconds),
            plannedTravelTimeSeconds = CreateMetricJson(
                aggregate.PlannedTravelTimeSeconds),
            landingToIntendedDistanceAtRelease = CreateMetricJson(
                aggregate.LandingToIntendedDistanceAtRelease),
            intendedMoveSpeedAtRelease = CreateMetricJson(
                aggregate.IntendedMoveSpeedAtRelease),
            landingToIntendedDistanceAtImpact = CreateMetricJson(
                aggregate.LandingToIntendedDistanceAtImpact),
            intendedMoveSpeedAtImpact = CreateMetricJson(
                aggregate.IntendedMoveSpeedAtImpact),
            landingToFallbackDistanceAtImpact = CreateMetricJson(
                aggregate.LandingToFallbackDistanceAtImpact),
            fallbackMoveSpeedAtImpact = CreateMetricJson(
                aggregate.FallbackMoveSpeedAtImpact),
            landingToPositionOnlyNearestOtherDistance = CreateMetricJson(
                aggregate.LandingToPositionOnlyNearestOtherDistance),
            positionOnlyNearestOtherMoveSpeedAtImpact = CreateMetricJson(
                aggregate.PositionOnlyNearestOtherMoveSpeedAtImpact),
            samples = new List<CombatBalanceArcTargetRelationSampleJson>(
                aggregate.Samples)
        };
    }
    private static CombatBalanceMetricJson CreateMetricJson(
        FloatMetricAggregate aggregate)
    {
        return new CombatBalanceMetricJson
        {
            samples = aggregate.Samples,
            minimum = aggregate.Samples > 0 ? aggregate.Minimum : 0f,
            average = aggregate.Samples > 0
                ? aggregate.Sum / aggregate.Samples
                : 0f,
            maximum = aggregate.Samples > 0 ? aggregate.Maximum : 0f
        };
    }
    internal bool DroneBurstDiagnosticsAreConsistent()
    {
        foreach (KeyValuePair<int, Dictionary<int, DroneRuntimeAggregate>>
                     towerEntry in droneRuntimeByTowerInstanceId)
        {
            foreach (KeyValuePair<int, DroneRuntimeAggregate> droneEntry in
                     towerEntry.Value)
            {
                if (!DroneRuntimeDiagnosticsAreConsistent(droneEntry.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal bool DroneLifecycleDiagnosticsAreConsistent()
    {
        foreach (KeyValuePair<int, Dictionary<int, DroneRuntimeAggregate>>
                     towerEntry in droneRuntimeByTowerInstanceId)
        {
            foreach (KeyValuePair<int, DroneRuntimeAggregate> droneEntry in
                     towerEntry.Value)
            {
                if (!DroneLifecycleDiagnosticsAreConsistent(droneEntry.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool DroneLifecycleDiagnosticsAreConsistent(
        DroneRuntimeAggregate drone)
    {
        if (drone == null ||
            !drone.DiagnosticsConsistent ||
            drone.InitializedCount != 1 ||
            drone.LaunchCompletedCount > 1 ||
            drone.CompletionCount != 1 ||
            !drone.IsCompleted ||
            drone.IsHolding ||
            drone.HasPendingTargetLoss ||
            drone.HoldingEntryCount < drone.HoldingExitCount ||
            drone.HoldingEntryCount - drone.HoldingExitCount > 1 ||
            drone.BatteryDepletedCount > 1 ||
            drone.FinalDiveEnteredCount > 1 ||
            drone.FinalDiveCompletedCount > 1 ||
            drone.HoldingTimeSeconds < 0f ||
            drone.CompletedAtTime < drone.InitializedAtTime)
        {
            return false;
        }

        if (drone.CompletionReason !=
                DroneCompletionReason.TechnicalCleanup &&
            drone.LaunchCompletedCount != 1)
        {
            return false;
        }

        return CompletionReasonMatchesLifecycle(drone);
    }

    internal static bool DroneRuntimeDiagnosticsAreConsistent(
        DroneRuntimeAggregate drone)
    {
        if (drone == null || !drone.DiagnosticsConsistent)
        {
            return false;
        }

        foreach (KeyValuePair<long, DroneBurstAggregate> entry in drone.Bursts)
        {
            DroneBurstAggregate burst = entry.Value;

            if (burst == null ||
                burst.BurstId <= 0 ||
                burst.Started != 1 ||
                burst.OpeningProjectilesReleased > 1 ||
                burst.OpeningProjectileDirectHits +
                burst.OpeningProjectilesEndedWithoutImpact >
                burst.OpeningProjectilesReleased ||
                burst.ElementalEligibleOpeningProjectilesReleased >
                burst.OpeningProjectilesReleased ||
                burst.ElementalEligibleOpeningProjectileDirectHits >
                burst.OpeningProjectileDirectHits ||
                burst.ElementalEligibleOpeningProjectileDirectHits >
                burst.ElementalEligibleOpeningProjectilesReleased ||
                burst.LaterProjectileDirectHits +
                burst.LaterProjectilesEndedWithoutImpact >
                burst.LaterProjectilesReleased ||
                (drone.IsAdditionalAttackEntity &&
                 (burst.ElementalEligibleOpeningProjectilesReleased > 0 ||
                  burst.ElementalEligibleOpeningProjectileDirectHits > 0)))
            {
                return false;
            }
        }

        return true;
    }

}
#endif
