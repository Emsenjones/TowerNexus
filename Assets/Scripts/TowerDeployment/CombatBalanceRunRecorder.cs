#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

public enum PlacementRouteForcedRelocationExpectation
{
    RequireZero = 0,
    RequireDiagnosed = 1,
    Ignore = 2
}

[DisallowMultipleComponent]
public sealed class CombatBalanceRunRecorder : MonoBehaviour
{
    private enum SpawnOrdinalRelation
    {
        Unknown = 0,
        ImmediateLaterSpawn = 1,
        ImmediateEarlierSpawn = 2,
        NonAdjacent = 3
    }

    [Header("Run Identity")]
    [FormerlySerializedAs("runLabel")]
    [SerializeField] private string runName;

    [Header("Placement Route Fixture")]
    [SerializeField] private PlacementRouteForcedRelocationExpectation
        placementRouteForcedRelocationExpectation =
            PlacementRouteForcedRelocationExpectation.RequireZero;

    [Header("Runtime References")]
    [SerializeField] private BattleRuntimeCoordinator battleRuntimeCoordinator;
    [SerializeField] private StageCompositionController stageCompositionController;
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private DraftSystem draftSystem;
    [SerializeField] private TowerPlacementController towerPlacementController;

    private readonly Dictionary<MonsterBehaviour, MonsterObservation>
        trackedMonsters =
            new Dictionary<MonsterBehaviour, MonsterObservation>();
    private readonly List<MonsterObservation> monsterObservations =
        new List<MonsterObservation>();
    private readonly HashSet<int> seenMonsterInstanceIds = new HashSet<int>();
    private readonly List<int> progressionRequirementsSnapshot =
        new List<int>();
    private readonly List<CombatBalanceProgressionEventJson>
        progressionEvents =
            new List<CombatBalanceProgressionEventJson>();
    private readonly List<CombatBalanceWaveEventJson> waveEvents =
        new List<CombatBalanceWaveEventJson>();
    private readonly List<CombatBalanceDraftAttemptJson> draftAttempts =
        new List<CombatBalanceDraftAttemptJson>();
    private readonly List<CombatBalanceInvestmentCommitJson> investmentCommits =
        new List<CombatBalanceInvestmentCommitJson>();
    private readonly List<CombatBalanceDraftItemJson> towerDraftPoolSnapshot =
        new List<CombatBalanceDraftItemJson>();
    private readonly List<CombatBalanceDraftItemJson>
        towerUpgradeDraftPoolSnapshot =
            new List<CombatBalanceDraftItemJson>();
    private readonly List<PendingLevelUpObservation> pendingLevelUps =
        new List<PendingLevelUpObservation>();
    private readonly ElementalBuffRunAccumulator buffAccumulator =
        new ElementalBuffRunAccumulator();
    private readonly ElementalHitReactionRunAccumulator
        elementalHitReactionAccumulator =
            new ElementalHitReactionRunAccumulator();
    private readonly Dictionary<int, ProjectileRuntimeAggregate>
        projectileRuntimeByTowerInstanceId =
            new Dictionary<int, ProjectileRuntimeAggregate>();
    private readonly Dictionary<int, Dictionary<int, DroneRuntimeAggregate>>
        droneRuntimeByTowerInstanceId =
            new Dictionary<int, Dictionary<int, DroneRuntimeAggregate>>();
    private readonly Dictionary<int, TowerDeploymentObservation>
        towerDeploymentByInstanceId =
            new Dictionary<int, TowerDeploymentObservation>();
    private readonly Dictionary<string, TowerScaledDamageAggregate>
        towerScaledDamageBySignature =
            new Dictionary<string, TowerScaledDamageAggregate>();
    private readonly Dictionary<string, TowerScaledRejectionAggregate>
        towerScaledRejectionBySignature =
            new Dictionary<string, TowerScaledRejectionAggregate>();
    private readonly Dictionary<string, FixedBuffDamageAggregate>
        fixedBuffDamageBySignature =
            new Dictionary<string, FixedBuffDamageAggregate>();
    private readonly Dictionary<string, TowerWaveDamageAggregate>
        towerWaveDamageByScope =
            new Dictionary<string, TowerWaveDamageAggregate>();
    private readonly Dictionary<int, TowerRouteDamageCoverageAggregate>
        towerRouteDamageCoverageByTowerInstanceId =
            new Dictionary<int, TowerRouteDamageCoverageAggregate>();
    private readonly Dictionary<string, ElementalOpportunityAggregate>
        elementalOpportunityByScope =
            new Dictionary<string, ElementalOpportunityAggregate>();
    private readonly Dictionary<string, int>
        elementalBuffApplicationAttemptsBySourceWave =
            new Dictionary<string, int>();
    private readonly List<CombatBalancePlacementRouteCommitJson>
        placementRouteCommits =
            new List<CombatBalancePlacementRouteCommitJson>();
    private readonly List<CombatBalancePlacementRouteLifecycleJson>
        placementRouteLifecycle =
            new List<CombatBalancePlacementRouteLifecycleJson>();

    private bool isSubscribed;
    private bool isTrackingRun;
    private bool hasLoggedFinalSummary;
    private bool hasPendingFinalSummary;
    private bool hasObservedFirstSpawn;
    private bool hasObservedSpawningCompletion;
    private string pendingTerminalState;
    private string pendingFailureReason;
    private int spawnedCount;
    private int resolvedCount;
    private int killedCount;
    private int leakedCount;
    private int peakAliveCount;
    private int effectiveDamage;
    private int leakedRemainingHealth;
    private int observedTotalMonsterMaxHealth;
    private int initialPlayerHealth;
    private int observedMinimumMonsterHealth;
    private int observedMaximumMonsterHealth;
    private int expectedMonsterCount;
    private bool hasExpectedMonsterCount;
    private int expectedWaveCount;
    private bool hasExpectedWaveCount;
    private string waveConfigName;
    private float observedMinimumMonsterSpeed;
    private float observedMaximumMonsterSpeed;
    private float firstSpawnTime;
    private float lastSpawnTime;
    private float lastResolutionTime;
    private float spawningCompletedTime;
    private float observedSpawnIntervalTotal;
    private int observedSpawnIntervalCount;
    private float runStartedAtTime;
    private int initialDraftCompletionCount;
    private string configuredDraftGenerationMode;
    private int configuredFixedDraftStepCount;
    private CombatBalanceFixtureJson fixtureSnapshot =
        new CombatBalanceFixtureJson();

    private sealed class PendingLevelUpObservation
    {
        public int PlayerLevel { get; set; }
        public int CurrentProgress { get; set; }
        public int RequiredProgress { get; set; }
        public float ActiveTimeSeconds { get; set; }
        public int TriggerResolutionNode { get; set; }
    }

    private sealed class TowerWaveDamageAggregate
    {
        public int TowerInstanceId { get; set; }
        public string TowerDisplayName { get; set; }
        public string TowerFamily { get; set; }
        public int WaveNumber { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int EffectiveTowerScaledDamage { get; set; }
        public int KillingBlows { get; set; }
    }

    private sealed class TowerRouteDamageCoverageAggregate
    {
        public int TowerInstanceId { get; set; }
        public string TowerDisplayName { get; set; }
        public string TowerFamily { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int LocatedDamageApplications { get; set; }
        public int UnresolvedRouteCellApplicationCount { get; set; }
        public int EffectiveDamage { get; set; }
        public int LocatedEffectiveDamage { get; set; }
        public int UnresolvedRouteCellEffectiveDamage { get; set; }
        public int KillingBlows { get; set; }
        public int LocatedKillingBlows { get; set; }
        public int UnresolvedRouteCellKillingBlows { get; set; }
        public HashSet<int> DamagedMonsterInstanceIds { get; } =
            new HashSet<int>();
        public Dictionary<string, TowerRouteDamageCellAggregate> Cells { get; } =
            new Dictionary<string, TowerRouteDamageCellAggregate>();
    }

    private sealed class TowerRouteDamageCellAggregate
    {
        public int PlacementCommitOrdinal { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int EffectiveDamage { get; set; }
        public int KillingBlows { get; set; }
        public HashSet<int> DamagedMonsterInstanceIds { get; } =
            new HashSet<int>();
    }

    private sealed class TowerDeploymentObservation
    {
        public int DeploymentOrdinal { get; set; }
        public float DeployedAtSeconds { get; set; }
        public Vector3 WorldPosition { get; set; }
        public List<Vector2Int> GridPositions { get; } =
            new List<Vector2Int>();
    }

    private sealed class TowerScaledDamageAggregate
    {
        public TowerScaledDamageAggregate(TowerOwnedDamageResolution resolution)
        {
            Resolution = resolution;
        }

        public TowerOwnedDamageResolution Resolution { get; }
        public int ResolutionCount { get; set; }
        public int SuccessfulApplicationCount { get; set; }
        public int AppliedDamageTotal { get; set; }
    }

    private sealed class TowerScaledRejectionAggregate
    {
        public TowerScaledRejectionAggregate(
            TowerOwnedDamageResolutionObservation observation)
        {
            Observation = observation;
        }

        public TowerOwnedDamageResolutionObservation Observation { get; }
        public int RejectionCount { get; set; }
    }

    private sealed class FixedBuffDamageAggregate
    {
        public FixedBuffDamageAggregate(FixedBuffDamageObservation observation)
        {
            EffectDefinition = observation.EffectDefinition;
            ActionOrdinal = observation.ActionOrdinal;
            FixedDamage = observation.FixedDamage;
        }

        public EffectDefinition EffectDefinition { get; }
        public int ActionOrdinal { get; }
        public int FixedDamage { get; }
        public int ResolvedTargetCount { get; set; }
        public int ResolutionCount { get; set; }
        public int SuccessfulApplicationCount { get; set; }
        public int AppliedDamageTotal { get; set; }
        public HashSet<int> SourceTowerInstanceIds { get; } =
            new HashSet<int>();
    }

    private sealed class ElementalOpportunityAggregate
    {
        public ElementalOpportunityAggregate(
            ElementalOpportunityObservation observation,
            int waveNumber)
        {
            SourceTower = observation.SourceTower;
            ElementalUpgrade = observation.ElementalUpgrade;
            WaveNumber = waveNumber;
            Diagnostics = observation.Diagnostics;
            MinimumResultOrdinal = observation.Diagnostics.ResultOrdinal;
            MaximumResultOrdinal = observation.Diagnostics.ResultOrdinal;
        }

        public TowerInstance SourceTower { get; }
        public TowerUpgradeDefinition ElementalUpgrade { get; }
        public int WaveNumber { get; }
        public ElementalOpportunityDiagnosticContext Diagnostics { get; }
        public int MinimumResultOrdinal { get; set; }
        public int MaximumResultOrdinal { get; set; }
        public int CandidateResults { get; set; }
        public int EligibleResults { get; set; }
        public int DispatchedRequests { get; set; }
    }

    private sealed class MonsterObservation
    {
        public MonsterObservation(
            string runtimeTemplateName,
            string displayName,
            int maximumHealth,
            float moveSpeedAtSpawn,
            int currentHealth,
            int spawnOrdinal,
            float spawnTime,
            float spawnedAtSeconds,
            bool observedThroughRegistrationEvent)
        {
            RuntimeTemplateName = runtimeTemplateName;
            DisplayName = displayName;
            MaximumHealth = maximumHealth;
            MoveSpeedAtSpawn = moveSpeedAtSpawn;
            LastHealth = currentHealth;
            SpawnOrdinal = spawnOrdinal;
            SpawnTime = spawnTime;
            SpawnedAtSeconds = spawnedAtSeconds;
            ObservedThroughRegistrationEvent =
                observedThroughRegistrationEvent;
            HealthAtObservationStart = currentHealth;
            EffectiveDamage = Mathf.Max(0, maximumHealth - currentHealth);
        }

        public string RuntimeTemplateName { get; }
        public string DisplayName { get; }
        public int MaximumHealth { get; }
        public float MoveSpeedAtSpawn { get; }
        public int LastHealth { get; set; }
        public int SpawnOrdinal { get; }
        public int SourceWaveNumber { get; set; }
        public int SourceWaveSpawnOrdinal { get; set; }
        public float SpawnTime { get; }
        public float SpawnedAtSeconds { get; }
        public bool ObservedThroughRegistrationEvent { get; }
        public int HealthAtObservationStart { get; }
        public int UnobservedDamageAtObservationStart =>
            Mathf.Max(0, MaximumHealth - HealthAtObservationStart);
        public float ResolutionTime { get; set; }
        public float ResolvedAtSeconds { get; set; }
        public int FinalHealth { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int EffectiveDamage { get; set; }
        public bool ReachedTarget { get; set; }
        public bool IsResolved { get; set; }
    }

    private sealed class WaveResolutionAggregate
    {
        public int WaveNumber { get; set; }
        public string RuntimeTemplateName { get; set; }
        public int ConfiguredCount { get; set; }
        public int Spawned { get; set; }
        public int Resolved { get; set; }
        public int Killed { get; set; }
        public int Leaked { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int EffectiveDamage { get; set; }
        public int LeakedRemainingHealth { get; set; }
        public float FirstResolutionAtSeconds { get; set; }
        public float LastResolutionAtSeconds { get; set; }
    }

    private sealed class MonsterTypeAggregate
    {
        public MonsterTypeAggregate(MonsterObservation observation)
        {
            RuntimeTemplateName = observation.RuntimeTemplateName;
            DisplayName = observation.DisplayName;
            MaximumHealth = observation.MaximumHealth;
            MoveSpeedAtSpawn = observation.MoveSpeedAtSpawn;
        }

        public string RuntimeTemplateName { get; }
        public string DisplayName { get; }
        public int MaximumHealth { get; }
        public float MoveSpeedAtSpawn { get; }
        public int Spawned { get; set; }
        public int Resolved { get; set; }
        public int Killed { get; set; }
        public int Leaked { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int EffectiveDamage { get; set; }
        public int LeakedRemainingHealth { get; set; }
        public int RegistrationObservedInstances { get; set; }
        public int FallbackObservedInstances { get; set; }
        public int InstancesObservedAtFullHealth { get; set; }
        public int InstancesObservedAfterDamage { get; set; }
        public int UnobservedDamageAtObservationStart { get; set; }
        public FloatMetricAggregate ResolutionLifetimeSeconds { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate KilledLifetimeSeconds { get; } =
            new FloatMetricAggregate();
        public FloatMetricAggregate LeakedLifetimeSeconds { get; } =
            new FloatMetricAggregate();
    }

    private sealed class FloatMetricAggregate
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

    private sealed class ArcTargetRelationAggregate
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

    private sealed class ProjectileRuntimeAggregate
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

    private sealed class DroneBurstAggregate
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

    private sealed class DroneRuntimeAggregate
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

    private void OnEnable()
    {
        ResolveReferences();
        WarnAboutMissingReferences();
        SubscribeToRuntime();

        if (battleRuntimeCoordinator != null &&
            battleRuntimeCoordinator.IsBattleActive)
        {
            BeginRun();
        }
    }

    private void OnDisable()
    {
        FlushPendingFinalSummary();
        UnsubscribeFromRuntime();
        UnsubscribeFromTrackedMonsters();
        isTrackingRun = false;
    }

    private void Update()
    {
        if (!isTrackingRun &&
            battleRuntimeCoordinator != null &&
            battleRuntimeCoordinator.IsBattleActive)
        {
            BeginRun();
        }

        if (!isTrackingRun)
        {
            return;
        }

        TrackCurrentMonsters();

        if (monsterManager != null)
        {
            peakAliveCount = Mathf.Max(
                peakAliveCount,
                monsterManager.AliveMonsterCount);
        }
    }

    private void LateUpdate()
    {
        FlushPendingProgressionEvents();
        FlushPendingFinalSummary();
    }

    [ContextMenu("Log Current Balance Summary")]
    private void LogCurrentBalanceSummary()
    {
        TrackCurrentMonsters();
        Debug.Log(BuildSummary("Manual Snapshot", null, DateTime.Now), this);
    }

    [ContextMenu("Reset Balance Recorder")]
    private void ResetBalanceRecorder()
    {
        BeginRun();
    }

    private void ResolveReferences()
    {
        if (battleRuntimeCoordinator == null)
        {
            battleRuntimeCoordinator =
                FindFirstObjectByType<BattleRuntimeCoordinator>();
        }

        if (stageCompositionController == null)
        {
            stageCompositionController =
                FindFirstObjectByType<StageCompositionController>();
        }

        if (monsterSpawner == null)
        {
            monsterSpawner = FindFirstObjectByType<MonsterSpawner>();
        }

        if (monsterManager == null)
        {
            monsterManager = FindFirstObjectByType<MonsterManager>();
        }

        if (playerSystem == null)
        {
            playerSystem = FindFirstObjectByType<PlayerSystem>();
        }

        if (draftSystem == null)
        {
            draftSystem = FindFirstObjectByType<DraftSystem>();
        }

        if (towerPlacementController == null)
        {
            towerPlacementController =
                FindFirstObjectByType<TowerPlacementController>();
        }
    }

    private void SubscribeToRuntime()
    {
        if (isSubscribed)
        {
            return;
        }

        if (playerSystem != null)
        {
            playerSystem.OnBattleStateInitialized +=
                HandleBattleStateInitialized;
            playerSystem.OnLevelUp += HandlePlayerLevelUp;
        }

        if (draftSystem != null)
        {
            draftSystem.OnInitialDraftCompleted +=
                HandleInitialDraftCompleted;
            draftSystem.OnDraftChoicesOpened +=
                HandleDraftChoicesOpened;
            draftSystem.OnDraftChoiceCommitted +=
                HandleDraftChoiceCommitted;
        }

        if (monsterSpawner != null)
        {
            monsterSpawner.OnSpawningStarted += HandleSpawningStarted;
            monsterSpawner.OnAllSpawningCompleted +=
                HandleAllSpawningCompleted;
            monsterSpawner.OnWaveSpawningStarted +=
                HandleWaveSpawningStarted;
            monsterSpawner.OnWaveSpawningCompleted +=
                HandleWaveSpawningCompleted;
            monsterSpawner.OnMonsterSpawnedFromWave +=
                HandleMonsterSpawnedFromWave;
        }

        if (monsterManager != null)
        {
            monsterManager.OnMonsterRegistered += HandleMonsterRegistered;
            monsterManager.OnPlacementRouteLifecycleObserved +=
                HandlePlacementRouteLifecycleObserved;
        }

        if (towerPlacementController != null)
        {
            towerPlacementController.OnTowerDeploymentCommitted +=
                HandleTowerDeploymentCommitted;
            towerPlacementController.OnTowerInvestmentCommitted +=
                HandleTowerInvestmentCommitted;
            towerPlacementController.OnPlacementRouteRevisionCommitted +=
                HandlePlacementRouteRevisionCommitted;
        }

        if (battleRuntimeCoordinator != null)
        {
            battleRuntimeCoordinator.OnBattleResultPublished +=
                HandleBattleResultPublished;
            battleRuntimeCoordinator.OnBattleRuntimeFailed +=
                HandleBattleRuntimeFailed;
        }

        ProjectileBehaviour.OnRuntimeObserved +=
            HandleProjectileRuntimeObserved;
        DroneBurstRuntimeDiagnostics.OnObserved +=
            HandleDroneBurstRuntimeObserved;
        DroneLifecycleRuntimeDiagnostics.OnObserved +=
            HandleDroneLifecycleRuntimeObserved;
        TowerRuntimeStatResolver.OnTowerOwnedDamageResolutionObserved +=
            HandleTowerOwnedDamageResolutionObserved;
        TowerRuntimeStatResolver.OnTowerOwnedDamageApplicationObserved +=
            HandleTowerOwnedDamageApplicationObserved;
        TowerRuntimeStatResolver.OnTowerOwnedTargetDamageObserved +=
            HandleTowerOwnedTargetDamageObserved;
        EffectExecutor.OnFixedBuffDamageObserved +=
            HandleFixedBuffDamageObserved;
        ElementalApplication.OnOpportunityObserved +=
            HandleElementalOpportunityObserved;

        isSubscribed = true;
    }

    private void WarnAboutMissingReferences()
    {
        List<string> missingReferences = new List<string>();

        if (battleRuntimeCoordinator == null)
        {
            missingReferences.Add(nameof(battleRuntimeCoordinator));
        }

        if (stageCompositionController == null)
        {
            missingReferences.Add(nameof(stageCompositionController));
        }

        if (monsterSpawner == null)
        {
            missingReferences.Add(nameof(monsterSpawner));
        }

        if (monsterManager == null)
        {
            missingReferences.Add(nameof(monsterManager));
        }

        if (playerSystem == null)
        {
            missingReferences.Add(nameof(playerSystem));
        }

        if (draftSystem == null)
        {
            missingReferences.Add(nameof(draftSystem));
        }

        if (towerPlacementController == null)
        {
            missingReferences.Add(nameof(towerPlacementController));
        }

        if (missingReferences.Count > 0)
        {
            Debug.LogWarning(
                "Combat balance run recorder is missing runtime references: " +
                string.Join(", ", missingReferences) + ".",
                this);
        }
    }

    private void UnsubscribeFromRuntime()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (playerSystem != null)
        {
            playerSystem.OnBattleStateInitialized -=
                HandleBattleStateInitialized;
            playerSystem.OnLevelUp -= HandlePlayerLevelUp;
        }

        if (draftSystem != null)
        {
            draftSystem.OnInitialDraftCompleted -=
                HandleInitialDraftCompleted;
            draftSystem.OnDraftChoicesOpened -=
                HandleDraftChoicesOpened;
            draftSystem.OnDraftChoiceCommitted -=
                HandleDraftChoiceCommitted;
        }

        if (monsterSpawner != null)
        {
            monsterSpawner.OnSpawningStarted -= HandleSpawningStarted;
            monsterSpawner.OnAllSpawningCompleted -=
                HandleAllSpawningCompleted;
            monsterSpawner.OnWaveSpawningStarted -=
                HandleWaveSpawningStarted;
            monsterSpawner.OnWaveSpawningCompleted -=
                HandleWaveSpawningCompleted;
            monsterSpawner.OnMonsterSpawnedFromWave -=
                HandleMonsterSpawnedFromWave;
        }

        if (monsterManager != null)
        {
            monsterManager.OnMonsterRegistered -= HandleMonsterRegistered;
            monsterManager.OnPlacementRouteLifecycleObserved -=
                HandlePlacementRouteLifecycleObserved;
        }

        if (towerPlacementController != null)
        {
            towerPlacementController.OnTowerDeploymentCommitted -=
                HandleTowerDeploymentCommitted;
            towerPlacementController.OnTowerInvestmentCommitted -=
                HandleTowerInvestmentCommitted;
            towerPlacementController.OnPlacementRouteRevisionCommitted -=
                HandlePlacementRouteRevisionCommitted;
        }

        if (battleRuntimeCoordinator != null)
        {
            battleRuntimeCoordinator.OnBattleResultPublished -=
                HandleBattleResultPublished;
            battleRuntimeCoordinator.OnBattleRuntimeFailed -=
                HandleBattleRuntimeFailed;
        }

        ProjectileBehaviour.OnRuntimeObserved -=
            HandleProjectileRuntimeObserved;
        DroneBurstRuntimeDiagnostics.OnObserved -=
            HandleDroneBurstRuntimeObserved;
        DroneLifecycleRuntimeDiagnostics.OnObserved -=
            HandleDroneLifecycleRuntimeObserved;
        TowerRuntimeStatResolver.OnTowerOwnedDamageResolutionObserved -=
            HandleTowerOwnedDamageResolutionObserved;
        TowerRuntimeStatResolver.OnTowerOwnedDamageApplicationObserved -=
            HandleTowerOwnedDamageApplicationObserved;
        TowerRuntimeStatResolver.OnTowerOwnedTargetDamageObserved -=
            HandleTowerOwnedTargetDamageObserved;
        EffectExecutor.OnFixedBuffDamageObserved -=
            HandleFixedBuffDamageObserved;
        ElementalApplication.OnOpportunityObserved -=
            HandleElementalOpportunityObserved;

        isSubscribed = false;
    }

    private void HandleBattleStateInitialized()
    {
        BeginRun();
    }

    private void HandleInitialDraftCompleted(DraftAttemptToken attemptToken)
    {
        if (!isTrackingRun || !attemptToken.IsValid)
        {
            return;
        }

        initialDraftCompletionCount++;
        progressionEvents.Add(CreateProgressionEvent(
            "Initial",
            playerSystem != null ? playerSystem.CurrentLevel : 0,
            playerSystem != null ? playerSystem.CurrentProgress : 0,
            playerSystem != null ? playerSystem.RequiredProgress : 0,
            GetRunActiveTimeSeconds()));
    }

    private void HandlePlayerLevelUp(int playerLevel)
    {
        if (!isTrackingRun)
        {
            return;
        }

        pendingLevelUps.Add(new PendingLevelUpObservation
        {
            PlayerLevel = playerLevel,
            CurrentProgress = playerSystem != null
                ? playerSystem.CurrentProgress
                : 0,
            RequiredProgress = playerSystem != null
                ? playerSystem.RequiredProgress
                : 0,
            ActiveTimeSeconds = GetRunActiveTimeSeconds(),
            TriggerResolutionNode = resolvedCount + 1
        });
    }

    private void FlushPendingProgressionEvents()
    {
        for (int i = 0; i < pendingLevelUps.Count; i++)
        {
            PendingLevelUpObservation observation = pendingLevelUps[i];

            progressionEvents.Add(CreateProgressionEvent(
                "LevelUp",
                observation.PlayerLevel,
                observation.CurrentProgress,
                observation.RequiredProgress,
                observation.ActiveTimeSeconds,
                observation.TriggerResolutionNode));
        }

        pendingLevelUps.Clear();
    }

    private void CaptureDraftFixture()
    {
        if (draftSystem == null)
        {
            return;
        }

        configuredDraftGenerationMode = draftSystem.UseFixedDraftChoices
            ? DraftChoiceGenerationMode.Fixed.ToString()
            : DraftChoiceGenerationMode.Natural.ToString();
        configuredFixedDraftStepCount =
            draftSystem.ConfiguredFixedDraftStepCount;

        IReadOnlyList<TowerDefinition> towerDefinitions =
            draftSystem.BoundTowerDefinitions;

        if (towerDefinitions != null)
        {
            for (int i = 0; i < towerDefinitions.Count; i++)
            {
                TowerDefinition definition = towerDefinitions[i];

                if (definition != null)
                {
                    towerDraftPoolSnapshot.Add(CreateDraftItemJson(
                        DraftResult.CreateTowerDraft(definition),
                        1));
                }
            }
        }

        IReadOnlyList<TowerUpgradeDefinition> upgradeDefinitions =
            draftSystem.BoundUpgradeDefinitions;

        if (upgradeDefinitions == null)
        {
            return;
        }

        for (int i = 0; i < upgradeDefinitions.Count; i++)
        {
            TowerUpgradeDefinition definition = upgradeDefinitions[i];

            if (definition != null)
            {
                towerUpgradeDraftPoolSnapshot.Add(CreateDraftItemJson(
                    DraftResult.CreateTowerUpgradeDraft(definition),
                    1));
            }
        }
    }

    private void HandleDraftChoicesOpened(
        DraftChoicesOpenedObservation observation)
    {
        if (!isTrackingRun || observation == null)
        {
            return;
        }

        if (towerDraftPoolSnapshot.Count == 0 &&
            towerUpgradeDraftPoolSnapshot.Count == 0)
        {
            CaptureDraftFixture();
        }

        CombatBalanceDraftAttemptJson attempt =
            new CombatBalanceDraftAttemptJson
            {
                attemptToken = observation.AttemptToken.ToString(),
                ordinal = observation.DraftOrdinal,
                sessionKind = observation.SessionKind.ToString(),
                generationMode = observation.GenerationMode.ToString(),
                resolvedMonsterCount = ResolveDraftResolutionNode(
                    observation),
                playerLevel = playerSystem != null
                    ? playerSystem.CurrentLevel
                    : 0,
                currentProgress = playerSystem != null
                    ? playerSystem.CurrentProgress
                    : 0,
                requiredProgress = playerSystem != null
                    ? playerSystem.RequiredProgress
                    : 0,
                activeTimeSeconds = GetRunActiveTimeSeconds()
            };

        for (int i = 0; i < observation.NaturalCandidates.Count; i++)
        {
            DraftChoiceCandidateObservation candidate =
                observation.NaturalCandidates[i];

            if (candidate != null && candidate.DraftResult != null)
            {
                attempt.naturalCandidates.Add(CreateDraftItemJson(
                    candidate.DraftResult,
                    candidate.Multiplicity));
            }
        }

        for (int i = 0; i < observation.DisplayedChoices.Count; i++)
        {
            DraftResult displayedChoice = observation.DisplayedChoices[i];

            if (displayedChoice != null)
            {
                attempt.displayedChoices.Add(CreateDraftItemJson(
                    displayedChoice,
                    ResolveNaturalMultiplicity(
                        observation.NaturalCandidates,
                        displayedChoice.Identity)));
            }
        }

        draftAttempts.Add(attempt);
    }

    private void HandleDraftChoiceCommitted(
        DraftChoiceCommittedObservation observation)
    {
        if (!isTrackingRun ||
            observation == null ||
            observation.SelectedChoice == null)
        {
            return;
        }

        string attemptToken = observation.AttemptToken.ToString();

        for (int i = draftAttempts.Count - 1; i >= 0; i--)
        {
            CombatBalanceDraftAttemptJson attempt = draftAttempts[i];

            if (!string.Equals(
                    attempt.attemptToken,
                    attemptToken,
                    StringComparison.Ordinal))
            {
                continue;
            }

            attempt.selectionCommitted = true;
            attempt.selectionCommittedAtSeconds =
                GetRunActiveTimeSeconds();
            attempt.selectedChoice = CreateDraftItemJson(
                observation.SelectedChoice,
                ResolveDisplayedMultiplicity(
                    attempt.displayedChoices,
                    observation.SelectedChoice));
            return;
        }
    }

    private int ResolveDraftResolutionNode(
        DraftChoicesOpenedObservation observation)
    {
        if (observation.SessionKind != DraftChoiceSessionKind.LevelUp)
        {
            return resolvedCount;
        }

        for (int i = pendingLevelUps.Count - 1; i >= 0; i--)
        {
            PendingLevelUpObservation pending = pendingLevelUps[i];

            if (playerSystem == null ||
                pending.PlayerLevel == playerSystem.CurrentLevel)
            {
                return pending.TriggerResolutionNode;
            }
        }

        return resolvedCount;
    }

    private static int ResolveNaturalMultiplicity(
        IReadOnlyList<DraftChoiceCandidateObservation> candidates,
        UnityEngine.Object identity)
    {
        if (candidates == null || identity == null)
        {
            return 0;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            DraftChoiceCandidateObservation candidate = candidates[i];

            if (candidate != null &&
                candidate.DraftResult != null &&
                candidate.DraftResult.Identity == identity)
            {
                return candidate.Multiplicity;
            }
        }

        return 0;
    }

    private static int ResolveDisplayedMultiplicity(
        IReadOnlyList<CombatBalanceDraftItemJson> displayedChoices,
        DraftResult selectedChoice)
    {
        if (displayedChoices == null || selectedChoice == null)
        {
            return 0;
        }

        for (int i = 0; i < displayedChoices.Count; i++)
        {
            CombatBalanceDraftItemJson displayedChoice =
                displayedChoices[i];

            if (string.Equals(
                    displayedChoice.assetName,
                    selectedChoice.Identity.name,
                    StringComparison.Ordinal) &&
                string.Equals(
                    displayedChoice.resultType,
                    selectedChoice.ResultType.ToString(),
                    StringComparison.Ordinal))
            {
                return displayedChoice.multiplicity;
            }
        }

        return 0;
    }

    private static CombatBalanceDraftItemJson CreateDraftItemJson(
        DraftResult draftResult,
        int multiplicity)
    {
        CombatBalanceDraftItemJson item = new CombatBalanceDraftItemJson
        {
            multiplicity = Mathf.Max(0, multiplicity)
        };

        if (draftResult == null || !draftResult.IsValid)
        {
            return item;
        }

        item.resultType = draftResult.ResultType.ToString();
        item.assetName = draftResult.Identity != null
            ? draftResult.Identity.name
            : string.Empty;
        item.displayName = draftResult.DisplayName;

        if (draftResult.ResultType == DraftResultType.TowerDraft)
        {
            TowerDefinition towerDefinition = draftResult.TowerDefinition;
            item.towerFamily = towerDefinition != null
                ? towerDefinition.TowerFamily.ToString()
                : string.Empty;
            return item;
        }

        TowerUpgradeDefinition upgradeDefinition =
            draftResult.TowerUpgradeDefinition;

        if (upgradeDefinition != null)
        {
            item.towerFamily = upgradeDefinition.TowerFamily.ToString();
            item.upgradeLayer = upgradeDefinition.UpgradeLayer.ToString();
            item.requiredTowerLevel =
                upgradeDefinition.RequiredTowerLevel;
        }

        return item;
    }

    private CombatBalanceProgressionEventJson CreateProgressionEvent(
        string kind,
        int playerLevel,
        int currentProgress,
        int requiredProgress,
        float activeTimeSeconds,
        int resolvedMonsterCountOverride = -1)
    {
        int eventResolvedMonsterCount = resolvedMonsterCountOverride >= 0
            ? resolvedMonsterCountOverride
            : resolvedCount;
        return new CombatBalanceProgressionEventJson
        {
            ordinal = progressionEvents.Count + 1,
            kind = kind,
            resolvedMonsterCount = eventResolvedMonsterCount,
            playerLevel = playerLevel,
            currentProgress = currentProgress,
            requiredProgress = requiredProgress,
            activeTimeSeconds = activeTimeSeconds,
            spawned = spawnedCount,
            resolved = eventResolvedMonsterCount,
            killed = killedCount,
            leaked = leakedCount,
            alive = monsterManager != null
                ? monsterManager.AliveMonsterCount
                : Mathf.Max(0, spawnedCount - resolvedCount),
            playerHealth = playerSystem != null
                ? playerSystem.CurrentHealth
                : 0
        };
    }

    private void HandleSpawningStarted()
    {
        if (!isTrackingRun)
        {
            BeginRun();
            return;
        }

        CaptureStageFixture();
        CaptureExpectedMonsterFixture();
    }

    private void BeginRun()
    {
        UnsubscribeFromTrackedMonsters();
        trackedMonsters.Clear();
        monsterObservations.Clear();
        seenMonsterInstanceIds.Clear();
        progressionRequirementsSnapshot.Clear();
        progressionEvents.Clear();
        waveEvents.Clear();
        draftAttempts.Clear();
        investmentCommits.Clear();
        towerDraftPoolSnapshot.Clear();
        towerUpgradeDraftPoolSnapshot.Clear();
        pendingLevelUps.Clear();
        buffAccumulator.Reset();
        elementalHitReactionAccumulator.Reset();
        projectileRuntimeByTowerInstanceId.Clear();
        droneRuntimeByTowerInstanceId.Clear();
        towerDeploymentByInstanceId.Clear();
        towerScaledDamageBySignature.Clear();
        towerScaledRejectionBySignature.Clear();
        fixedBuffDamageBySignature.Clear();
        towerWaveDamageByScope.Clear();
        towerRouteDamageCoverageByTowerInstanceId.Clear();
        elementalOpportunityByScope.Clear();
        elementalBuffApplicationAttemptsBySourceWave.Clear();
        placementRouteCommits.Clear();
        placementRouteLifecycle.Clear();
        ResetExpectedMonsterFixture();
        spawnedCount = 0;
        resolvedCount = 0;
        killedCount = 0;
        leakedCount = 0;
        peakAliveCount = 0;
        effectiveDamage = 0;
        leakedRemainingHealth = 0;
        observedTotalMonsterMaxHealth = 0;
        initialPlayerHealth = playerSystem != null
            ? playerSystem.CurrentHealth
            : 0;
        observedMinimumMonsterHealth = int.MaxValue;
        observedMaximumMonsterHealth = 0;
        observedMinimumMonsterSpeed = float.PositiveInfinity;
        observedMaximumMonsterSpeed = 0f;
        firstSpawnTime = 0f;
        lastSpawnTime = 0f;
        lastResolutionTime = 0f;
        spawningCompletedTime = 0f;
        observedSpawnIntervalTotal = 0f;
        observedSpawnIntervalCount = 0;
        runStartedAtTime = Time.time;
        initialDraftCompletionCount = 0;
        configuredDraftGenerationMode = string.Empty;
        configuredFixedDraftStepCount = 0;
        hasObservedFirstSpawn = false;
        hasObservedSpawningCompletion = false;
        hasLoggedFinalSummary = false;
        hasPendingFinalSummary = false;
        pendingTerminalState = null;
        pendingFailureReason = null;
        isTrackingRun = true;

        CaptureStageFixture();
        CaptureDraftFixture();

        if (playerSystem != null)
        {
            IReadOnlyList<int> progressRequirements =
                playerSystem.ProgressRequirements;

            if (progressRequirements != null)
            {
                for (int i = 0; i < progressRequirements.Count; i++)
                {
                    progressionRequirementsSnapshot.Add(
                        progressRequirements[i]);
                }
            }
        }

        if (monsterSpawner != null && monsterSpawner.IsSpawning)
        {
            CaptureExpectedMonsterFixture();
        }
    }

    private void ResetExpectedMonsterFixture()
    {
        expectedMonsterCount = 0;
        hasExpectedMonsterCount = false;
        expectedWaveCount = 0;
        hasExpectedWaveCount = false;
        waveConfigName = monsterSpawner != null
            ? monsterSpawner.BoundWaveConfigName
            : string.Empty;
    }

    private void CaptureStageFixture()
    {
        fixtureSnapshot = new CombatBalanceFixtureJson();
        StageDefinition stage = stageCompositionController != null
            ? stageCompositionController.ActiveStage
            : null;
        MapGeneratorBehaviour map = stageCompositionController != null
            ? stageCompositionController.ActiveMap
            : null;

        if (stage == null)
        {
            return;
        }

        fixtureSnapshot.stageDefinitionName = stage.name;
        fixtureSnapshot.stageDisplayName = stage.DisplayName ?? string.Empty;
        fixtureSnapshot.configuredPlayerMaxHealth = stage.PlayerMaxHealth;
        fixtureSnapshot.mapTemplateName = stage.MapTemplate != null
            ? stage.MapTemplate.name
            : string.Empty;
        fixtureSnapshot.runtimeMapName = map != null
            ? map.name
            : string.Empty;
        fixtureSnapshot.mapWidth = map != null ? map.Width : 0;
        fixtureSnapshot.mapLength = map != null ? map.Lengh : 0;
        fixtureSnapshot.mapNodeSize = map != null ? map.NodeSize : 0f;

        if (map != null)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int z = 0; z < map.Lengh; z++)
                {
                    GridNodeBehaviour node = map.GetNode(x, z);

                    if (node == null)
                    {
                        continue;
                    }

                    fixtureSnapshot.mapNodes.Add(
                        new CombatBalanceMapNodeFixtureJson
                        {
                            x = node.GridPosition.x,
                            z = node.GridPosition.y,
                            nodeType = node.NodeType.ToString(),
                            baseWalkable = node.BaseWalkable
                        });
                }
            }
        }

        MonsterWaveConfig waveConfig = stage.MonsterWaveConfig;
        fixtureSnapshot.waveConfigName = waveConfig != null
            ? waveConfig.name
            : string.Empty;
        IReadOnlyList<MonsterWaveEntry> waves = waveConfig != null
            ? waveConfig.Waves
            : null;

        if (waves == null)
        {
            return;
        }

        for (int i = 0; i < waves.Count; i++)
        {
            MonsterWaveEntry wave = waves[i];

            if (wave == null)
            {
                continue;
            }

            MonsterBehaviour template = wave.MonsterRuntimeTemplate;
            fixtureSnapshot.waves.Add(new CombatBalanceWaveFixtureJson
            {
                waveNumber = i + 1,
                runtimeTemplateName = template != null
                    ? template.name
                    : string.Empty,
                monsterDisplayName = template != null
                    ? template.DisplayName
                    : string.Empty,
                maximumHealth = template != null ? template.MaxHealth : 0,
                moveSpeed = template != null ? template.BaseMoveSpeed : 0f,
                count = wave.Count,
                spawnIntervalSeconds = wave.SpawnInterval,
                waveDelaySeconds = wave.WaveDelay
            });
        }
    }

    private void CaptureExpectedMonsterFixture()
    {
        ResetExpectedMonsterFixture();

        if (monsterSpawner != null &&
            monsterSpawner.TryGetExpectedMonsterCount(
                out int resolvedExpectedMonsterCount))
        {
            expectedMonsterCount = resolvedExpectedMonsterCount;
            hasExpectedMonsterCount = true;
        }
        else
        {
            Debug.LogWarning(
                "Combat balance run recorder could not resolve Expected " +
                "Monster Count from the Monster Spawner's bound Wave Config.",
                this);
        }

        if (monsterSpawner != null &&
            monsterSpawner.TryGetExpectedWaveCount(
                out int resolvedExpectedWaveCount))
        {
            expectedWaveCount = resolvedExpectedWaveCount;
            hasExpectedWaveCount = true;
        }
    }

    private void TrackCurrentMonsters()
    {
        if (monsterManager == null)
        {
            return;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters =
            monsterManager.GetAliveMonsters();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            TrackMonster(
                aliveMonsters[i],
                observedThroughRegistrationEvent: false);
        }
    }

    private void TrackMonster(
        MonsterBehaviour monster,
        bool observedThroughRegistrationEvent)
    {
        if (monster == null)
        {
            return;
        }

        int instanceId = monster.GetInstanceID();

        if (!seenMonsterInstanceIds.Add(instanceId))
        {
            return;
        }

        float observedTime = Time.time;
        string runtimeTemplateName = ResolveRuntimeTemplateName(monster.name);
        MonsterObservation observation =
            new MonsterObservation(
                runtimeTemplateName,
                GetDisplayName(monster.DisplayName, runtimeTemplateName),
                monster.MaxHealth,
                monster.CurrentMoveSpeed,
                monster.CurrentHealth,
                spawnedCount + 1,
                observedTime,
                GetRunActiveTimeSeconds(),
                observedThroughRegistrationEvent);
        trackedMonsters.Add(monster, observation);
        monsterObservations.Add(observation);
        effectiveDamage += Mathf.Max(
            0,
            monster.MaxHealth - monster.CurrentHealth);
        monster.OnHealthChanged += HandleMonsterHealthChanged;
        monster.OnResolved += HandleMonsterResolved;
        monster.OnDestroyed += HandleMonsterDestroyed;
        monster.OnBuffRuntimeObserved += HandleBuffRuntimeObserved;
        monster.OnElementalHitReactionObserved +=
            HandleElementalHitReactionObserved;

        if (hasObservedFirstSpawn)
        {
            observedSpawnIntervalTotal += observedTime - lastSpawnTime;
            observedSpawnIntervalCount++;
        }
        else
        {
            firstSpawnTime = observedTime;
            hasObservedFirstSpawn = true;
        }

        lastSpawnTime = observedTime;
        spawnedCount++;
        observedTotalMonsterMaxHealth += monster.MaxHealth;
        observedMinimumMonsterHealth = Mathf.Min(
            observedMinimumMonsterHealth,
            monster.MaxHealth);
        observedMaximumMonsterHealth = Mathf.Max(
            observedMaximumMonsterHealth,
            monster.MaxHealth);
        observedMinimumMonsterSpeed = Mathf.Min(
            observedMinimumMonsterSpeed,
            monster.CurrentMoveSpeed);
        observedMaximumMonsterSpeed = Mathf.Max(
            observedMaximumMonsterSpeed,
            monster.CurrentMoveSpeed);
    }

    private void HandleMonsterHealthChanged(
        MonsterBehaviour monster,
        int currentHealth,
        int _)
    {
        if (monster == null ||
            !trackedMonsters.TryGetValue(
                monster,
                out MonsterObservation observation))
        {
            return;
        }

        int appliedDamage = Mathf.Max(
            0,
            observation.LastHealth - currentHealth);
        effectiveDamage += appliedDamage;

        if (appliedDamage > 0)
        {
            observation.SuccessfulDamageApplications++;
            observation.EffectiveDamage += appliedDamage;
        }

        observation.LastHealth = currentHealth;
    }

    private void HandleMonsterRegistered(MonsterBehaviour monster)
    {
        if (isTrackingRun)
        {
            TrackMonster(
                monster,
                observedThroughRegistrationEvent: true);
        }
    }

    private void HandleTowerDeploymentCommitted(TowerInstance towerInstance)
    {
        if (!isTrackingRun || towerInstance == null)
        {
            return;
        }

        int instanceId = towerInstance.GetInstanceID();

        if (towerDeploymentByInstanceId.ContainsKey(instanceId))
        {
            return;
        }

        TowerDeploymentObservation observation =
            new TowerDeploymentObservation
            {
                DeploymentOrdinal = towerDeploymentByInstanceId.Count + 1,
                DeployedAtSeconds = GetRunActiveTimeSeconds(),
                WorldPosition = towerInstance.transform.position
            };
        IReadOnlyList<GridNodeBehaviour> occupiedNodes =
            towerInstance.OccupiedNodes;

        if (occupiedNodes != null)
        {
            for (int i = 0; i < occupiedNodes.Count; i++)
            {
                GridNodeBehaviour node = occupiedNodes[i];

                if (node != null)
                {
                    observation.GridPositions.Add(node.GridPosition);
                }
            }
        }

        observation.GridPositions.Sort(CompareGridPositions);
        towerDeploymentByInstanceId.Add(instanceId, observation);
    }

    private void HandlePlacementRouteRevisionCommitted(
        TowerInstance towerInstance,
        TowerPlacementTopologyPlan topologyPlan,
        MonsterRouteRevisionBatch revisionBatch)
    {
        if (!isTrackingRun || topologyPlan == null || revisionBatch == null)
        {
            return;
        }

        CombatBalancePlacementRouteCommitJson commit =
            new CombatBalancePlacementRouteCommitJson
            {
                ordinal = placementRouteCommits.Count + 1,
                towerInstanceId = towerInstance != null
                    ? towerInstance.GetInstanceID()
                    : 0,
                activeTimeSeconds = GetRunActiveTimeSeconds(),
                playerHealthBefore = revisionBatch.PlayerHealthBefore,
                playerHealthAfter = revisionBatch.PlayerHealthAfter,
                playerProgressBefore = revisionBatch.PlayerProgressBefore,
                playerProgressAfter = revisionBatch.PlayerProgressAfter,
                aliveMonsterCountBefore =
                    revisionBatch.AliveMonsterCountBefore,
                aliveMonsterCountAfter = revisionBatch.AliveMonsterCountAfter,
                resolvedMonsterCountBefore =
                    revisionBatch.ResolvedMonsterCountBefore,
                resolvedMonsterCountAfter =
                    revisionBatch.ResolvedMonsterCountAfter,
                alreadyOnNewRouteCount =
                    revisionBatch.AlreadyOnNewRouteCount,
                reachableRouteRejoinCount =
                    revisionBatch.ReachableRouteRejoinCount,
                forcedRelocationCount =
                    revisionBatch.ForcedRelocationCount,
                combatOwnershipFingerprintBefore =
                    revisionBatch.CombatOwnershipFingerprintBefore,
                combatOwnershipFingerprintAfter =
                    revisionBatch.CombatOwnershipFingerprintAfter
            };
        AppendGridPositions(commit.footprint, topologyPlan.Footprint);
        AppendGridPositions(
            commit.authoritativeRoute,
            topologyPlan.AuthoritativeRoute);

        for (int i = 0; i < revisionBatch.Entries.Count; i++)
        {
            MonsterRouteRevisionEntry entry = revisionBatch.Entries[i];
            trackedMonsters.TryGetValue(
                entry.Monster,
                out MonsterObservation runtimeObservation);
            bool hasComparableImmediateDisplacement =
                entry.HasComparableCapturedPosition &&
                IsFinite(entry.ImmediatePostCommitPosition);
            CombatBalancePlacementRouteMonsterJson monster =
                new CombatBalancePlacementRouteMonsterJson
                {
                    revisionId = entry.RevisionId,
                    monsterInstanceId = entry.Monster != null
                        ? entry.Monster.GetInstanceID()
                        : 0,
                    spawnOrdinal = GetSpawnOrdinal(entry.Monster),
                    sourceWaveNumber = runtimeObservation != null
                        ? runtimeObservation.SourceWaveNumber
                        : 0,
                    sourceWaveSpawnOrdinal = runtimeObservation != null
                        ? runtimeObservation.SourceWaveSpawnOrdinal
                        : 0,
                    mode = entry.Mode.ToString(),
                    forcedRelocationReason =
                        entry.RelocationReason.ToString(),
                    capturedWorldPosition =
                        CreateVector3Json(entry.CapturedWorldPosition),
                    immediatePostCommitPosition = CreateVector3Json(
                        entry.ImmediatePostCommitPosition),
                    preparedWorldPosition =
                        CreateVector3Json(entry.PreparedWorldPosition),
                    hasComparableCapturedPosition =
                        entry.HasComparableCapturedPosition,
                    hasComparableImmediateDisplacement =
                        hasComparableImmediateDisplacement,
                    immediateDisplacement =
                        hasComparableImmediateDisplacement
                            ? Vector3.Distance(
                                entry.CapturedWorldPosition,
                                entry.ImmediatePostCommitPosition)
                            : 0f,
                    hasComparableRelocationDistance =
                        entry.HasComparableRelocationDistance,
                    plannedRelocationDistance = entry.RelocationDistance,
                    plannedConnectorDistance =
                        entry.PlannedConnectorDistance,
                    requiresExactTargetApproach =
                        entry.RequiresExactTargetApproach,
                    requiresConnector = entry.RequiresConnector,
                    joinedAtCommit = entry.JoinedAtCommit,
                    physicalGrid = CreateGridPositionJson(
                        entry.PhysicalCurrentGrid),
                    joinGrid = CreateGridPositionJson(entry.JoinGrid),
                    recoveryGrid = CreateGridPositionJson(entry.RecoveryGrid),
                    preGameplayStateFingerprint =
                        CreatePlacementGameplayStateFingerprint(entry.PreState),
                    postGameplayStateFingerprint =
                        CreatePlacementGameplayStateFingerprint(entry.PostState)
                };
            AppendGridPositions(monster.connectorPath, entry.ConnectorPath);
            AppendGridPositions(
                monster.preparedContinuation,
                entry.PreparedRoute);
            AppendGridPositions(monster.routeSuffix, entry.RouteSuffix);
            commit.monsters.Add(monster);
        }

        placementRouteCommits.Add(commit);
    }

    private void HandlePlacementRouteLifecycleObserved(
        MonsterPlacementRouteLifecycleObservation observation)
    {
        if (!isTrackingRun)
        {
            return;
        }

        placementRouteLifecycle.Add(
            new CombatBalancePlacementRouteLifecycleJson
            {
                revisionId = observation.RevisionId,
                replacementRevisionId = observation.ReplacementRevisionId,
                monsterInstanceId = observation.Monster != null
                    ? observation.Monster.GetInstanceID()
                    : 0,
                spawnOrdinal = GetSpawnOrdinal(observation.Monster),
                kind = observation.Kind.ToString(),
                resolutionReason = observation.ResolutionReason.ToString(),
                joinedAtCommit = observation.JoinedAtCommit,
                activeTimeSeconds = GetRunActiveTimeSeconds()
            });
    }

    private void HandleTowerInvestmentCommitted(
        TowerInvestmentCommitObservation observation)
    {
        if (!isTrackingRun ||
            !observation.DraftAttemptToken.IsValid ||
            observation.DraftResult == null ||
            observation.TowerInstance == null)
        {
            return;
        }

        TowerInstance tower = observation.TowerInstance;
        TowerDefinition towerDefinition = tower.TowerDefinition;
        TowerUpgradeDefinition upgradeDefinition =
            observation.DraftResult.TowerUpgradeDefinition;
        int draftOrdinal = ResolveDraftOrdinal(
            observation.DraftAttemptToken);
        investmentCommits.Add(new CombatBalanceInvestmentCommitJson
        {
            ordinal = investmentCommits.Count + 1,
            kind = observation.Kind.ToString(),
            authoritySource = draftOrdinal > 0
                ? "Draft"
                : "EditorDebug",
            draftAttemptToken = observation.DraftAttemptToken.ToString(),
            draftOrdinal = draftOrdinal,
            draftResultType = observation.DraftResult.ResultType.ToString(),
            draftAssetName = GetAssetName(
                observation.DraftResult.Identity),
            towerInstanceId = tower.GetInstanceID(),
            towerDisplayName = towerDefinition != null
                ? towerDefinition.DisplayName
                : tower.name,
            towerFamily = towerDefinition != null
                ? towerDefinition.TowerFamily.ToString()
                : string.Empty,
            previousLevel = observation.PreviousLevel,
            currentLevel = observation.CurrentLevel,
            upgradeLayer = upgradeDefinition != null
                ? upgradeDefinition.UpgradeLayer.ToString()
                : string.Empty,
            activeTimeSeconds = GetRunActiveTimeSeconds(),
            resolvedMonsterCount = resolvedCount,
            spawned = spawnedCount,
            resolved = resolvedCount,
            killed = killedCount,
            leaked = leakedCount,
            alive = monsterManager != null
                ? monsterManager.AliveMonsterCount
                : Mathf.Max(0, spawnedCount - resolvedCount),
            playerHealth = playerSystem != null
                ? playerSystem.CurrentHealth
                : 0
        });
    }

    private int ResolveDraftOrdinal(DraftAttemptToken attemptToken)
    {
        string token = attemptToken.ToString();

        for (int i = draftAttempts.Count - 1; i >= 0; i--)
        {
            if (string.Equals(
                    draftAttempts[i].attemptToken,
                    token,
                    StringComparison.Ordinal))
            {
                return draftAttempts[i].ordinal;
            }
        }

        return 0;
    }

    private static int CompareGridPositions(
        Vector2Int left,
        Vector2Int right)
    {
        int xComparison = left.x.CompareTo(right.x);
        return xComparison != 0
            ? xComparison
            : left.y.CompareTo(right.y);
    }

    private void HandleBuffRuntimeObserved(
        MonsterBehaviour monster,
        BuffRuntimeObservation observation)
    {
        if (!isTrackingRun)
        {
            return;
        }

        MonsterBehaviour ownerMonster = observation.OwnerMonster != null
            ? observation.OwnerMonster
            : monster;
        int sourceWaveNumber = ownerMonster != null &&
            trackedMonsters.TryGetValue(
                ownerMonster,
                out MonsterObservation monsterObservation)
                ? monsterObservation.SourceWaveNumber
                : 0;
        buffAccumulator.Consume(observation, sourceWaveNumber);

        if (observation.ObservationType ==
                BuffRuntimeObservationType.ApplicationAttempt &&
            observation.BuffDefinition != null &&
            observation.BuffDefinition.ElementType != ElementType.None &&
            observation.SourceTower != null &&
            observation.SourceUpgrade != null)
        {
            string sourceWaveKey = CreateElementalSourceWaveKey(
                observation.SourceTower,
                observation.SourceUpgrade,
                sourceWaveNumber);
            elementalBuffApplicationAttemptsBySourceWave.TryGetValue(
                sourceWaveKey,
                out int currentCount);
            elementalBuffApplicationAttemptsBySourceWave[sourceWaveKey] =
                currentCount + 1;
        }
    }

    private void HandleElementalOpportunityObserved(
        ElementalOpportunityObservation observation)
    {
        if (!isTrackingRun ||
            observation.SourceTower == null ||
            observation.ElementalUpgrade == null ||
            observation.TargetMonster == null ||
            !observation.Diagnostics.IsValid)
        {
            return;
        }

        int waveNumber = trackedMonsters.TryGetValue(
                observation.TargetMonster,
                out MonsterObservation monsterObservation)
            ? monsterObservation.SourceWaveNumber
            : 0;
        string scopeKey = CreateElementalOpportunityScopeKey(
            observation,
            waveNumber);

        if (!elementalOpportunityByScope.TryGetValue(
                scopeKey,
                out ElementalOpportunityAggregate aggregate))
        {
            aggregate = new ElementalOpportunityAggregate(
                observation,
                waveNumber);
            elementalOpportunityByScope.Add(scopeKey, aggregate);
        }

        aggregate.MinimumResultOrdinal = Mathf.Min(
            aggregate.MinimumResultOrdinal,
            observation.Diagnostics.ResultOrdinal);
        aggregate.MaximumResultOrdinal = Mathf.Max(
            aggregate.MaximumResultOrdinal,
            observation.Diagnostics.ResultOrdinal);

        switch (observation.ObservationType)
        {
            case ElementalOpportunityObservationType.Candidate:
                aggregate.CandidateResults++;

                if (observation.Diagnostics.TopologyAuthorized)
                {
                    aggregate.EligibleResults++;
                }

                break;
            case ElementalOpportunityObservationType.Dispatched:
                aggregate.DispatchedRequests++;
                break;
        }
    }

    private void HandleElementalHitReactionObserved(
        MonsterBehaviour monster,
        ElementalHitReactionObservation observation)
    {
        if (!isTrackingRun)
        {
            return;
        }

        MonsterBehaviour ownerMonster = observation.OwnerMonster != null
            ? observation.OwnerMonster
            : monster;
        int sourceWaveNumber = ownerMonster != null &&
            trackedMonsters.TryGetValue(
                ownerMonster,
                out MonsterObservation monsterObservation)
                ? monsterObservation.SourceWaveNumber
                : 0;
        elementalHitReactionAccumulator.Consume(
            observation,
            sourceWaveNumber);
    }

    private void HandleTowerOwnedDamageResolutionObserved(
        TowerOwnedDamageResolutionObservation observation)
    {
        if (!isTrackingRun)
        {
            return;
        }

        if (observation.IsResolved)
        {
            TowerOwnedDamageResolution resolution = observation.Resolution;
            string signature = CreateTowerScaledDamageSignature(resolution);

            if (!towerScaledDamageBySignature.TryGetValue(
                    signature,
                    out TowerScaledDamageAggregate aggregate))
            {
                aggregate = new TowerScaledDamageAggregate(resolution);
                towerScaledDamageBySignature.Add(signature, aggregate);
            }

            aggregate.ResolutionCount++;
            return;
        }

        string rejectionSignature =
            CreateTowerScaledRejectionSignature(observation);

        if (!towerScaledRejectionBySignature.TryGetValue(
                rejectionSignature,
                out TowerScaledRejectionAggregate rejectionAggregate))
        {
            rejectionAggregate =
                new TowerScaledRejectionAggregate(observation);
            towerScaledRejectionBySignature.Add(
                rejectionSignature,
                rejectionAggregate);
        }

        rejectionAggregate.RejectionCount++;
    }

    private void HandleTowerOwnedDamageApplicationObserved(
        TowerOwnedDamageApplicationObservation observation)
    {
        if (!isTrackingRun || observation.SuccessfulApplicationCount <= 0)
        {
            return;
        }

        TowerOwnedDamageResolution resolution = observation.Resolution;
        string signature = CreateTowerScaledDamageSignature(resolution);

        if (!towerScaledDamageBySignature.TryGetValue(
                signature,
                out TowerScaledDamageAggregate aggregate))
        {
            aggregate = new TowerScaledDamageAggregate(resolution);
            towerScaledDamageBySignature.Add(signature, aggregate);
        }

        aggregate.SuccessfulApplicationCount +=
            observation.SuccessfulApplicationCount;
        aggregate.AppliedDamageTotal +=
            resolution.FinalDamage * observation.SuccessfulApplicationCount;
    }

    private void HandleTowerOwnedTargetDamageObserved(
        TowerOwnedTargetDamageObservation observation)
    {
        if (!isTrackingRun ||
            observation.Target == null ||
            observation.AppliedDamage <= 0 ||
            observation.Resolution.SourceTower == null)
        {
            return;
        }

        if (!trackedMonsters.TryGetValue(
                observation.Target,
                out MonsterObservation monsterObservation))
        {
            TrackMonster(
                observation.Target,
                observedThroughRegistrationEvent: false);
            trackedMonsters.TryGetValue(
                observation.Target,
                out monsterObservation);
        }

        int waveNumber = monsterObservation != null
            ? monsterObservation.SourceWaveNumber
            : 0;
        TowerInstance tower = observation.Resolution.SourceTower;
        string scopeKey = tower.GetInstanceID().ToString(
            CultureInfo.InvariantCulture) + ":" +
            waveNumber.ToString(CultureInfo.InvariantCulture);

        if (!towerWaveDamageByScope.TryGetValue(
                scopeKey,
                out TowerWaveDamageAggregate aggregate))
        {
            aggregate = new TowerWaveDamageAggregate
            {
                TowerInstanceId = tower.GetInstanceID(),
                TowerDisplayName = tower.TowerDefinition != null
                    ? tower.TowerDefinition.DisplayName
                    : tower.name,
                TowerFamily = tower.TowerDefinition != null
                    ? tower.TowerDefinition.TowerFamily.ToString()
                    : string.Empty,
                WaveNumber = waveNumber
            };
            towerWaveDamageByScope.Add(scopeKey, aggregate);
        }

        aggregate.SuccessfulDamageApplications++;
        aggregate.EffectiveTowerScaledDamage += observation.AppliedDamage;

        if (observation.KillingBlow)
        {
            aggregate.KillingBlows++;
        }

        RecordTowerRouteDamageCoverage(observation, tower);
    }

    private void RecordTowerRouteDamageCoverage(
        TowerOwnedTargetDamageObservation observation,
        TowerInstance tower)
    {
        int towerInstanceId = tower.GetInstanceID();

        if (!towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
                towerInstanceId,
                out TowerRouteDamageCoverageAggregate aggregate))
        {
            aggregate = new TowerRouteDamageCoverageAggregate
            {
                TowerInstanceId = towerInstanceId,
                TowerDisplayName = tower.TowerDefinition != null
                    ? tower.TowerDefinition.DisplayName
                    : tower.name,
                TowerFamily = tower.TowerDefinition != null
                    ? tower.TowerDefinition.TowerFamily.ToString()
                    : string.Empty
            };
            towerRouteDamageCoverageByTowerInstanceId.Add(
                towerInstanceId,
                aggregate);
        }

        int targetInstanceId = observation.Target.GetInstanceID();
        aggregate.SuccessfulDamageApplications++;
        aggregate.EffectiveDamage += observation.AppliedDamage;
        aggregate.DamagedMonsterInstanceIds.Add(targetInstanceId);

        if (observation.KillingBlow)
        {
            aggregate.KillingBlows++;
        }

        GridNodeBehaviour routeStateNode = observation.Target.CurrentNode;

        if (routeStateNode == null)
        {
            aggregate.UnresolvedRouteCellApplicationCount++;
            aggregate.UnresolvedRouteCellEffectiveDamage +=
                observation.AppliedDamage;

            if (observation.KillingBlow)
            {
                aggregate.UnresolvedRouteCellKillingBlows++;
            }

            return;
        }

        aggregate.LocatedDamageApplications++;
        aggregate.LocatedEffectiveDamage += observation.AppliedDamage;

        if (observation.KillingBlow)
        {
            aggregate.LocatedKillingBlows++;
        }

        Vector2Int gridPosition = routeStateNode.GridPosition;
        int placementCommitOrdinal = placementRouteCommits.Count;
        string cellKey = placementCommitOrdinal.ToString(
            CultureInfo.InvariantCulture) + ":" +
            gridPosition.x.ToString(CultureInfo.InvariantCulture) + ":" +
            gridPosition.y.ToString(CultureInfo.InvariantCulture);

        if (!aggregate.Cells.TryGetValue(
                cellKey,
                out TowerRouteDamageCellAggregate cell))
        {
            cell = new TowerRouteDamageCellAggregate
            {
                PlacementCommitOrdinal = placementCommitOrdinal,
                X = gridPosition.x,
                Z = gridPosition.y
            };
            aggregate.Cells.Add(cellKey, cell);
        }

        cell.SuccessfulDamageApplications++;
        cell.EffectiveDamage += observation.AppliedDamage;
        cell.DamagedMonsterInstanceIds.Add(targetInstanceId);

        if (observation.KillingBlow)
        {
            cell.KillingBlows++;
        }
    }

    private void HandleFixedBuffDamageObserved(
        FixedBuffDamageObservation observation)
    {
        if (!isTrackingRun)
        {
            return;
        }

        string signature = CreateFixedBuffDamageSignature(observation);

        if (!fixedBuffDamageBySignature.TryGetValue(
                signature,
                out FixedBuffDamageAggregate aggregate))
        {
            aggregate = new FixedBuffDamageAggregate(observation);
            fixedBuffDamageBySignature.Add(signature, aggregate);
        }

        aggregate.ResolutionCount++;
        aggregate.ResolvedTargetCount += observation.ResolvedTargetCount;
        aggregate.SuccessfulApplicationCount +=
            observation.SuccessfulApplicationCount;
        aggregate.AppliedDamageTotal +=
            observation.FixedDamage * observation.SuccessfulApplicationCount;

        if (observation.SourceTower != null)
        {
            aggregate.SourceTowerInstanceIds.Add(
                observation.SourceTower.GetInstanceID());
        }
    }

    private static string CreateTowerScaledDamageSignature(
        TowerOwnedDamageResolution resolution)
    {
        TowerDamageSourceIdentity sourceIdentity =
            resolution.DamageSourceIdentity;
        return string.Join(
            "|",
            resolution.SourceTower != null
                ? resolution.SourceTower.GetInstanceID().ToString(
                    CultureInfo.InvariantCulture)
                : "0",
            resolution.TowerFamily.ToString(),
            resolution.Level.ToString(CultureInfo.InvariantCulture),
            resolution.LevelBasicDamage.ToString(CultureInfo.InvariantCulture),
            resolution.RawDamageBonus.ToString("R", CultureInfo.InvariantCulture),
            resolution.ResolvedBasicDamage.ToString("R", CultureInfo.InvariantCulture),
            sourceIdentity.SourceType.ToString(),
            GetEffectIdentity(sourceIdentity.EffectDefinition),
            sourceIdentity.ActionOrdinal.ToString(CultureInfo.InvariantCulture),
            resolution.DamageScale.ToString("R", CultureInfo.InvariantCulture),
            resolution.RawProduct.ToString("R", CultureInfo.InvariantCulture),
            resolution.FinalDamage.ToString(CultureInfo.InvariantCulture));
    }

    private static string CreateTowerScaledRejectionSignature(
        TowerOwnedDamageResolutionObservation observation)
    {
        TowerDamageSourceIdentity sourceIdentity =
            observation.DamageSourceIdentity;
        return string.Join(
            "|",
            observation.SourceTower != null
                ? observation.SourceTower.GetInstanceID().ToString(
                    CultureInfo.InvariantCulture)
                : "0",
            sourceIdentity.SourceType.ToString(),
            GetEffectIdentity(sourceIdentity.EffectDefinition),
            sourceIdentity.ActionOrdinal.ToString(CultureInfo.InvariantCulture),
            observation.DamageScale.ToString("R", CultureInfo.InvariantCulture),
            observation.FailureReason.ToString());
    }

    private static string CreateFixedBuffDamageSignature(
        FixedBuffDamageObservation observation)
    {
        return CreateFixedBuffDamageSignature(
            observation.EffectDefinition,
            observation.ActionOrdinal,
            observation.FixedDamage);
    }

    private static string CreateFixedBuffDamageSignature(
        EffectDefinition effectDefinition,
        int actionOrdinal,
        int fixedDamage)
    {
        return string.Join(
            "|",
            GetEffectIdentity(effectDefinition),
            actionOrdinal.ToString(CultureInfo.InvariantCulture),
            fixedDamage.ToString(CultureInfo.InvariantCulture));
    }

    private static string CreateElementalOpportunityScopeKey(
        ElementalOpportunityObservation observation,
        int waveNumber)
    {
        ElementalOpportunityDiagnosticContext diagnostics =
            observation.Diagnostics;
        return string.Join(
            "|",
            observation.SourceTower.GetInstanceID().ToString(
                CultureInfo.InvariantCulture),
            observation.ElementalUpgrade.GetInstanceID().ToString(
                CultureInfo.InvariantCulture),
            waveNumber.ToString(CultureInfo.InvariantCulture),
            diagnostics.Provenance.ToString(),
            diagnostics.MemberIdentity.ToString(),
            diagnostics.ResultRole.ToString());
    }

    private static string CreateElementalSourceWaveKey(
        TowerInstance sourceTower,
        TowerUpgradeDefinition elementalUpgrade,
        int waveNumber)
    {
        return string.Join(
            "|",
            sourceTower.GetInstanceID().ToString(CultureInfo.InvariantCulture),
            elementalUpgrade.GetInstanceID().ToString(
                CultureInfo.InvariantCulture),
            waveNumber.ToString(CultureInfo.InvariantCulture));
    }

    private static string GetEffectIdentity(EffectDefinition effectDefinition)
    {
        return effectDefinition != null
            ? effectDefinition.GetInstanceID().ToString(
                CultureInfo.InvariantCulture)
            : "0";
    }

    private void HandleProjectileRuntimeObserved(
        ProjectileRuntimeObservation observation)
    {
        if (!isTrackingRun || observation.SourceTower == null)
        {
            return;
        }

        int towerInstanceId = observation.SourceTower.GetInstanceID();

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
        TowerInstance sourceTower,
        int sourceDroneInstanceId,
        bool isAdditionalDrone)
    {
        int towerInstanceId = sourceTower.GetInstanceID();

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

    private void HandleDroneLifecycleRuntimeObserved(
        DroneLifecycleRuntimeObservation observation)
    {
        if (!isTrackingRun ||
            observation.SourceTower == null ||
            observation.SourceDroneInstanceId == 0)
        {
            return;
        }

        DroneRuntimeAggregate drone = GetOrCreateDroneRuntimeAggregate(
            observation.SourceTower,
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

    private void HandleDroneBurstRuntimeObserved(
        DroneBurstRuntimeObservation observation)
    {
        if (!isTrackingRun ||
            observation.SourceTower == null ||
            observation.SourceDroneInstanceId == 0 ||
            observation.Count <= 0)
        {
            return;
        }

        DroneRuntimeAggregate drone = GetOrCreateDroneRuntimeAggregate(
            observation.SourceTower,
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

    private int GetSpawnOrdinal(MonsterBehaviour monster)
    {
        return monster != null &&
               trackedMonsters.TryGetValue(
                   monster,
                   out MonsterObservation observation)
            ? observation.SpawnOrdinal
            : 0;
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

    private void HandleMonsterResolved(
        MonsterBehaviour monster,
        bool reachedTarget)
    {
        if (monster == null)
        {
            return;
        }

        if (!trackedMonsters.TryGetValue(
                monster,
                out MonsterObservation observation))
        {
            TrackMonster(
                monster,
                observedThroughRegistrationEvent: false);
            trackedMonsters.TryGetValue(monster, out observation);
        }

        if (observation == null || observation.IsResolved)
        {
            return;
        }

        observation.IsResolved = true;
        observation.ReachedTarget = reachedTarget;
        observation.ResolutionTime = Time.time;
        observation.ResolvedAtSeconds = GetRunActiveTimeSeconds();
        observation.FinalHealth = monster.CurrentHealth;
        resolvedCount++;
        lastResolutionTime = observation.ResolutionTime;

        if (reachedTarget)
        {
            leakedCount++;
            leakedRemainingHealth += monster.CurrentHealth;
        }
        else
        {
            killedCount++;
        }

        // PlayerSystem publishes Level-up before this resolution callback.
        // Flush after classifying this resolution, but before another resolution
        // can shift the Draft node or its pressure snapshot.
        FlushPendingProgressionEvents();
    }

    private void HandleMonsterDestroyed(MonsterBehaviour monster)
    {
        UnsubscribeFromMonster(monster);
    }

    private void HandleWaveSpawningStarted(
        int waveIndex,
        MonsterWaveEntry wave)
    {
        CaptureWaveEvent("SpawnStarted", waveIndex, wave);
    }

    private void HandleWaveSpawningCompleted(
        int waveIndex,
        MonsterWaveEntry wave)
    {
        CaptureWaveEvent("SpawnCompleted", waveIndex, wave);
    }

    private void HandleMonsterSpawnedFromWave(
        int waveIndex,
        int spawnIndex,
        MonsterBehaviour monster)
    {
        if (!isTrackingRun || monster == null)
        {
            return;
        }

        if (!trackedMonsters.TryGetValue(
                monster,
                out MonsterObservation observation))
        {
            TrackMonster(
                monster,
                observedThroughRegistrationEvent: false);
            trackedMonsters.TryGetValue(monster, out observation);
        }

        if (observation == null)
        {
            return;
        }

        observation.SourceWaveNumber = waveIndex + 1;
        observation.SourceWaveSpawnOrdinal = spawnIndex + 1;
    }

    private void CaptureWaveEvent(
        string kind,
        int waveIndex,
        MonsterWaveEntry wave)
    {
        if (!isTrackingRun || wave == null)
        {
            return;
        }

        MonsterBehaviour runtimeTemplate = wave.MonsterRuntimeTemplate;
        waveEvents.Add(new CombatBalanceWaveEventJson
        {
            ordinal = waveEvents.Count + 1,
            kind = kind,
            waveNumber = waveIndex + 1,
            runtimeTemplateName = runtimeTemplate != null
                ? ResolveRuntimeTemplateName(runtimeTemplate.name)
                : string.Empty,
            configuredCount = wave.Count,
            configuredSpawnIntervalSeconds = wave.SpawnInterval,
            configuredWaveDelaySeconds = wave.WaveDelay,
            activeTimeSeconds = GetRunActiveTimeSeconds(),
            spawned = spawnedCount,
            resolved = resolvedCount,
            killed = killedCount,
            leaked = leakedCount,
            alive = monsterManager != null
                ? monsterManager.AliveMonsterCount
                : Mathf.Max(0, spawnedCount - resolvedCount),
            playerHealth = playerSystem != null
                ? playerSystem.CurrentHealth
                : 0
        });
    }

    private void HandleAllSpawningCompleted()
    {
        hasObservedSpawningCompletion = true;
        spawningCompletedTime = Time.time;
    }

    private void HandleBattleResultPublished(BattleResult result)
    {
        QueueFinalSummary(result.ToString(), null);
    }

    private void HandleBattleRuntimeFailed(string failureReason)
    {
        QueueFinalSummary("TechnicalFailure", failureReason);
    }

    private void QueueFinalSummary(string terminalState, string failureReason)
    {
        if (hasLoggedFinalSummary || hasPendingFinalSummary)
        {
            return;
        }

        pendingTerminalState = terminalState;
        pendingFailureReason = failureReason;
        hasPendingFinalSummary = true;
    }

    private void FlushPendingFinalSummary()
    {
        if (!hasPendingFinalSummary)
        {
            return;
        }

        string terminalState = pendingTerminalState;
        string failureReason = pendingFailureReason;
        hasPendingFinalSummary = false;
        pendingTerminalState = null;
        pendingFailureReason = null;
        LogFinalSummary(terminalState, failureReason);
    }

    private void LogFinalSummary(string terminalState, string failureReason)
    {
        if (hasLoggedFinalSummary)
        {
            return;
        }

        FlushPendingProgressionEvents();
        TrackCurrentMonsters();

        if (!hasExpectedMonsterCount)
        {
            CaptureExpectedMonsterFixture();
        }

        hasLoggedFinalSummary = true;
        DateTime generatedAt = DateTime.Now;
        string summary = BuildSummary(
            terminalState,
            failureReason,
            generatedAt);
        string reportPath = TryWriteJsonReport(
            terminalState,
            failureReason,
            generatedAt);

        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            summary += "\nReport JSON: " + reportPath;
        }

        Debug.Log(summary, this);
        isTrackingRun = false;
        UnsubscribeFromTrackedMonsters();
    }

    private string BuildSummary(
        string terminalState,
        string failureReason,
        DateTime generatedAt)
    {
        StringBuilder builder = new StringBuilder(1024);
        int unresolvedCount = Mathf.Max(0, spawnedCount - resolvedCount);
        int notSpawnedCount = hasExpectedMonsterCount
            ? Mathf.Max(0, expectedMonsterCount - spawnedCount)
            : 0;
        float killRate = spawnedCount > 0
            ? (float)killedCount / spawnedCount
            : 0f;
        float averageObservedSpawnInterval = observedSpawnIntervalCount > 0
            ? observedSpawnIntervalTotal / observedSpawnIntervalCount
            : 0f;
        float damageCoverage = observedTotalMonsterMaxHealth > 0
            ? (float)effectiveDamage / observedTotalMonsterMaxHealth
            : 0f;
        float averageLeakedRemainingHealth = leakedCount > 0
            ? (float)leakedRemainingHealth / leakedCount
            : 0f;
        float battleDuration = hasObservedFirstSpawn
            ? Mathf.Max(
                0f,
                (lastResolutionTime > 0f ? lastResolutionTime : Time.time) -
                firstSpawnTime)
            : 0f;
        float spawnSpan = hasObservedFirstSpawn
            ? Mathf.Max(0f, lastSpawnTime - firstSpawnTime)
            : 0f;
        float spawningCompletionOffset =
            hasObservedFirstSpawn && hasObservedSpawningCompletion
                ? Mathf.Max(0f, spawningCompletedTime - firstSpawnTime)
                : 0f;
        int observedPlayerHealthLoss = playerSystem != null
            ? Mathf.Max(0, initialPlayerHealth - playerSystem.CurrentHealth)
            : 0;
        bool resolutionCountsMatch =
            resolvedCount == killedCount + leakedCount;
        bool leakCountMatchesPlayerHealth =
            playerSystem != null && leakedCount == observedPlayerHealthLoss;
        CombatBalanceMonsterRuntimeJson monsterRuntime =
            CreateMonsterRuntimeJson();
        bool monsterRuntimeCountsMatch =
            MonsterRuntimeCountsMatch(monsterRuntime);
        bool monsterRuntimeDamageMatches =
            MonsterRuntimeDamageMatches(monsterRuntime);
        bool monsterRuntimeRegistrationCoverageMatch =
            MonsterRuntimeRegistrationCoverageMatches(monsterRuntime);
        bool monsterRuntimeStartedAtFullHealth =
            MonsterRuntimeStartedAtFullHealth(monsterRuntime);

        builder.AppendLine("[Combat Balance Run]");
        builder.Append("Run: ")
            .AppendLine(ResolveRunName(generatedAt));
        builder.Append("Terminal State: ").AppendLine(terminalState);

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            builder.Append("Failure: ").AppendLine(failureReason);
        }

        builder.Append("Monsters: Expected=")
            .Append(hasExpectedMonsterCount
                ? expectedMonsterCount.ToString(CultureInfo.InvariantCulture)
                : "Unavailable")
            .Append(", Spawned=")
            .Append(spawnedCount)
            .Append(", Resolved=")
            .Append(resolvedCount)
            .Append(", Killed=")
            .Append(killedCount)
            .Append(", Leaked=")
            .Append(leakedCount)
            .Append(", Unresolved=")
            .Append(unresolvedCount)
            .Append(", NotSpawned=")
            .Append(notSpawnedCount)
            .Append(", KillRate=")
            .Append(FormatPercent(killRate))
            .AppendLine();
        builder.Append("Monster Fixture: WaveConfig=")
            .Append(string.IsNullOrWhiteSpace(waveConfigName)
                ? "Unavailable"
                : waveConfigName)
            .Append(", HP=")
            .Append(FormatObservedIntRange(
                observedMinimumMonsterHealth,
                observedMaximumMonsterHealth))
            .Append(", Speed=")
            .Append(FormatObservedFloatRange(
                observedMinimumMonsterSpeed,
                observedMaximumMonsterSpeed))
            .Append(", ObservedAverageSpawnInterval=")
            .Append(FormatSeconds(averageObservedSpawnInterval))
            .AppendLine();
        builder.Append("Damage: EffectiveDamage=")
            .Append(effectiveDamage)
            .Append(", TotalObservedHP=")
            .Append(observedTotalMonsterMaxHealth)
            .Append(", DamageCoverage=")
            .Append(FormatPercent(damageCoverage))
            .Append(", LeakedRemainingHP=")
            .Append(leakedRemainingHealth)
            .Append(", AverageLeakedRemainingHP=")
            .Append(FormatFloat(averageLeakedRemainingHealth))
            .AppendLine();
        builder.Append("Pressure: PeakAlive=")
            .Append(peakAliveCount)
            .AppendLine();
        builder.Append("Timing: SpawnSpan=")
            .Append(FormatSeconds(spawnSpan))
            .Append(", SpawningCompletedAt=")
            .Append(hasObservedSpawningCompletion
                ? FormatSeconds(spawningCompletionOffset)
                : "NotCompleted")
            .Append(", BattleDuration=")
            .Append(FormatSeconds(battleDuration))
            .AppendLine();
        builder.Append("Player: InitialHealth=")
            .Append(initialPlayerHealth)
            .Append(", FinalHealth=")
            .Append(playerSystem != null ? playerSystem.CurrentHealth : 0)
            .Append('/')
            .Append(playerSystem != null ? playerSystem.MaxHealth : 0)
            .AppendLine();
        builder.Append("Integrity: ResolutionCountsMatch=")
            .Append(resolutionCountsMatch)
            .Append(", LeakCountMatchesPlayerHealthLoss=")
            .Append(leakCountMatchesPlayerHealth)
            .Append(", MonsterRuntimeCountsMatch=")
            .Append(monsterRuntimeCountsMatch)
            .Append(", MonsterRuntimeDamageMatches=")
            .Append(monsterRuntimeDamageMatches)
            .Append(", MonsterRuntimeRegistrationCoverageMatch=")
            .Append(monsterRuntimeRegistrationCoverageMatch)
            .Append(", MonsterRuntimeStartedAtFullHealth=")
            .Append(monsterRuntimeStartedAtFullHealth)
            .AppendLine();

        AppendMonsterRuntimeSummary(builder, monsterRuntime);
        AppendTowerSnapshot(builder);
        buffAccumulator.AppendSummary(builder);
        return builder.ToString().TrimEnd();
    }

    private string TryWriteJsonReport(
        string terminalState,
        string failureReason,
        DateTime generatedAt)
    {
        try
        {
            CombatBalanceRunJsonReport report = CreateJsonReport(
                terminalState,
                failureReason,
                generatedAt);
            string outputDirectory = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Doc",
                "GamePlayRecord"));
            Directory.CreateDirectory(outputDirectory);

            string outputPath = GetReportPath(
                outputDirectory,
                report.runLabel);
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(
                outputPath,
                json,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return outputPath;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Combat balance run recorder could not write its JSON report: " +
                exception.Message,
                this);
            return null;
        }
    }

    private CombatBalanceRunJsonReport CreateJsonReport(
        string terminalState,
        string failureReason,
        DateTime generatedAt)
    {
        int unresolvedCount = Mathf.Max(0, spawnedCount - resolvedCount);
        int notSpawnedCount = hasExpectedMonsterCount
            ? Mathf.Max(0, expectedMonsterCount - spawnedCount)
            : 0;
        float killRate = spawnedCount > 0
            ? (float)killedCount / spawnedCount
            : 0f;
        float averageObservedSpawnInterval = observedSpawnIntervalCount > 0
            ? observedSpawnIntervalTotal / observedSpawnIntervalCount
            : 0f;
        float damageCoverage = observedTotalMonsterMaxHealth > 0
            ? (float)effectiveDamage / observedTotalMonsterMaxHealth
            : 0f;
        float averageLeakedRemainingHealth = leakedCount > 0
            ? (float)leakedRemainingHealth / leakedCount
            : 0f;
        float battleDuration = hasObservedFirstSpawn
            ? Mathf.Max(
                0f,
                (lastResolutionTime > 0f ? lastResolutionTime : Time.time) -
                firstSpawnTime)
            : 0f;
        float spawnSpan = hasObservedFirstSpawn
            ? Mathf.Max(0f, lastSpawnTime - firstSpawnTime)
            : 0f;
        float spawningCompletionOffset =
            hasObservedFirstSpawn && hasObservedSpawningCompletion
                ? Mathf.Max(0f, spawningCompletedTime - firstSpawnTime)
                : 0f;
        int observedPlayerHealthLoss = playerSystem != null
            ? Mathf.Max(0, initialPlayerHealth - playerSystem.CurrentHealth)
            : 0;

        CombatBalanceRunJsonReport report = new CombatBalanceRunJsonReport
        {
            generatedAtLocal = generatedAt.ToString(
                "yyyy-MM-dd'T'HH:mm:sszzz",
                CultureInfo.InvariantCulture),
            runLabel = ResolveRunName(generatedAt),
            terminalState = terminalState,
            failureReason = failureReason ?? string.Empty
        };

        report.fixture = fixtureSnapshot ?? new CombatBalanceFixtureJson();
        report.fixture.waveConfigName = string.IsNullOrWhiteSpace(
            report.fixture.waveConfigName)
                ? waveConfigName ?? string.Empty
                : report.fixture.waveConfigName;
        report.fixture.expectedWaveCountAvailable = hasExpectedWaveCount;
        report.fixture.expectedWaveCount = expectedWaveCount;
        report.fixture.expectedMonsterCountAvailable =
            hasExpectedMonsterCount;
        report.fixture.expectedMonsterCount = expectedMonsterCount;
        report.fixture.placementRouteForcedRelocationExpectation =
            placementRouteForcedRelocationExpectation.ToString();
        report.fixture.observedMinimumMonsterHealth =
            observedMinimumMonsterHealth == int.MaxValue
                ? 0
                : observedMinimumMonsterHealth;
        report.fixture.observedMaximumMonsterHealth = observedMaximumMonsterHealth;
        report.fixture.observedMinimumMonsterSpeed =
            float.IsPositiveInfinity(observedMinimumMonsterSpeed)
                ? 0f
                : observedMinimumMonsterSpeed;
        report.fixture.observedMaximumMonsterSpeed = observedMaximumMonsterSpeed;
        report.fixture.observedAverageSpawnIntervalSeconds =
            averageObservedSpawnInterval;

        report.combat.spawned = spawnedCount;
        report.combat.resolved = resolvedCount;
        report.combat.killed = killedCount;
        report.combat.leaked = leakedCount;
        report.combat.unresolved = unresolvedCount;
        report.combat.notSpawned = notSpawnedCount;
        report.combat.killRate = killRate;
        report.combat.effectiveDamage = effectiveDamage;
        report.combat.totalObservedHealth = observedTotalMonsterMaxHealth;
        report.combat.damageCoverage = damageCoverage;
        report.combat.leakedRemainingHealth = leakedRemainingHealth;
        report.combat.averageLeakedRemainingHealth = averageLeakedRemainingHealth;
        report.combat.peakAlive = peakAliveCount;

        report.timing.runDurationSeconds = GetRunActiveTimeSeconds();
        report.timing.spawnSpanSeconds = spawnSpan;
        report.timing.spawningCompleted = hasObservedSpawningCompletion;
        report.timing.spawningCompletedAtSeconds = spawningCompletionOffset;
        report.timing.battleDurationSeconds = battleDuration;

        report.player.initialHealth = initialPlayerHealth;
        report.player.finalHealth = playerSystem != null
            ? playerSystem.CurrentHealth
            : 0;
        report.player.maximumHealth = playerSystem != null
            ? playerSystem.MaxHealth
            : 0;

        report.progression = CreateProgressionJson();
        report.timing.secondsAfterFinalDraft =
            ResolveSecondsAfterFinalDraft(
                report.progression,
                report.timing.runDurationSeconds);
        report.waveRuntime = CreateWaveRuntimeJson();
        report.draftRuntime = CreateDraftRuntimeJson();
        report.investmentRuntime = CreateInvestmentRuntimeJson();
        report.timing.finalBuildCommitObserved =
            report.investmentRuntime.finalBuildCommitObserved;
        report.timing.finalBuildCommittedAtSeconds =
            ResolveFinalBuildCommitTime();
        report.timing.secondsAfterFinalBuildCommit =
            report.timing.finalBuildCommitObserved
                ? Mathf.Max(
                    0f,
                    report.timing.runDurationSeconds -
                    report.timing.finalBuildCommittedAtSeconds)
                : 0f;

        report.monsterRuntime = CreateMonsterRuntimeJson();
        report.placementRouteRuntime = CreatePlacementRouteRuntimeJson();
        report.damageDiagnostics = CreateDamageDiagnosticsJson();
        report.towers = CreateTowerJsonRecords();
        report.towerWaveSummaries = CreateTowerWaveSummaries();
        report.towerRouteDamageCoverage =
            CreateTowerRouteDamageCoverageRuntimeJson(report.towers);
        report.execution.initialDraftCompleted =
            initialDraftCompletionCount > 0;
        report.execution.allExpectedLevelUpsObserved =
            report.progression.observedLevelUpCount ==
            report.progression.expectedLevelUpCount;
        report.execution.expectedFinalPlayerLevelReached =
            report.progression.finalLevel ==
            report.progression.expectedLevelUpCount + 1;
        report.execution.allConfiguredWavesStarted =
            !hasExpectedWaveCount ||
            report.waveRuntime.observedWaveStartCount == expectedWaveCount;
        report.execution.allConfiguredWavesCompleted =
            !hasExpectedWaveCount ||
            report.waveRuntime.observedWaveCompletionCount == expectedWaveCount;
        report.execution.allConfiguredMonstersSpawned =
            !hasExpectedMonsterCount || spawnedCount == expectedMonsterCount;
        report.execution.finalBuildCommitted =
            report.investmentRuntime.committedInvestmentCount ==
            report.progression.expectedTotalDraftCount;
        report.execution.postFinalBuildCombatObserved =
            report.investmentRuntime.resolutionsAfterFinalBuildCommit > 0;
        report.integrity.fixtureSnapshotComplete =
            FixtureSnapshotIsComplete(report.fixture);
        report.integrity.resolutionCountsMatch =
            resolvedCount == killedCount + leakedCount;
        report.integrity.leakCountMatchesPlayerHealthLoss =
            playerSystem != null && leakedCount == observedPlayerHealthLoss;
        report.integrity.monsterRuntimeCountsMatch =
            MonsterRuntimeCountsMatch(report.monsterRuntime);
        report.integrity.monsterRuntimeDamageMatches =
            MonsterRuntimeDamageMatches(report.monsterRuntime);
        report.integrity.monsterRuntimeRegistrationCoverageMatch =
            MonsterRuntimeRegistrationCoverageMatches(report.monsterRuntime);
        report.integrity.monsterRuntimeStartedAtFullHealth =
            MonsterRuntimeStartedAtFullHealth(report.monsterRuntime);
        report.integrity.levelUpResolutionNodesMatch =
            LevelUpResolutionNodesMatch(report.progression);
        report.integrity.waveMonsterAttributionMatches =
            WaveMonsterAttributionMatches(report.waveRuntime);
        report.integrity.draftAttemptSelectionsMatch =
            report.draftRuntime.observedAttemptCount ==
            report.draftRuntime.committedSelectionCount;
        report.integrity.draftAttemptCountMatchesProgression =
            report.draftRuntime.observedAttemptCount ==
            report.progression.observedTotalDraftCount;
        report.integrity.towerDeploymentCoverageMatches =
            TowerDeploymentCoverageMatches(report.towers);
        report.integrity.investmentCommitsMatchDraftSelections =
            InvestmentCommitsMatchDraftSelections(
                report.investmentRuntime,
                report.draftRuntime);
        report.integrity.towerWaveAttributionMatches =
            TowerWaveAttributionMatches(
                report.towerWaveSummaries,
                report.damageDiagnostics);
        report.integrity.towerRouteDamageCoverageMatches =
            TowerRouteDamageCoverageMatches(
                report.towerRouteDamageCoverage,
                report.towers,
                report.towerWaveSummaries);
        report.integrity.damageDiagnosticsCountsMatch =
            DamageDiagnosticsCountsMatch(report.damageDiagnostics);
        report.integrity.droneBurstDiagnosticsConsistent =
            DroneBurstDiagnosticsAreConsistent();
        report.integrity.droneLifecycleDiagnosticsConsistent =
            DroneLifecycleDiagnosticsAreConsistent();
        report.elementalOpportunityDiagnostics =
            CreateElementalOpportunityDiagnosticsJson();
        report.integrity.elementalOpportunityDiagnosticsConsistent =
            report.elementalOpportunityDiagnostics.diagnosticsConsistent;
        report.elementalHitReactionDiagnostics =
            elementalHitReactionAccumulator.CreateJson();
        report.integrity.elementalHitReactionDiagnosticsConsistent =
            report.elementalHitReactionDiagnostics.diagnosticsConsistent &&
            ElementalHitReactionDamageReconciliationMatches();
        report.buffs = buffAccumulator.CreateJsonRecords();
        report.integrity.buffDiagnosticsConsistent =
            buffAccumulator.DiagnosticsConsistent;
        report.integrity.buffStackUnitAccountingConsistent =
            buffAccumulator.StackUnitAccountingConsistent;
        report.integrity.buffWaveAttributionMatches =
            buffAccumulator.WaveAttributionMatches;
        report.integrity.placementRouteBatchCountsMatch =
            PlacementRouteBatchCountsMatch(report.placementRouteRuntime);
        report.integrity.placementRouteCommitStatePreserved =
            PlacementRouteCommitStatePreserved(
                report.placementRouteRuntime);
        report.integrity.placementRouteTopologyValid =
            PlacementRouteTopologyValid(report.placementRouteRuntime);
        report.integrity.placementRouteGameplayStatePreserved =
            PlacementRouteGameplayStatePreserved(
                report.placementRouteRuntime);
        report.integrity.placementRouteCombatOwnershipPreserved =
            PlacementRouteCombatOwnershipPreserved(
                report.placementRouteRuntime);
        report.integrity.placementRouteLifecycleConsistent =
            PlacementRouteLifecycleConsistent(
                report.placementRouteRuntime);
        report.integrity.placementRouteForcedRelocationUsageValid =
            PlacementRouteForcedRelocationUsageValid(
                report.placementRouteRuntime);
        return report;
    }

    private CombatBalancePlacementRouteRuntimeJson
        CreatePlacementRouteRuntimeJson()
    {
        CombatBalancePlacementRouteRuntimeJson runtime =
            new CombatBalancePlacementRouteRuntimeJson
            {
                forcedRelocationExpectation =
                    placementRouteForcedRelocationExpectation.ToString(),
                committedPlacementCount = placementRouteCommits.Count
            };
        HashSet<long> started = new HashSet<long>();
        HashSet<long> terminated = new HashSet<long>();

        for (int i = 0; i < placementRouteCommits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit =
                placementRouteCommits[i];
            runtime.commits.Add(commit);
            runtime.observedMonsterRevisionCount += commit.monsters.Count;

            for (int monsterIndex = 0;
                 monsterIndex < commit.monsters.Count;
                 monsterIndex++)
            {
                switch (commit.monsters[monsterIndex].mode)
                {
                    case nameof(
                        MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute):
                        runtime.alreadyOnNewRouteCount++;
                        break;
                    case nameof(
                        MonsterPlacementRouteRevisionMode.ReachableRouteRejoin):
                        runtime.reachableRouteRejoinCount++;
                        break;
                    case nameof(
                        MonsterPlacementRouteRevisionMode.ForcedRelocation):
                        runtime.forcedRelocationCount++;
                        break;
                }
            }
        }

        for (int i = 0; i < placementRouteLifecycle.Count; i++)
        {
            CombatBalancePlacementRouteLifecycleJson observation =
                placementRouteLifecycle[i];
            runtime.lifecycle.Add(observation);

            if (observation.kind ==
                nameof(MonsterPlacementRouteLifecycleKind.Started))
            {
                started.Add(observation.revisionId);
            }
            else if (IsPlacementConnectorTerminal(observation.kind))
            {
                terminated.Add(observation.revisionId);
            }
        }

        foreach (long revisionId in started)
        {
            if (terminated.Contains(revisionId))
            {
                continue;
            }

            CombatBalancePlacementRouteMonsterJson revision =
                FindPlacementRouteMonster(runtime, revisionId);
            runtime.lifecycle.Add(
                new CombatBalancePlacementRouteLifecycleJson
                {
                    revisionId = revisionId,
                    monsterInstanceId = revision != null
                        ? revision.monsterInstanceId
                        : 0,
                    spawnOrdinal = revision != null
                        ? revision.spawnOrdinal
                        : 0,
                    kind = nameof(
                        MonsterPlacementRouteLifecycleKind.ActiveAtRunEnd),
                    resolutionReason = nameof(
                        MonsterPlacementRouteResolutionReason.None),
                    activeTimeSeconds = GetRunActiveTimeSeconds()
                });
        }

        runtime.lifecycleObservationCount = runtime.lifecycle.Count;
        return runtime;
    }

    private CombatBalanceDamageDiagnosticsJson CreateDamageDiagnosticsJson()
    {
        CombatBalanceDamageDiagnosticsJson diagnostics =
            new CombatBalanceDamageDiagnosticsJson();
        List<string> towerScaledKeys =
            new List<string>(towerScaledDamageBySignature.Keys);
        towerScaledKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < towerScaledKeys.Count; i++)
        {
            TowerScaledDamageAggregate aggregate =
                towerScaledDamageBySignature[towerScaledKeys[i]];
            TowerOwnedDamageResolution resolution = aggregate.Resolution;
            TowerDamageSourceIdentity sourceIdentity =
                resolution.DamageSourceIdentity;
            TowerInstance sourceTower = resolution.SourceTower;
            TowerDefinition towerDefinition = sourceTower != null
                ? sourceTower.TowerDefinition
                : null;
            float relativeRoundingError = resolution.RawProduct > 0f
                ? Mathf.Abs(resolution.FinalDamage - resolution.RawProduct) /
                  resolution.RawProduct
                : 0f;

            diagnostics.towerScaledSignatures.Add(
                new CombatBalanceTowerScaledDamageJson
                {
                    towerInstanceId = sourceTower != null
                        ? sourceTower.GetInstanceID()
                        : 0,
                    towerDisplayName = towerDefinition != null
                        ? GetDisplayName(
                            towerDefinition.DisplayName,
                            towerDefinition.name)
                        : string.Empty,
                    towerFamily = resolution.TowerFamily.ToString(),
                    level = resolution.Level,
                    levelBasicDamage = resolution.LevelBasicDamage,
                    rawDamageBonus = resolution.RawDamageBonus,
                    resolvedBasicDamage = resolution.ResolvedBasicDamage,
                    damageSourceType = sourceIdentity.SourceType.ToString(),
                    effectDefinitionName =
                        GetAssetName(sourceIdentity.EffectDefinition),
                    actionOrdinal = sourceIdentity.ActionOrdinal,
                    damageScale = resolution.DamageScale,
                    rawProduct = resolution.RawProduct,
                    finalDamage = resolution.FinalDamage,
                    relativeRoundingError = relativeRoundingError,
                    resolutionCount = aggregate.ResolutionCount,
                    successfulApplicationCount =
                        aggregate.SuccessfulApplicationCount,
                    appliedDamageTotal = aggregate.AppliedDamageTotal
                });
            diagnostics.towerScaledResolutionCount +=
                aggregate.ResolutionCount;
            diagnostics.towerScaledSuccessfulApplicationCount +=
                aggregate.SuccessfulApplicationCount;
            diagnostics.towerScaledAppliedDamageTotal +=
                aggregate.AppliedDamageTotal;
            diagnostics.maximumTowerScaledRelativeRoundingError =
                Mathf.Max(
                    diagnostics.maximumTowerScaledRelativeRoundingError,
                    relativeRoundingError);
        }

        List<string> rejectionKeys =
            new List<string>(towerScaledRejectionBySignature.Keys);
        rejectionKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < rejectionKeys.Count; i++)
        {
            TowerScaledRejectionAggregate aggregate =
                towerScaledRejectionBySignature[rejectionKeys[i]];
            TowerOwnedDamageResolutionObservation observation =
                aggregate.Observation;
            TowerDamageSourceIdentity sourceIdentity =
                observation.DamageSourceIdentity;

            diagnostics.towerScaledRejections.Add(
                new CombatBalanceTowerScaledRejectionJson
                {
                    sourceTowerInstanceId = observation.SourceTower != null
                        ? observation.SourceTower.GetInstanceID()
                        : 0,
                    sourceTowerName = observation.SourceTower != null
                        ? observation.SourceTower.name
                        : string.Empty,
                    damageSourceType = sourceIdentity.SourceType.ToString(),
                    effectDefinitionName =
                        GetAssetName(sourceIdentity.EffectDefinition),
                    actionOrdinal = sourceIdentity.ActionOrdinal,
                    damageScale = observation.DamageScale,
                    failureReason = observation.FailureReason.ToString(),
                    rejectionCount = aggregate.RejectionCount
                });
            diagnostics.towerScaledRejectedCount +=
                aggregate.RejectionCount;
        }

        List<string> fixedBuffKeys =
            new List<string>(fixedBuffDamageBySignature.Keys);
        fixedBuffKeys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < fixedBuffKeys.Count; i++)
        {
            FixedBuffDamageAggregate aggregate =
                fixedBuffDamageBySignature[fixedBuffKeys[i]];

            diagnostics.fixedBuffSignatures.Add(
                new CombatBalanceFixedBuffDamageJson
                {
                    effectDefinitionName =
                        GetAssetName(aggregate.EffectDefinition),
                    actionOrdinal = aggregate.ActionOrdinal,
                    fixedDamage = aggregate.FixedDamage,
                    sourceTowerInstanceCount =
                        aggregate.SourceTowerInstanceIds.Count,
                    resolvedTargetCount = aggregate.ResolvedTargetCount,
                    resolutionCount = aggregate.ResolutionCount,
                    successfulApplicationCount =
                        aggregate.SuccessfulApplicationCount,
                    appliedDamageTotal = aggregate.AppliedDamageTotal
                });
            diagnostics.fixedBuffResolutionCount +=
                aggregate.ResolutionCount;
            diagnostics.fixedBuffSuccessfulApplicationCount +=
                aggregate.SuccessfulApplicationCount;
            diagnostics.fixedBuffAppliedDamageTotal +=
                aggregate.AppliedDamageTotal;
        }

        return diagnostics;
    }

    private static bool PlacementRouteBatchCountsMatch(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        int revisions = 0;

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit = runtime.commits[i];

            if (commit == null ||
                commit.monsters.Count != commit.aliveMonsterCountBefore ||
                commit.aliveMonsterCountBefore !=
                    commit.aliveMonsterCountAfter ||
                commit.resolvedMonsterCountBefore !=
                    commit.resolvedMonsterCountAfter)
            {
                return false;
            }

            int alreadyOnRoute = 0;
            int reachableRejoin = 0;
            int forcedRelocation = 0;

            for (int monsterIndex = 0;
                 monsterIndex < commit.monsters.Count;
                 monsterIndex++)
            {
                switch (commit.monsters[monsterIndex].mode)
                {
                    case nameof(
                        MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute):
                        alreadyOnRoute++;
                        break;
                    case nameof(
                        MonsterPlacementRouteRevisionMode.ReachableRouteRejoin):
                        reachableRejoin++;
                        break;
                    case nameof(
                        MonsterPlacementRouteRevisionMode.ForcedRelocation):
                        forcedRelocation++;
                        break;
                }
            }

            if (alreadyOnRoute != commit.alreadyOnNewRouteCount ||
                reachableRejoin != commit.reachableRouteRejoinCount ||
                forcedRelocation != commit.forcedRelocationCount)
            {
                return false;
            }

            revisions += commit.monsters.Count;
        }

        return revisions == runtime.observedMonsterRevisionCount &&
               revisions ==
                   runtime.alreadyOnNewRouteCount +
                   runtime.reachableRouteRejoinCount +
                   runtime.forcedRelocationCount;
    }

    private static bool PlacementRouteCommitStatePreserved(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        const float epsilon = 0.0001f;

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit = runtime.commits[i];

            if (commit.playerHealthBefore != commit.playerHealthAfter ||
                commit.playerProgressBefore != commit.playerProgressAfter)
            {
                return false;
            }

            for (int monsterIndex = 0;
                 monsterIndex < commit.monsters.Count;
                 monsterIndex++)
            {
                CombatBalancePlacementRouteMonsterJson monster =
                    commit.monsters[monsterIndex];
                bool forced = monster.mode == nameof(
                    MonsterPlacementRouteRevisionMode.ForcedRelocation);
                CombatBalanceVector3Json expected = forced
                    ? monster.preparedWorldPosition
                    : monster.capturedWorldPosition;

                if ((!forced && !monster.hasComparableCapturedPosition) ||
                    !PositionsApproximatelyEqual(
                        expected,
                        monster.immediatePostCommitPosition,
                        epsilon))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool PlacementRouteGameplayStatePreserved(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            List<CombatBalancePlacementRouteMonsterJson> monsters =
                runtime.commits[i].monsters;

            for (int monsterIndex = 0;
                 monsterIndex < monsters.Count;
                 monsterIndex++)
            {
                if (!string.Equals(
                        monsters[monsterIndex].preGameplayStateFingerprint,
                        monsters[monsterIndex].postGameplayStateFingerprint,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool PlacementRouteTopologyValid(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit = runtime.commits[i];

            if (commit.authoritativeRoute.Count < 2 ||
                !IsOrthogonalGridPath(commit.authoritativeRoute))
            {
                return false;
            }

            for (int routeIndex = 0;
                 routeIndex < commit.authoritativeRoute.Count;
                 routeIndex++)
            {
                if (ContainsGridPosition(
                        commit.footprint,
                        commit.authoritativeRoute[routeIndex]))
                {
                    return false;
                }
            }

            CombatBalanceGridPositionJson target =
                commit.authoritativeRoute[
                    commit.authoritativeRoute.Count - 1];

            for (int monsterIndex = 0;
                 monsterIndex < commit.monsters.Count;
                 monsterIndex++)
            {
                CombatBalancePlacementRouteMonsterJson monster =
                    commit.monsters[monsterIndex];

                if (monster.requiresExactTargetApproach)
                {
                    if (monster.preparedContinuation.Count != 0 ||
                        monster.joinGrid == null ||
                        !GridPositionsEqual(monster.joinGrid, target))
                    {
                        return false;
                    }

                    continue;
                }

                if (monster.preparedContinuation.Count == 0 ||
                    !GridPositionsEqual(
                        monster.preparedContinuation[
                            monster.preparedContinuation.Count - 1],
                        target) ||
                    !IsOrthogonalGridPath(monster.preparedContinuation))
                {
                    return false;
                }

                if (monster.routeSuffix.Count == 0 ||
                    monster.joinGrid == null ||
                    !GridPositionsEqual(
                        monster.routeSuffix[0],
                        monster.joinGrid) ||
                    !GridPositionsEqual(
                        monster.routeSuffix[
                            monster.routeSuffix.Count - 1],
                        target) ||
                    !IsOrthogonalGridPath(monster.routeSuffix))
                {
                    return false;
                }

                if (monster.connectorPath.Count > 0)
                {
                    if (!IsOrthogonalGridPath(monster.connectorPath) ||
                        monster.joinGrid == null ||
                        !GridPositionsEqual(
                            monster.connectorPath[
                                monster.connectorPath.Count - 1],
                            monster.joinGrid))
                    {
                        return false;
                    }

                    for (int connectorIndex = 0;
                         connectorIndex < monster.connectorPath.Count;
                         connectorIndex++)
                    {
                        if (ContainsGridPosition(
                                commit.footprint,
                                monster.connectorPath[connectorIndex]))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private static bool PlacementRouteCombatOwnershipPreserved(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            CombatBalancePlacementRouteCommitJson commit = runtime.commits[i];

            if (!string.Equals(
                    commit.combatOwnershipFingerprintBefore,
                    commit.combatOwnershipFingerprintAfter,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool PlacementRouteLifecycleConsistent(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        Dictionary<int, long> activeRevisionByMonster =
            new Dictionary<int, long>();

        for (int i = 0; i < runtime.lifecycle.Count; i++)
        {
            CombatBalancePlacementRouteLifecycleJson lifecycle =
                runtime.lifecycle[i];

            if (lifecycle.kind == nameof(
                    MonsterPlacementRouteLifecycleKind.Started))
            {
                if (activeRevisionByMonster.ContainsKey(
                        lifecycle.monsterInstanceId))
                {
                    return false;
                }

                activeRevisionByMonster[lifecycle.monsterInstanceId] =
                    lifecycle.revisionId;
            }
            else if (IsPlacementConnectorTerminal(lifecycle.kind) &&
                     lifecycle.kind != nameof(
                         MonsterPlacementRouteLifecycleKind.ActiveAtRunEnd))
            {
                if (activeRevisionByMonster.TryGetValue(
                        lifecycle.monsterInstanceId,
                        out long activeRevision) &&
                    activeRevision == lifecycle.revisionId)
                {
                    activeRevisionByMonster.Remove(
                        lifecycle.monsterInstanceId);
                }
            }
        }

        for (int i = 0; i < runtime.commits.Count; i++)
        {
            List<CombatBalancePlacementRouteMonsterJson> monsters =
                runtime.commits[i].monsters;

            for (int monsterIndex = 0;
                 monsterIndex < monsters.Count;
                 monsterIndex++)
            {
                CombatBalancePlacementRouteMonsterJson monster =
                    monsters[monsterIndex];
                int startedCount = 0;
                int terminalCount = 0;
                int relocationCount = 0;

                for (int lifecycleIndex = 0;
                     lifecycleIndex < runtime.lifecycle.Count;
                     lifecycleIndex++)
                {
                    CombatBalancePlacementRouteLifecycleJson lifecycle =
                        runtime.lifecycle[lifecycleIndex];

                    if (lifecycle.revisionId != monster.revisionId)
                    {
                        continue;
                    }

                    if (lifecycle.kind == nameof(
                        MonsterPlacementRouteLifecycleKind.Started))
                    {
                        startedCount++;
                    }
                    else if (lifecycle.kind == nameof(
                        MonsterPlacementRouteLifecycleKind.RelocationApplied))
                    {
                        relocationCount++;
                    }
                    else if (IsPlacementConnectorTerminal(lifecycle.kind))
                    {
                        terminalCount++;
                    }
                }

                if (monster.requiresConnector)
                {
                    if (startedCount != 1 || terminalCount != 1)
                    {
                        return false;
                    }
                }
                else if (monster.joinedAtCommit)
                {
                    if (startedCount != 0 || terminalCount != 1)
                    {
                        return false;
                    }
                }
                else if (startedCount != 0 || terminalCount != 0)
                {
                    return false;
                }

                bool forced = monster.mode == nameof(
                    MonsterPlacementRouteRevisionMode.ForcedRelocation);

                if ((forced && relocationCount != 1) ||
                    (!forced && relocationCount != 0))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool PlacementRouteForcedRelocationUsageValid(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        if (runtime == null ||
            !Enum.TryParse(
                runtime.forcedRelocationExpectation,
                out PlacementRouteForcedRelocationExpectation expectation))
        {
            return false;
        }

        switch (expectation)
        {
            case PlacementRouteForcedRelocationExpectation.RequireZero:
                return runtime.forcedRelocationCount == 0;
            case PlacementRouteForcedRelocationExpectation.RequireDiagnosed:
                return runtime.forcedRelocationCount > 0 &&
                       AllForcedRelocationsDiagnosed(runtime);
            case PlacementRouteForcedRelocationExpectation.Ignore:
                return AllForcedRelocationsDiagnosed(runtime);
            default:
                return false;
        }
    }

    private static bool AllForcedRelocationsDiagnosed(
        CombatBalancePlacementRouteRuntimeJson runtime)
    {
        for (int i = 0; i < runtime.commits.Count; i++)
        {
            List<CombatBalancePlacementRouteMonsterJson> monsters =
                runtime.commits[i].monsters;

            for (int monsterIndex = 0;
                 monsterIndex < monsters.Count;
                 monsterIndex++)
            {
                CombatBalancePlacementRouteMonsterJson monster =
                    monsters[monsterIndex];

                if (monster.mode == nameof(
                        MonsterPlacementRouteRevisionMode.ForcedRelocation) &&
                    (monster.forcedRelocationReason == nameof(
                         MonsterForcedRelocationReason.None) ||
                     monster.recoveryGrid == null))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static CombatBalancePlacementRouteMonsterJson
        FindPlacementRouteMonster(
            CombatBalancePlacementRouteRuntimeJson runtime,
            long revisionId)
    {
        for (int i = 0; i < runtime.commits.Count; i++)
        {
            List<CombatBalancePlacementRouteMonsterJson> monsters =
                runtime.commits[i].monsters;

            for (int monsterIndex = 0;
                 monsterIndex < monsters.Count;
                 monsterIndex++)
            {
                if (monsters[monsterIndex].revisionId == revisionId)
                {
                    return monsters[monsterIndex];
                }
            }
        }

        return null;
    }

    private static bool IsPlacementConnectorTerminal(string kind)
    {
        return kind == nameof(MonsterPlacementRouteLifecycleKind.Joined) ||
               kind == nameof(
                   MonsterPlacementRouteLifecycleKind.Superseded) ||
               kind == nameof(
                   MonsterPlacementRouteLifecycleKind.MonsterResolvedBeforeJoin) ||
               kind == nameof(
                   MonsterPlacementRouteLifecycleKind.ActiveAtRunEnd);
    }

    private static bool PositionsApproximatelyEqual(
        CombatBalanceVector3Json left,
        CombatBalanceVector3Json right,
        float epsilon)
    {
        return left != null &&
               right != null &&
               Mathf.Abs(left.x - right.x) <= epsilon &&
               Mathf.Abs(left.y - right.y) <= epsilon &&
               Mathf.Abs(left.z - right.z) <= epsilon;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) &&
               !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) &&
               !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) &&
               !float.IsInfinity(value.z);
    }

    private static bool IsOrthogonalGridPath(
        IReadOnlyList<CombatBalanceGridPositionJson> path)
    {
        if (path == null || path.Count == 0)
        {
            return false;
        }

        for (int i = 1; i < path.Count; i++)
        {
            if (path[i - 1] == null ||
                path[i] == null ||
                Mathf.Abs(path[i].x - path[i - 1].x) +
                Mathf.Abs(path[i].z - path[i - 1].z) != 1)
            {
                return false;
            }
        }

        return path[0] != null;
    }

    private static bool ContainsGridPosition(
        IReadOnlyList<CombatBalanceGridPositionJson> positions,
        CombatBalanceGridPositionJson candidate)
    {
        if (positions == null || candidate == null)
        {
            return false;
        }

        for (int i = 0; i < positions.Count; i++)
        {
            if (GridPositionsEqual(positions[i], candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool GridPositionsEqual(
        CombatBalanceGridPositionJson left,
        CombatBalanceGridPositionJson right)
    {
        return left != null &&
               right != null &&
               left.x == right.x &&
               left.z == right.z;
    }

    private static bool DamageDiagnosticsCountsMatch(
        CombatBalanceDamageDiagnosticsJson diagnostics)
    {
        if (diagnostics == null)
        {
            return false;
        }

        int towerResolutionCount = 0;
        int towerApplicationCount = 0;
        int towerAppliedDamage = 0;
        int towerRejectionCount = 0;
        int fixedResolutionCount = 0;
        int fixedApplicationCount = 0;
        int fixedAppliedDamage = 0;

        for (int i = 0; i < diagnostics.towerScaledSignatures.Count; i++)
        {
            CombatBalanceTowerScaledDamageJson record =
                diagnostics.towerScaledSignatures[i];
            towerResolutionCount += record.resolutionCount;
            towerApplicationCount += record.successfulApplicationCount;
            towerAppliedDamage += record.appliedDamageTotal;
        }

        for (int i = 0; i < diagnostics.towerScaledRejections.Count; i++)
        {
            towerRejectionCount +=
                diagnostics.towerScaledRejections[i].rejectionCount;
        }

        for (int i = 0; i < diagnostics.fixedBuffSignatures.Count; i++)
        {
            CombatBalanceFixedBuffDamageJson record =
                diagnostics.fixedBuffSignatures[i];
            fixedResolutionCount += record.resolutionCount;
            fixedApplicationCount += record.successfulApplicationCount;
            fixedAppliedDamage += record.appliedDamageTotal;
        }

        return towerResolutionCount == diagnostics.towerScaledResolutionCount &&
               towerApplicationCount ==
               diagnostics.towerScaledSuccessfulApplicationCount &&
               towerAppliedDamage == diagnostics.towerScaledAppliedDamageTotal &&
               towerRejectionCount == diagnostics.towerScaledRejectedCount &&
               fixedResolutionCount == diagnostics.fixedBuffResolutionCount &&
               fixedApplicationCount ==
               diagnostics.fixedBuffSuccessfulApplicationCount &&
               fixedAppliedDamage == diagnostics.fixedBuffAppliedDamageTotal;
    }

    private bool ElementalHitReactionDamageReconciliationMatches()
    {
        List<ElementalHitReactionDamageExpectation> expectations =
            elementalHitReactionAccumulator.CreateDamageExpectations();

        for (int i = 0; i < expectations.Count; i++)
        {
            ElementalHitReactionDamageExpectation expectation =
                expectations[i];
            string signature = CreateFixedBuffDamageSignature(
                expectation.EffectDefinition,
                expectation.ActionOrdinal,
                expectation.FixedDamage);

            if (!fixedBuffDamageBySignature.TryGetValue(
                    signature,
                    out FixedBuffDamageAggregate aggregate) ||
                aggregate.ResolutionCount != expectation.ResolutionCount ||
                aggregate.SuccessfulApplicationCount !=
                    expectation.SuccessfulApplicationCount ||
                aggregate.AppliedDamageTotal != expectation.AppliedDamageTotal)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetAssetName(UnityEngine.Object asset)
    {
        return asset != null ? asset.name : string.Empty;
    }

    private CombatBalanceDraftRuntimeJson CreateDraftRuntimeJson()
    {
        CombatBalanceDraftRuntimeJson runtime =
            new CombatBalanceDraftRuntimeJson
            {
                configuredGenerationMode =
                    configuredDraftGenerationMode ?? string.Empty,
                configuredFixedStepCount = configuredFixedDraftStepCount,
                towerDraftPool =
                    new List<CombatBalanceDraftItemJson>(
                        towerDraftPoolSnapshot),
                towerUpgradeDraftPool =
                    new List<CombatBalanceDraftItemJson>(
                        towerUpgradeDraftPoolSnapshot),
                attempts = new List<CombatBalanceDraftAttemptJson>(
                    draftAttempts)
            };
        runtime.observedAttemptCount = runtime.attempts.Count;

        for (int i = 0; i < runtime.attempts.Count; i++)
        {
            if (runtime.attempts[i].selectionCommitted)
            {
                runtime.committedSelectionCount++;
            }
        }

        return runtime;
    }

    private CombatBalanceInvestmentRuntimeJson CreateInvestmentRuntimeJson()
    {
        CombatBalanceInvestmentRuntimeJson runtime =
            new CombatBalanceInvestmentRuntimeJson
            {
                commits = new List<CombatBalanceInvestmentCommitJson>(
                    investmentCommits)
            };
        runtime.committedInvestmentCount = runtime.commits.Count;

        if (runtime.commits.Count > 0)
        {
            CombatBalanceInvestmentCommitJson finalCommit =
                runtime.commits[runtime.commits.Count - 1];
            runtime.finalBuildCommitObserved = true;
            runtime.finalBuildCommitResolutionNode =
                finalCommit.resolvedMonsterCount;
            runtime.resolutionsAfterFinalBuildCommit = Mathf.Max(
                0,
                resolvedCount - finalCommit.resolvedMonsterCount);
        }

        return runtime;
    }

    private float ResolveFinalBuildCommitTime()
    {
        return investmentCommits.Count > 0
            ? investmentCommits[investmentCommits.Count - 1]
                .activeTimeSeconds
            : 0f;
    }

    private List<CombatBalanceTowerWaveJson> CreateTowerWaveSummaries()
    {
        List<TowerWaveDamageAggregate> aggregates =
            new List<TowerWaveDamageAggregate>(
                towerWaveDamageByScope.Values);
        aggregates.Sort((left, right) =>
        {
            int towerComparison = left.TowerInstanceId.CompareTo(
                right.TowerInstanceId);
            return towerComparison != 0
                ? towerComparison
                : left.WaveNumber.CompareTo(right.WaveNumber);
        });
        List<CombatBalanceTowerWaveJson> summaries =
            new List<CombatBalanceTowerWaveJson>(aggregates.Count);

        for (int i = 0; i < aggregates.Count; i++)
        {
            TowerWaveDamageAggregate aggregate = aggregates[i];
            summaries.Add(new CombatBalanceTowerWaveJson
            {
                towerInstanceId = aggregate.TowerInstanceId,
                towerDisplayName = aggregate.TowerDisplayName ?? string.Empty,
                towerFamily = aggregate.TowerFamily ?? string.Empty,
                waveNumber = aggregate.WaveNumber,
                successfulDamageApplications =
                    aggregate.SuccessfulDamageApplications,
                effectiveTowerScaledDamage =
                    aggregate.EffectiveTowerScaledDamage,
                killingBlows = aggregate.KillingBlows
            });
        }

        return summaries;
    }

    private CombatBalanceTowerRouteDamageCoverageRuntimeJson
        CreateTowerRouteDamageCoverageRuntimeJson(
            IReadOnlyList<CombatBalanceTowerJson> towers)
    {
        CombatBalanceTowerRouteDamageCoverageRuntimeJson runtime =
            new CombatBalanceTowerRouteDamageCoverageRuntimeJson();

        if (towers == null)
        {
            return runtime;
        }

        for (int i = 0; i < towers.Count; i++)
        {
            CombatBalanceTowerJson tower = towers[i];

            if (tower == null)
            {
                continue;
            }

            towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
                tower.instanceId,
                out TowerRouteDamageCoverageAggregate aggregate);
            CombatBalanceTowerRouteDamageCoverageJson coverage =
                CreateTowerRouteDamageCoverageJson(tower, aggregate);
            runtime.towers.Add(coverage);
            runtime.successfulDamageApplications +=
                coverage.successfulDamageApplications;
            runtime.locatedDamageApplications +=
                coverage.locatedDamageApplications;
            runtime.unresolvedRouteCellApplicationCount +=
                coverage.unresolvedRouteCellApplicationCount;
            runtime.effectiveDamage += coverage.effectiveDamage;
            runtime.locatedEffectiveDamage += coverage.locatedEffectiveDamage;
            runtime.unresolvedRouteCellEffectiveDamage +=
                coverage.unresolvedRouteCellEffectiveDamage;
            runtime.killingBlows += coverage.killingBlows;
            runtime.locatedKillingBlows += coverage.locatedKillingBlows;
            runtime.unresolvedRouteCellKillingBlows +=
                coverage.unresolvedRouteCellKillingBlows;
        }

        runtime.observedTowerCount = runtime.towers.Count;

        for (int leftIndex = 0;
             leftIndex < runtime.towers.Count;
             leftIndex++)
        {
            CombatBalanceTowerRouteDamageCoverageJson left =
                runtime.towers[leftIndex];

            for (int rightIndex = leftIndex + 1;
                 rightIndex < runtime.towers.Count;
                 rightIndex++)
            {
                CombatBalanceTowerRouteDamageCoverageJson right =
                    runtime.towers[rightIndex];
                runtime.overlaps.Add(
                    CreateTowerRouteDamageOverlapJson(left, right));
            }
        }

        return runtime;
    }

    private static CombatBalanceTowerRouteDamageCoverageJson
        CreateTowerRouteDamageCoverageJson(
            CombatBalanceTowerJson tower,
            TowerRouteDamageCoverageAggregate aggregate)
    {
        CombatBalanceTowerRouteDamageCoverageJson coverage =
            new CombatBalanceTowerRouteDamageCoverageJson
            {
                towerInstanceId = tower.instanceId,
                towerDeploymentOrdinal = tower.deploymentOrdinal,
                towerDisplayName = tower.displayName ?? string.Empty,
                towerFamily = tower.family ?? string.Empty
            };

        if (aggregate == null)
        {
            return coverage;
        }

        coverage.successfulDamageApplications =
            aggregate.SuccessfulDamageApplications;
        coverage.locatedDamageApplications =
            aggregate.LocatedDamageApplications;
        coverage.unresolvedRouteCellApplicationCount =
            aggregate.UnresolvedRouteCellApplicationCount;
        coverage.effectiveDamage = aggregate.EffectiveDamage;
        coverage.locatedEffectiveDamage = aggregate.LocatedEffectiveDamage;
        coverage.unresolvedRouteCellEffectiveDamage =
            aggregate.UnresolvedRouteCellEffectiveDamage;
        coverage.killingBlows = aggregate.KillingBlows;
        coverage.locatedKillingBlows = aggregate.LocatedKillingBlows;
        coverage.unresolvedRouteCellKillingBlows =
            aggregate.UnresolvedRouteCellKillingBlows;
        coverage.distinctDamagedMonsterCount =
            aggregate.DamagedMonsterInstanceIds.Count;

        List<TowerRouteDamageCellAggregate> cells =
            new List<TowerRouteDamageCellAggregate>(aggregate.Cells.Values);
        cells.Sort(CompareTowerRouteDamageCells);
        HashSet<string> distinctGridCells = new HashSet<string>();

        for (int i = 0; i < cells.Count; i++)
        {
            TowerRouteDamageCellAggregate cell = cells[i];
            distinctGridCells.Add(CreateRouteGridCellKey(cell.X, cell.Z));
            coverage.routeCells.Add(
                new CombatBalanceTowerRouteDamageCellJson
                {
                    placementCommitOrdinal = cell.PlacementCommitOrdinal,
                    x = cell.X,
                    z = cell.Z,
                    successfulDamageApplications =
                        cell.SuccessfulDamageApplications,
                    effectiveDamage = cell.EffectiveDamage,
                    killingBlows = cell.KillingBlows,
                    distinctDamagedMonsterCount =
                        cell.DamagedMonsterInstanceIds.Count
                });
        }

        coverage.distinctDamageRouteCellCount = distinctGridCells.Count;
        return coverage;
    }

    private CombatBalanceTowerRouteDamageOverlapJson
        CreateTowerRouteDamageOverlapJson(
            CombatBalanceTowerRouteDamageCoverageJson left,
            CombatBalanceTowerRouteDamageCoverageJson right)
    {
        towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
            left.towerInstanceId,
            out TowerRouteDamageCoverageAggregate leftAggregate);
        towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
            right.towerInstanceId,
            out TowerRouteDamageCoverageAggregate rightAggregate);

        return new CombatBalanceTowerRouteDamageOverlapJson
        {
            towerAInstanceId = left.towerInstanceId,
            towerADeploymentOrdinal = left.towerDeploymentOrdinal,
            towerADisplayName = left.towerDisplayName ?? string.Empty,
            towerAFamily = left.towerFamily ?? string.Empty,
            towerBInstanceId = right.towerInstanceId,
            towerBDeploymentOrdinal = right.towerDeploymentOrdinal,
            towerBDisplayName = right.towerDisplayName ?? string.Empty,
            towerBFamily = right.towerFamily ?? string.Empty,
            sharedDamageRouteCellCount = CountSharedRouteGridCells(
                leftAggregate,
                rightAggregate),
            sharedRoutePhaseCellCount = CountSharedRoutePhaseCells(
                leftAggregate,
                rightAggregate),
            sharedDamagedMonsterCount = CountSharedMonsterInstances(
                leftAggregate,
                rightAggregate)
        };
    }

    private static int CompareTowerRouteDamageCells(
        TowerRouteDamageCellAggregate left,
        TowerRouteDamageCellAggregate right)
    {
        int phaseComparison = left.PlacementCommitOrdinal.CompareTo(
            right.PlacementCommitOrdinal);

        if (phaseComparison != 0)
        {
            return phaseComparison;
        }

        int xComparison = left.X.CompareTo(right.X);
        return xComparison != 0 ? xComparison : left.Z.CompareTo(right.Z);
    }

    private static int CountSharedRouteGridCells(
        TowerRouteDamageCoverageAggregate left,
        TowerRouteDamageCoverageAggregate right)
    {
        return CountSharedStrings(
            CreateRouteGridCellKeys(left),
            CreateRouteGridCellKeys(right));
    }

    private static int CountSharedRoutePhaseCells(
        TowerRouteDamageCoverageAggregate left,
        TowerRouteDamageCoverageAggregate right)
    {
        HashSet<string> leftKeys = left != null
            ? new HashSet<string>(left.Cells.Keys)
            : new HashSet<string>();
        HashSet<string> rightKeys = right != null
            ? new HashSet<string>(right.Cells.Keys)
            : new HashSet<string>();
        return CountSharedStrings(leftKeys, rightKeys);
    }

    private static int CountSharedMonsterInstances(
        TowerRouteDamageCoverageAggregate left,
        TowerRouteDamageCoverageAggregate right)
    {
        HashSet<int> leftIds = left != null
            ? left.DamagedMonsterInstanceIds
            : new HashSet<int>();
        HashSet<int> rightIds = right != null
            ? right.DamagedMonsterInstanceIds
            : new HashSet<int>();
        HashSet<int> smaller = leftIds.Count <= rightIds.Count
            ? leftIds
            : rightIds;
        HashSet<int> larger = ReferenceEquals(smaller, leftIds)
            ? rightIds
            : leftIds;
        int sharedCount = 0;

        foreach (int value in smaller)
        {
            if (larger.Contains(value))
            {
                sharedCount++;
            }
        }

        return sharedCount;
    }

    private static HashSet<string> CreateRouteGridCellKeys(
        TowerRouteDamageCoverageAggregate aggregate)
    {
        HashSet<string> keys = new HashSet<string>();

        if (aggregate == null)
        {
            return keys;
        }

        foreach (TowerRouteDamageCellAggregate cell in aggregate.Cells.Values)
        {
            keys.Add(CreateRouteGridCellKey(cell.X, cell.Z));
        }

        return keys;
    }

    private static string CreateRouteGridCellKey(int x, int z)
    {
        return x.ToString(CultureInfo.InvariantCulture) + ":" +
               z.ToString(CultureInfo.InvariantCulture);
    }

    private static int CountSharedStrings(
        HashSet<string> left,
        HashSet<string> right)
    {
        HashSet<string> smaller = left.Count <= right.Count ? left : right;
        HashSet<string> larger = ReferenceEquals(smaller, left) ? right : left;
        int sharedCount = 0;

        foreach (string value in smaller)
        {
            if (larger.Contains(value))
            {
                sharedCount++;
            }
        }

        return sharedCount;
    }

    private bool FixtureSnapshotIsComplete(
        CombatBalanceFixtureJson fixture)
    {
        if (fixture == null ||
            string.IsNullOrWhiteSpace(fixture.stageDefinitionName) ||
            string.IsNullOrWhiteSpace(fixture.mapTemplateName) ||
            string.IsNullOrWhiteSpace(fixture.runtimeMapName) ||
            fixture.mapWidth <= 0 ||
            fixture.mapLength <= 0 ||
            fixture.mapNodeSize <= 0f ||
            fixture.mapNodes == null ||
            fixture.mapNodes.Count != fixture.mapWidth * fixture.mapLength ||
            string.IsNullOrWhiteSpace(fixture.waveConfigName) ||
            fixture.waves == null ||
            fixture.waves.Count == 0 ||
            (hasExpectedWaveCount &&
             fixture.waves.Count != expectedWaveCount))
        {
            return false;
        }

        for (int i = 0; i < fixture.waves.Count; i++)
        {
            CombatBalanceWaveFixtureJson wave = fixture.waves[i];

            if (wave == null ||
                wave.waveNumber != i + 1 ||
                string.IsNullOrWhiteSpace(wave.runtimeTemplateName) ||
                wave.maximumHealth <= 0 ||
                wave.moveSpeed <= 0f ||
                wave.count <= 0 ||
                wave.spawnIntervalSeconds < 0f ||
                wave.waveDelaySeconds < 0f)
            {
                return false;
            }
        }

        return true;
    }

    private static bool InvestmentCommitsMatchDraftSelections(
        CombatBalanceInvestmentRuntimeJson investmentRuntime,
        CombatBalanceDraftRuntimeJson draftRuntime)
    {
        if (investmentRuntime == null || draftRuntime == null)
        {
            return false;
        }

        HashSet<string> committedTokens = new HashSet<string>(
            StringComparer.Ordinal);

        for (int i = 0; i < investmentRuntime.commits.Count; i++)
        {
            CombatBalanceInvestmentCommitJson commit =
                investmentRuntime.commits[i];
            CombatBalanceDraftAttemptJson matchingAttempt = null;

            if (string.Equals(
                    commit.authoritySource,
                    "EditorDebug",
                    StringComparison.Ordinal))
            {
                if (commit.draftOrdinal != 0 ||
                    !committedTokens.Add(commit.draftAttemptToken))
                {
                    return false;
                }

                continue;
            }

            for (int j = 0; j < draftRuntime.attempts.Count; j++)
            {
                CombatBalanceDraftAttemptJson attempt =
                    draftRuntime.attempts[j];

                if (string.Equals(
                        attempt.attemptToken,
                        commit.draftAttemptToken,
                        StringComparison.Ordinal))
                {
                    matchingAttempt = attempt;
                    break;
                }
            }

            if (matchingAttempt == null ||
                !matchingAttempt.selectionCommitted ||
                matchingAttempt.selectedChoice == null ||
                matchingAttempt.ordinal != commit.draftOrdinal ||
                !string.Equals(
                    matchingAttempt.selectedChoice.resultType,
                    commit.draftResultType,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    matchingAttempt.selectedChoice.assetName,
                    commit.draftAssetName,
                    StringComparison.Ordinal) ||
                !committedTokens.Add(commit.draftAttemptToken))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TowerWaveAttributionMatches(
        IReadOnlyList<CombatBalanceTowerWaveJson> summaries,
        CombatBalanceDamageDiagnosticsJson diagnostics)
    {
        if (summaries == null || diagnostics == null)
        {
            return false;
        }

        int attributedApplications = 0;

        for (int i = 0; i < summaries.Count; i++)
        {
            CombatBalanceTowerWaveJson summary = summaries[i];

            if (summary == null ||
                summary.towerInstanceId == 0 ||
                summary.waveNumber <= 0 ||
                summary.successfulDamageApplications <= 0 ||
                summary.effectiveTowerScaledDamage <= 0)
            {
                return false;
            }

            attributedApplications += summary.successfulDamageApplications;
        }

        return attributedApplications ==
            diagnostics.towerScaledSuccessfulApplicationCount;
    }

    private bool TowerRouteDamageCoverageMatches(
        CombatBalanceTowerRouteDamageCoverageRuntimeJson runtime,
        IReadOnlyList<CombatBalanceTowerJson> towers,
        IReadOnlyList<CombatBalanceTowerWaveJson> towerWaveSummaries)
    {
        if (runtime == null ||
            towers == null ||
            towerWaveSummaries == null ||
            runtime.towers == null ||
            runtime.overlaps == null ||
            runtime.observedTowerCount != towers.Count ||
            runtime.towers.Count != towers.Count ||
            runtime.overlaps.Count != towers.Count * (towers.Count - 1) / 2)
        {
            return false;
        }

        int expectedApplications = 0;
        int expectedDamage = 0;
        int expectedKillingBlows = 0;

        for (int i = 0; i < towerWaveSummaries.Count; i++)
        {
            CombatBalanceTowerWaveJson summary = towerWaveSummaries[i];

            if (summary == null)
            {
                return false;
            }

            expectedApplications += summary.successfulDamageApplications;
            expectedDamage += summary.effectiveTowerScaledDamage;
            expectedKillingBlows += summary.killingBlows;
        }

        if (runtime.successfulDamageApplications != expectedApplications ||
            runtime.effectiveDamage != expectedDamage ||
            runtime.killingBlows != expectedKillingBlows ||
            runtime.locatedDamageApplications +
                runtime.unresolvedRouteCellApplicationCount !=
                    runtime.successfulDamageApplications ||
            runtime.locatedEffectiveDamage +
                runtime.unresolvedRouteCellEffectiveDamage !=
                    runtime.effectiveDamage ||
            runtime.locatedKillingBlows +
                runtime.unresolvedRouteCellKillingBlows !=
                    runtime.killingBlows)
        {
            return false;
        }

        HashSet<int> observedTowerIds = new HashSet<int>();
        int observedApplications = 0;
        int observedLocatedApplications = 0;
        int observedUnresolvedApplications = 0;
        int observedDamage = 0;
        int observedLocatedDamage = 0;
        int observedUnresolvedDamage = 0;
        int observedKillingBlows = 0;
        int observedLocatedKillingBlows = 0;
        int observedUnresolvedKillingBlows = 0;

        for (int i = 0; i < runtime.towers.Count; i++)
        {
            CombatBalanceTowerRouteDamageCoverageJson coverage =
                runtime.towers[i];
            CombatBalanceTowerJson tower = towers[i];

            if (coverage == null ||
                tower == null ||
                coverage.towerInstanceId != tower.instanceId ||
                coverage.towerDeploymentOrdinal != tower.deploymentOrdinal ||
                !observedTowerIds.Add(coverage.towerInstanceId) ||
                coverage.routeCells == null ||
                !TowerRouteDamageCoverageEntryMatches(
                    coverage,
                    towerWaveSummaries))
            {
                return false;
            }

            observedApplications += coverage.successfulDamageApplications;
            observedLocatedApplications += coverage.locatedDamageApplications;
            observedUnresolvedApplications +=
                coverage.unresolvedRouteCellApplicationCount;
            observedDamage += coverage.effectiveDamage;
            observedLocatedDamage += coverage.locatedEffectiveDamage;
            observedUnresolvedDamage +=
                coverage.unresolvedRouteCellEffectiveDamage;
            observedKillingBlows += coverage.killingBlows;
            observedLocatedKillingBlows += coverage.locatedKillingBlows;
            observedUnresolvedKillingBlows +=
                coverage.unresolvedRouteCellKillingBlows;
        }

        if (observedApplications != runtime.successfulDamageApplications ||
            observedLocatedApplications != runtime.locatedDamageApplications ||
            observedUnresolvedApplications !=
                runtime.unresolvedRouteCellApplicationCount ||
            observedDamage != runtime.effectiveDamage ||
            observedLocatedDamage != runtime.locatedEffectiveDamage ||
            observedUnresolvedDamage !=
                runtime.unresolvedRouteCellEffectiveDamage ||
            observedKillingBlows != runtime.killingBlows ||
            observedLocatedKillingBlows != runtime.locatedKillingBlows ||
            observedUnresolvedKillingBlows !=
                runtime.unresolvedRouteCellKillingBlows)
        {
            return false;
        }

        int overlapIndex = 0;

        for (int leftIndex = 0;
             leftIndex < runtime.towers.Count;
             leftIndex++)
        {
            CombatBalanceTowerRouteDamageCoverageJson left =
                runtime.towers[leftIndex];

            for (int rightIndex = leftIndex + 1;
                 rightIndex < runtime.towers.Count;
                 rightIndex++)
            {
                CombatBalanceTowerRouteDamageCoverageJson right =
                    runtime.towers[rightIndex];
                CombatBalanceTowerRouteDamageOverlapJson overlap =
                    runtime.overlaps[overlapIndex++];

                towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
                    left.towerInstanceId,
                    out TowerRouteDamageCoverageAggregate leftAggregate);
                towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
                    right.towerInstanceId,
                    out TowerRouteDamageCoverageAggregate rightAggregate);

                if (overlap == null ||
                    overlap.towerAInstanceId != left.towerInstanceId ||
                    overlap.towerADeploymentOrdinal !=
                        left.towerDeploymentOrdinal ||
                    overlap.towerBInstanceId != right.towerInstanceId ||
                    overlap.towerBDeploymentOrdinal !=
                        right.towerDeploymentOrdinal ||
                    overlap.sharedDamageRouteCellCount !=
                        CountSharedRouteGridCells(
                            leftAggregate,
                            rightAggregate) ||
                    overlap.sharedRoutePhaseCellCount !=
                        CountSharedRoutePhaseCells(
                            leftAggregate,
                            rightAggregate) ||
                    overlap.sharedDamagedMonsterCount !=
                        CountSharedMonsterInstances(
                            leftAggregate,
                            rightAggregate))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool TowerRouteDamageCoverageEntryMatches(
        CombatBalanceTowerRouteDamageCoverageJson coverage,
        IReadOnlyList<CombatBalanceTowerWaveJson> towerWaveSummaries)
    {
        int expectedApplications = 0;
        int expectedDamage = 0;
        int expectedKillingBlows = 0;

        for (int i = 0; i < towerWaveSummaries.Count; i++)
        {
            CombatBalanceTowerWaveJson summary = towerWaveSummaries[i];

            if (summary.towerInstanceId != coverage.towerInstanceId)
            {
                continue;
            }

            expectedApplications += summary.successfulDamageApplications;
            expectedDamage += summary.effectiveTowerScaledDamage;
            expectedKillingBlows += summary.killingBlows;
        }

        towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
            coverage.towerInstanceId,
            out TowerRouteDamageCoverageAggregate aggregate);
        int expectedDistinctMonsters = aggregate != null
            ? aggregate.DamagedMonsterInstanceIds.Count
            : 0;
        int cellApplications = 0;
        int cellDamage = 0;
        int cellKillingBlows = 0;
        HashSet<string> distinctGridCells = new HashSet<string>();

        for (int i = 0; i < coverage.routeCells.Count; i++)
        {
            CombatBalanceTowerRouteDamageCellJson cell =
                coverage.routeCells[i];

            if (cell == null ||
                cell.placementCommitOrdinal < 0 ||
                cell.successfulDamageApplications <= 0 ||
                cell.effectiveDamage <= 0 ||
                cell.distinctDamagedMonsterCount <= 0)
            {
                return false;
            }

            cellApplications += cell.successfulDamageApplications;
            cellDamage += cell.effectiveDamage;
            cellKillingBlows += cell.killingBlows;
            distinctGridCells.Add(CreateRouteGridCellKey(cell.x, cell.z));
        }

        return coverage.successfulDamageApplications == expectedApplications &&
               coverage.effectiveDamage == expectedDamage &&
               coverage.killingBlows == expectedKillingBlows &&
               coverage.distinctDamagedMonsterCount ==
                   expectedDistinctMonsters &&
               coverage.distinctDamageRouteCellCount ==
                   distinctGridCells.Count &&
               coverage.locatedDamageApplications == cellApplications &&
               coverage.locatedEffectiveDamage == cellDamage &&
               coverage.locatedKillingBlows == cellKillingBlows &&
               coverage.locatedDamageApplications +
                   coverage.unresolvedRouteCellApplicationCount ==
                       coverage.successfulDamageApplications &&
               coverage.locatedEffectiveDamage +
                   coverage.unresolvedRouteCellEffectiveDamage ==
                       coverage.effectiveDamage &&
               coverage.locatedKillingBlows +
                   coverage.unresolvedRouteCellKillingBlows ==
                       coverage.killingBlows;
    }

    private CombatBalanceWaveRuntimeJson CreateWaveRuntimeJson()
    {
        CombatBalanceWaveRuntimeJson runtime =
            new CombatBalanceWaveRuntimeJson
            {
                events = new List<CombatBalanceWaveEventJson>(waveEvents)
            };
        List<WaveResolutionAggregate> aggregates =
            new List<WaveResolutionAggregate>();

        for (int i = 0; i < runtime.events.Count; i++)
        {
            CombatBalanceWaveEventJson waveEvent = runtime.events[i];
            WaveResolutionAggregate aggregate =
                GetOrCreateWaveResolutionAggregate(
                    aggregates,
                    waveEvent.waveNumber);
            aggregate.RuntimeTemplateName =
                waveEvent.runtimeTemplateName ?? string.Empty;
            aggregate.ConfiguredCount = waveEvent.configuredCount;

            if (string.Equals(
                    waveEvent.kind,
                    "SpawnStarted",
                    StringComparison.Ordinal))
            {
                runtime.observedWaveStartCount++;
            }
            else if (string.Equals(
                         waveEvent.kind,
                         "SpawnCompleted",
                         StringComparison.Ordinal))
            {
                runtime.observedWaveCompletionCount++;
            }
        }

        for (int i = 0; i < monsterObservations.Count; i++)
        {
            MonsterObservation observation = monsterObservations[i];

            if (observation == null || observation.SourceWaveNumber <= 0)
            {
                continue;
            }

            WaveResolutionAggregate aggregate =
                GetOrCreateWaveResolutionAggregate(
                    aggregates,
                    observation.SourceWaveNumber);

            if (string.IsNullOrWhiteSpace(aggregate.RuntimeTemplateName))
            {
                aggregate.RuntimeTemplateName =
                    observation.RuntimeTemplateName;
            }

            aggregate.Spawned++;
            aggregate.SuccessfulDamageApplications +=
                observation.SuccessfulDamageApplications;
            aggregate.EffectiveDamage += observation.EffectiveDamage;

            if (!observation.IsResolved)
            {
                continue;
            }

            if (aggregate.Resolved == 0)
            {
                aggregate.FirstResolutionAtSeconds =
                    observation.ResolvedAtSeconds;
            }
            else
            {
                aggregate.FirstResolutionAtSeconds = Mathf.Min(
                    aggregate.FirstResolutionAtSeconds,
                    observation.ResolvedAtSeconds);
            }

            aggregate.Resolved++;
            aggregate.LastResolutionAtSeconds = Mathf.Max(
                aggregate.LastResolutionAtSeconds,
                observation.ResolvedAtSeconds);

            if (observation.ReachedTarget)
            {
                aggregate.Leaked++;
                aggregate.LeakedRemainingHealth += observation.FinalHealth;
            }
            else
            {
                aggregate.Killed++;
            }
        }

        aggregates.Sort(
            (left, right) => left.WaveNumber.CompareTo(right.WaveNumber));

        for (int i = 0; i < aggregates.Count; i++)
        {
            WaveResolutionAggregate aggregate = aggregates[i];
            runtime.resolutionSummaries.Add(
                new CombatBalanceWaveResolutionJson
                {
                    waveNumber = aggregate.WaveNumber,
                    runtimeTemplateName =
                        aggregate.RuntimeTemplateName ?? string.Empty,
                    configuredCount = aggregate.ConfiguredCount,
                    spawned = aggregate.Spawned,
                    resolved = aggregate.Resolved,
                    killed = aggregate.Killed,
                    leaked = aggregate.Leaked,
                    unresolvedAtReport = Mathf.Max(
                        0,
                        aggregate.Spawned - aggregate.Resolved),
                    successfulDamageApplications =
                        aggregate.SuccessfulDamageApplications,
                    effectiveDamage = aggregate.EffectiveDamage,
                    leakedRemainingHealth =
                        aggregate.LeakedRemainingHealth,
                    hasResolvedMonsters = aggregate.Resolved > 0,
                    firstResolutionAtSeconds =
                        aggregate.FirstResolutionAtSeconds,
                    lastResolutionAtSeconds =
                        aggregate.LastResolutionAtSeconds
                });
        }

        return runtime;
    }

    private static WaveResolutionAggregate
        GetOrCreateWaveResolutionAggregate(
            List<WaveResolutionAggregate> aggregates,
            int waveNumber)
    {
        for (int i = 0; i < aggregates.Count; i++)
        {
            if (aggregates[i].WaveNumber == waveNumber)
            {
                return aggregates[i];
            }
        }

        WaveResolutionAggregate created = new WaveResolutionAggregate
        {
            WaveNumber = waveNumber
        };
        aggregates.Add(created);
        return created;
    }

    private static float ResolveSecondsAfterFinalDraft(
        CombatBalanceProgressionJson progression,
        float runDurationSeconds)
    {
        if (progression == null || progression.events.Count == 0)
        {
            return 0f;
        }

        float finalDraftTime = progression.events[
            progression.events.Count - 1].activeTimeSeconds;
        return Mathf.Max(0f, runDurationSeconds - finalDraftTime);
    }

    private CombatBalanceProgressionJson CreateProgressionJson()
    {
        CombatBalanceProgressionJson progression =
            new CombatBalanceProgressionJson
            {
                requirements = new List<int>(
                    progressionRequirementsSnapshot),
                expectedLevelUpCount =
                    progressionRequirementsSnapshot.Count,
                expectedTotalDraftCount =
                    progressionRequirementsSnapshot.Count + 1,
                initialDraftCompleted = initialDraftCompletionCount > 0,
                finalLevel = playerSystem != null
                    ? playerSystem.CurrentLevel
                    : 0,
                finalProgress = playerSystem != null
                    ? playerSystem.CurrentProgress
                    : 0,
                finalRequiredProgress = playerSystem != null
                    ? playerSystem.RequiredProgress
                    : 0,
                events = new List<CombatBalanceProgressionEventJson>(
                    progressionEvents)
            };

        for (int i = 0; i < progression.requirements.Count; i++)
        {
            progression.expectedFinalDraftResolutionNode +=
                progression.requirements[i];
        }

        for (int i = 0; i < progression.events.Count; i++)
        {
            CombatBalanceProgressionEventJson progressionEvent =
                progression.events[i];

            if (!string.Equals(
                    progressionEvent.kind,
                    "LevelUp",
                    StringComparison.Ordinal))
            {
                continue;
            }

            progression.observedLevelUpCount++;
            progression.observedFinalDraftResolutionNode =
                progressionEvent.resolvedMonsterCount;
        }

        progression.observedTotalDraftCount =
            initialDraftCompletionCount + progression.observedLevelUpCount;

        if (progression.observedLevelUpCount > 0)
        {
            progression.resolutionsAfterFinalDraft = Mathf.Max(
                0,
                resolvedCount - progression.observedFinalDraftResolutionNode);
        }

        return progression;
    }

    private static bool LevelUpResolutionNodesMatch(
        CombatBalanceProgressionJson progression)
    {
        if (progression == null ||
            progression.observedLevelUpCount >
            progression.requirements.Count)
        {
            return false;
        }

        int expectedResolutionNode = 0;
        int levelUpIndex = 0;

        for (int i = 0; i < progression.events.Count; i++)
        {
            CombatBalanceProgressionEventJson progressionEvent =
                progression.events[i];

            if (!string.Equals(
                    progressionEvent.kind,
                    "LevelUp",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (levelUpIndex >= progression.requirements.Count)
            {
                return false;
            }

            expectedResolutionNode += progression.requirements[levelUpIndex];

            if (progressionEvent.resolvedMonsterCount !=
                expectedResolutionNode)
            {
                return false;
            }

            levelUpIndex++;
        }

        return levelUpIndex == progression.observedLevelUpCount;
    }

    private float GetRunActiveTimeSeconds()
    {
        return Mathf.Max(0f, Time.time - runStartedAtTime);
    }

    private CombatBalanceMonsterRuntimeJson CreateMonsterRuntimeJson()
    {
        CombatBalanceMonsterRuntimeJson runtime =
            new CombatBalanceMonsterRuntimeJson();
        List<MonsterTypeAggregate> aggregates =
            new List<MonsterTypeAggregate>();
        float observedAtTime = Time.time;

        for (int i = 0; i < monsterObservations.Count; i++)
        {
            MonsterObservation observation = monsterObservations[i];

            if (observation == null)
            {
                continue;
            }

            float lifetimeSeconds = ResolveMonsterLifetimeSeconds(
                observation,
                observedAtTime);
            int finalHealth = observation.IsResolved
                ? observation.FinalHealth
                : observation.LastHealth;
            string resolutionType = ResolveMonsterResolutionType(observation);
            MonsterTypeAggregate aggregate = GetOrCreateMonsterTypeAggregate(
                aggregates,
                observation);

            aggregate.Spawned++;
            aggregate.SuccessfulDamageApplications +=
                observation.SuccessfulDamageApplications;
            aggregate.EffectiveDamage += observation.EffectiveDamage;
            aggregate.UnobservedDamageAtObservationStart +=
                observation.UnobservedDamageAtObservationStart;
            runtime.successfulDamageApplications +=
                observation.SuccessfulDamageApplications;
            runtime.unobservedDamageAtObservationStart +=
                observation.UnobservedDamageAtObservationStart;

            if (observation.ObservedThroughRegistrationEvent)
            {
                aggregate.RegistrationObservedInstances++;
                runtime.registrationObservedInstances++;
            }
            else
            {
                aggregate.FallbackObservedInstances++;
                runtime.fallbackObservedInstances++;
            }

            if (observation.HealthAtObservationStart ==
                observation.MaximumHealth)
            {
                aggregate.InstancesObservedAtFullHealth++;
                runtime.instancesObservedAtFullHealth++;
            }
            else
            {
                aggregate.InstancesObservedAfterDamage++;
                runtime.instancesObservedAfterDamage++;
            }

            if (observation.IsResolved)
            {
                aggregate.Resolved++;
                aggregate.ResolutionLifetimeSeconds.Add(lifetimeSeconds);

                if (observation.ReachedTarget)
                {
                    aggregate.Leaked++;
                    aggregate.LeakedRemainingHealth += finalHealth;
                    aggregate.LeakedLifetimeSeconds.Add(lifetimeSeconds);
                }
                else
                {
                    aggregate.Killed++;
                    aggregate.KilledLifetimeSeconds.Add(lifetimeSeconds);
                }
            }

            runtime.instances.Add(new CombatBalanceMonsterInstanceJson
            {
                spawnOrdinal = observation.SpawnOrdinal,
                sourceWaveNumber = observation.SourceWaveNumber,
                sourceWaveSpawnOrdinal =
                    observation.SourceWaveSpawnOrdinal,
                runtimeTemplateName = observation.RuntimeTemplateName,
                displayName = observation.DisplayName,
                maximumHealth = observation.MaximumHealth,
                moveSpeedAtSpawn = observation.MoveSpeedAtSpawn,
                spawnedAtSeconds = observation.SpawnedAtSeconds,
                resolvedAtSeconds = observation.IsResolved
                    ? observation.ResolvedAtSeconds
                    : 0f,
                observedThroughRegistrationEvent =
                    observation.ObservedThroughRegistrationEvent,
                healthAtObservationStart =
                    observation.HealthAtObservationStart,
                unobservedDamageAtObservationStart =
                    observation.UnobservedDamageAtObservationStart,
                resolutionType = resolutionType,
                finalHealth = finalHealth,
                successfulDamageApplications =
                    observation.SuccessfulDamageApplications,
                effectiveDamage = observation.EffectiveDamage,
                lifetimeSeconds = lifetimeSeconds
            });
        }

        for (int i = 0; i < aggregates.Count; i++)
        {
            MonsterTypeAggregate aggregate = aggregates[i];
            runtime.types.Add(new CombatBalanceMonsterTypeJson
            {
                runtimeTemplateName = aggregate.RuntimeTemplateName,
                displayName = aggregate.DisplayName,
                maximumHealth = aggregate.MaximumHealth,
                moveSpeedAtSpawn = aggregate.MoveSpeedAtSpawn,
                spawned = aggregate.Spawned,
                resolved = aggregate.Resolved,
                killed = aggregate.Killed,
                leaked = aggregate.Leaked,
                unresolvedAtReport = Mathf.Max(
                    0,
                    aggregate.Spawned - aggregate.Resolved),
                registrationObservedInstances =
                    aggregate.RegistrationObservedInstances,
                fallbackObservedInstances =
                    aggregate.FallbackObservedInstances,
                instancesObservedAtFullHealth =
                    aggregate.InstancesObservedAtFullHealth,
                instancesObservedAfterDamage =
                    aggregate.InstancesObservedAfterDamage,
                unobservedDamageAtObservationStart =
                    aggregate.UnobservedDamageAtObservationStart,
                successfulDamageApplications =
                    aggregate.SuccessfulDamageApplications,
                effectiveDamage = aggregate.EffectiveDamage,
                leakedRemainingHealth = aggregate.LeakedRemainingHealth,
                resolutionLifetimeSeconds = CreateMetricJson(
                    aggregate.ResolutionLifetimeSeconds),
                killedLifetimeSeconds = CreateMetricJson(
                    aggregate.KilledLifetimeSeconds),
                leakedLifetimeSeconds = CreateMetricJson(
                    aggregate.LeakedLifetimeSeconds)
            });
        }

        runtime.observedTypes = runtime.types.Count;
        runtime.instanceSamples = runtime.instances.Count;
        return runtime;
    }

    private static MonsterTypeAggregate GetOrCreateMonsterTypeAggregate(
        List<MonsterTypeAggregate> aggregates,
        MonsterObservation observation)
    {
        for (int i = 0; i < aggregates.Count; i++)
        {
            MonsterTypeAggregate aggregate = aggregates[i];

            if (string.Equals(
                    aggregate.RuntimeTemplateName,
                    observation.RuntimeTemplateName,
                    StringComparison.Ordinal) &&
                aggregate.MaximumHealth == observation.MaximumHealth &&
                Mathf.Approximately(
                    aggregate.MoveSpeedAtSpawn,
                    observation.MoveSpeedAtSpawn))
            {
                return aggregate;
            }
        }

        MonsterTypeAggregate created =
            new MonsterTypeAggregate(observation);
        aggregates.Add(created);
        return created;
    }

    private static float ResolveMonsterLifetimeSeconds(
        MonsterObservation observation,
        float observedAtTime)
    {
        float resolvedTime = observation.IsResolved
            ? observation.ResolutionTime
            : observedAtTime;
        return Mathf.Max(0f, resolvedTime - observation.SpawnTime);
    }

    private static string ResolveMonsterResolutionType(
        MonsterObservation observation)
    {
        if (!observation.IsResolved)
        {
            return "Unresolved";
        }

        return observation.ReachedTarget ? "Leaked" : "Killed";
    }

    private void AppendMonsterRuntimeSummary(
        StringBuilder builder,
        CombatBalanceMonsterRuntimeJson runtime)
    {
        builder.Append("Monster Runtime: Types=")
            .Append(runtime.observedTypes)
            .Append(", InstanceSamples=")
            .Append(runtime.instanceSamples)
            .Append(", RegistrationObserved=")
            .Append(runtime.registrationObservedInstances)
            .Append(", FallbackObserved=")
            .Append(runtime.fallbackObservedInstances)
            .Append(", ObservedAfterDamage=")
            .Append(runtime.instancesObservedAfterDamage)
            .Append(", UnobservedInitialDamage=")
            .Append(runtime.unobservedDamageAtObservationStart)
            .Append(", SuccessfulDamageApplications=")
            .Append(runtime.successfulDamageApplications)
            .AppendLine();

        for (int i = 0; i < runtime.types.Count; i++)
        {
            CombatBalanceMonsterTypeJson type = runtime.types[i];
            builder.Append("- Monster Type: ")
                .Append(type.displayName)
                .Append(" {Template=")
                .Append(type.runtimeTemplateName)
                .Append(", HP=")
                .Append(type.maximumHealth)
                .Append(", Speed=")
                .Append(FormatFloat(type.moveSpeedAtSpawn))
                .Append(", Spawned=")
                .Append(type.spawned)
                .Append(", Killed=")
                .Append(type.killed)
                .Append(", Leaked=")
                .Append(type.leaked)
                .Append(", Unresolved=")
                .Append(type.unresolvedAtReport)
                .Append(", RegistrationObserved=")
                .Append(type.registrationObservedInstances)
                .Append(", FallbackObserved=")
                .Append(type.fallbackObservedInstances)
                .Append(", ObservedAfterDamage=")
                .Append(type.instancesObservedAfterDamage)
                .Append(", UnobservedInitialDamage=")
                .Append(type.unobservedDamageAtObservationStart)
                .Append(", DamageApplications=")
                .Append(type.successfulDamageApplications)
                .Append(", EffectiveDamage=")
                .Append(type.effectiveDamage)
                .Append(", KilledLifetime=")
                .Append(FormatMetricAverageSeconds(
                    type.killedLifetimeSeconds))
                .Append(", LeakedLifetime=")
                .Append(FormatMetricAverageSeconds(
                    type.leakedLifetimeSeconds))
                .AppendLine("}");
        }
    }

    private static string FormatMetricAverageSeconds(
        CombatBalanceMetricJson metric)
    {
        return metric != null && metric.samples > 0
            ? FormatSeconds(metric.average)
            : "N/A";
    }

    private bool MonsterRuntimeCountsMatch(
        CombatBalanceMonsterRuntimeJson runtime)
    {
        if (runtime == null || runtime.instanceSamples != spawnedCount)
        {
            return false;
        }

        int observedSpawned = 0;
        int observedResolved = 0;
        int observedKilled = 0;
        int observedLeaked = 0;

        for (int i = 0; i < runtime.types.Count; i++)
        {
            CombatBalanceMonsterTypeJson type = runtime.types[i];
            observedSpawned += type.spawned;
            observedResolved += type.resolved;
            observedKilled += type.killed;
            observedLeaked += type.leaked;
        }

        return observedSpawned == spawnedCount &&
               observedResolved == resolvedCount &&
               observedKilled == killedCount &&
               observedLeaked == leakedCount;
    }

    private bool WaveMonsterAttributionMatches(
        CombatBalanceWaveRuntimeJson runtime)
    {
        if (runtime == null || runtime.resolutionSummaries == null)
        {
            return false;
        }

        int attributedSpawned = 0;
        int attributedResolved = 0;
        int attributedKilled = 0;
        int attributedLeaked = 0;
        int attributedUnresolved = 0;
        int attributedEffectiveDamage = 0;
        int attributedLeakedRemainingHealth = 0;

        for (int i = 0; i < runtime.resolutionSummaries.Count; i++)
        {
            CombatBalanceWaveResolutionJson summary =
                runtime.resolutionSummaries[i];

            if (summary == null || summary.waveNumber <= 0)
            {
                return false;
            }

            attributedSpawned += summary.spawned;
            attributedResolved += summary.resolved;
            attributedKilled += summary.killed;
            attributedLeaked += summary.leaked;
            attributedUnresolved += summary.unresolvedAtReport;
            attributedEffectiveDamage += summary.effectiveDamage;
            attributedLeakedRemainingHealth +=
                summary.leakedRemainingHealth;
        }

        return attributedSpawned == spawnedCount &&
               attributedResolved == resolvedCount &&
               attributedKilled == killedCount &&
               attributedLeaked == leakedCount &&
               attributedUnresolved ==
                   Mathf.Max(0, spawnedCount - resolvedCount) &&
               attributedEffectiveDamage == effectiveDamage &&
               attributedLeakedRemainingHealth == leakedRemainingHealth;
    }

    private bool MonsterRuntimeDamageMatches(
        CombatBalanceMonsterRuntimeJson runtime)
    {
        if (runtime == null)
        {
            return false;
        }

        int observedEffectiveDamage = 0;

        for (int i = 0; i < runtime.types.Count; i++)
        {
            observedEffectiveDamage += runtime.types[i].effectiveDamage;
        }

        return observedEffectiveDamage == effectiveDamage;
    }

    private bool MonsterRuntimeRegistrationCoverageMatches(
        CombatBalanceMonsterRuntimeJson runtime)
    {
        return runtime != null &&
               runtime.instanceSamples == spawnedCount &&
               runtime.registrationObservedInstances == spawnedCount &&
               runtime.fallbackObservedInstances == 0;
    }

    private static bool MonsterRuntimeStartedAtFullHealth(
        CombatBalanceMonsterRuntimeJson runtime)
    {
        return runtime != null &&
               runtime.instancesObservedAtFullHealth ==
                   runtime.instanceSamples &&
               runtime.instancesObservedAfterDamage == 0 &&
               runtime.unobservedDamageAtObservationStart == 0;
    }

    private bool TowerDeploymentCoverageMatches(
        IReadOnlyList<CombatBalanceTowerJson> towers)
    {
        if (towers == null ||
            towers.Count == 0 ||
            towerDeploymentByInstanceId.Count != towers.Count)
        {
            return false;
        }

        HashSet<int> observedOrdinals = new HashSet<int>();

        for (int i = 0; i < towers.Count; i++)
        {
            CombatBalanceTowerJson tower = towers[i];

            if (tower == null ||
                !tower.deploymentObserved ||
                tower.deploymentOrdinal <= 0 ||
                tower.deploymentGridPositions == null ||
                tower.deploymentGridPositions.Count == 0 ||
                !observedOrdinals.Add(tower.deploymentOrdinal))
            {
                return false;
            }
        }

        return true;
    }

    private static CombatBalanceVector3Json CreateVector3Json(Vector3 value)
    {
        return new CombatBalanceVector3Json
        {
            x = SanitizeFinite(value.x),
            y = SanitizeFinite(value.y),
            z = SanitizeFinite(value.z)
        };
    }

    private static float SanitizeFinite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }

    private static CombatBalanceGridPositionJson CreateGridPositionJson(
        GridNodeBehaviour node)
    {
        if (node == null)
        {
            return null;
        }

        return new CombatBalanceGridPositionJson
        {
            x = node.GridPosition.x,
            z = node.GridPosition.y
        };
    }

    private static void AppendGridPositions(
        List<CombatBalanceGridPositionJson> destination,
        IReadOnlyList<GridNodeBehaviour> source)
    {
        if (destination == null || source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            CombatBalanceGridPositionJson position =
                CreateGridPositionJson(source[i]);

            if (position != null)
            {
                destination.Add(position);
            }
        }
    }

    private static string CreatePlacementGameplayStateFingerprint(
        MonsterPlacementGameplayStateSnapshot state)
    {
        if (state == null)
        {
            return "Missing";
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "health={0}/{1};speed={2:R};locked={3};lane={4};targetable={5};buff={6}",
            state.CurrentHealth,
            state.MaximumHealth,
            state.MoveSpeedMultiplier,
            state.IsMovementLocked,
            state.LaneIdentity,
            state.IsGameplayTargetable,
            state.BuffFingerprint ?? string.Empty);
    }

    private List<CombatBalanceTowerJson> CreateTowerJsonRecords()
    {
        TowerInstance[] towerInstances = FindObjectsByType<TowerInstance>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        Array.Sort(
            towerInstances,
            (left, right) => string.CompareOrdinal(
                GetTowerSortKey(left),
                GetTowerSortKey(right)));

        List<CombatBalanceTowerJson> records =
            new List<CombatBalanceTowerJson>();

        for (int i = 0; i < towerInstances.Length; i++)
        {
            TowerInstance towerInstance = towerInstances[i];

            if (towerInstance == null || towerInstance.TowerDefinition == null)
            {
                continue;
            }

            TowerDefinition definition = towerInstance.TowerDefinition;
            TowerCombatBehaviour combatBehaviour =
                towerInstance.GetComponent<TowerCombatBehaviour>();
            int towerInstanceId = towerInstance.GetInstanceID();
            CombatBalanceTowerJson record = new CombatBalanceTowerJson
            {
                instanceId = towerInstanceId,
                displayName = GetDisplayName(
                    definition.DisplayName,
                    definition.name),
                family = definition.TowerFamily.ToString(),
                level = towerInstance.CurrentLevel,
                hasCombatRuntime = combatBehaviour != null
            };

            if (towerDeploymentByInstanceId.TryGetValue(
                    towerInstanceId,
                    out TowerDeploymentObservation deployment))
            {
                record.deploymentObserved = true;
                record.deploymentOrdinal = deployment.DeploymentOrdinal;
                record.deployedAtSeconds = deployment.DeployedAtSeconds;
                record.deploymentWorldPosition =
                    CreateVector3Json(deployment.WorldPosition);

                for (int gridIndex = 0;
                     gridIndex < deployment.GridPositions.Count;
                     gridIndex++)
                {
                    Vector2Int gridPosition =
                        deployment.GridPositions[gridIndex];
                    record.deploymentGridPositions.Add(
                        new CombatBalanceGridPositionJson
                        {
                            x = gridPosition.x,
                            z = gridPosition.y
                        });
                }
            }

            if (combatBehaviour != null)
            {
                TowerCombatBaseStats baseStats = new TowerCombatBaseStats(
                    combatBehaviour.BaseAttackRange,
                    combatBehaviour.BaseAttackCycleDuration);
                ResolvedTowerCombatStats resolvedStats =
                    TowerRuntimeStatResolver.Resolve(towerInstance, baseStats);
                record.baseDamage = resolvedStats.LevelBasicDamage;
                record.baseRange = combatBehaviour.BaseAttackRange;
                record.baseCycleSeconds =
                    combatBehaviour.BaseAttackCycleDuration;
                record.resolvedDamage =
                    Mathf.RoundToInt(resolvedStats.ResolvedBasicDamage);
                record.resolvedRange = resolvedStats.AttackRange;
                record.resolvedCycleSeconds = resolvedStats.AttackCycleDuration;
            }

            record.projectileRuntime = CreateProjectileRuntimeJson(
                towerInstanceId);
            record.droneBurstRuntime = CreateDroneBurstRuntimeJson(
                towerInstanceId);
            record.droneLifecycleRuntime = CreateDroneLifecycleRuntimeJson(
                towerInstanceId);

            IReadOnlyList<TowerUpgradeDefinition> upgrades =
                towerInstance.AppliedUpgrades;

            if (upgrades != null)
            {
                for (int upgradeIndex = 0;
                     upgradeIndex < upgrades.Count;
                     upgradeIndex++)
                {
                    TowerUpgradeDefinition upgrade = upgrades[upgradeIndex];

                    if (upgrade == null)
                    {
                        continue;
                    }

                    record.upgrades.Add(new CombatBalanceUpgradeJson
                    {
                        displayName = GetDisplayName(
                            upgrade.DisplayName,
                            upgrade.name),
                        layer = upgrade.UpgradeLayer.ToString(),
                        requiredLevel = upgrade.RequiredTowerLevel,
                        behaviourPackage =
                            upgrade.UpgradeLayer == TowerUpgradeLayer.Behaviour
                                ? upgrade.BehaviourPackageType.ToString()
                                : string.Empty,
                        element =
                            upgrade.UpgradeLayer == TowerUpgradeLayer.Elemental
                                ? upgrade.ElementType.ToString()
                                : string.Empty,
                        elementalStackContribution =
                            upgrade.UpgradeLayer == TowerUpgradeLayer.Elemental
                                ? upgrade.ElementalStackContribution
                                : 0
                    });
                }
            }

            records.Add(record);
        }

        return records;
    }

    private CombatBalanceProjectileRuntimeJson CreateProjectileRuntimeJson(
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

    private CombatBalanceDroneBurstRuntimeJson CreateDroneBurstRuntimeJson(
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

    private CombatBalanceDroneLifecycleRuntimeJson
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

    private CombatBalanceElementalOpportunityDiagnosticsJson
        CreateElementalOpportunityDiagnosticsJson()
    {
        CombatBalanceElementalOpportunityDiagnosticsJson result =
            new CombatBalanceElementalOpportunityDiagnosticsJson();
        List<string> keys =
            new List<string>(elementalOpportunityByScope.Keys);
        keys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < keys.Count; i++)
        {
            ElementalOpportunityAggregate aggregate =
                elementalOpportunityByScope[keys[i]];
            TowerDefinition towerDefinition = aggregate.SourceTower != null
                ? aggregate.SourceTower.TowerDefinition
                : null;
            result.candidateResults += aggregate.CandidateResults;
            result.eligibleResults += aggregate.EligibleResults;
            result.dispatchedRequests += aggregate.DispatchedRequests;
            result.scopes.Add(new CombatBalanceElementalOpportunityScopeJson
            {
                sourceTowerInstanceId = aggregate.SourceTower != null
                    ? aggregate.SourceTower.GetInstanceID()
                    : 0,
                towerFamily = towerDefinition != null
                    ? towerDefinition.TowerFamily.ToString()
                    : string.Empty,
                elementalUpgradeName = aggregate.ElementalUpgrade != null
                    ? aggregate.ElementalUpgrade.name
                    : string.Empty,
                waveNumber = aggregate.WaveNumber,
                provenance = aggregate.Diagnostics.Provenance.ToString(),
                memberIdentity =
                    aggregate.Diagnostics.MemberIdentity.ToString(),
                resultRole = aggregate.Diagnostics.ResultRole.ToString(),
                minimumResultOrdinal = aggregate.MinimumResultOrdinal,
                maximumResultOrdinal = aggregate.MaximumResultOrdinal,
                candidateResults = aggregate.CandidateResults,
                eligibleResults = aggregate.EligibleResults,
                dispatchedRequests = aggregate.DispatchedRequests
            });
        }

        result.diagnosticsConsistent =
            ElementalOpportunityDiagnosticsAreConsistent();
        return result;
    }

    private bool ElementalOpportunityDiagnosticsAreConsistent()
    {
        Dictionary<string, int> dispatchedBySourceWave =
            new Dictionary<string, int>();

        foreach (KeyValuePair<string, ElementalOpportunityAggregate> entry in
                 elementalOpportunityByScope)
        {
            ElementalOpportunityAggregate aggregate = entry.Value;

            if (aggregate == null ||
                aggregate.SourceTower == null ||
                aggregate.ElementalUpgrade == null ||
                aggregate.WaveNumber <= 0 ||
                aggregate.DispatchedRequests > aggregate.EligibleResults ||
                aggregate.EligibleResults > aggregate.CandidateResults)
            {
                return false;
            }

            bool isApprovedPrimary = IsApprovedPrimaryOpportunityScope(
                aggregate.Diagnostics);

            if (aggregate.Diagnostics.TopologyAuthorized != isApprovedPrimary ||
                (!isApprovedPrimary &&
                 (aggregate.EligibleResults != 0 ||
                  aggregate.DispatchedRequests != 0)))
            {
                return false;
            }

            string sourceWaveKey = CreateElementalSourceWaveKey(
                aggregate.SourceTower,
                aggregate.ElementalUpgrade,
                aggregate.WaveNumber);
            dispatchedBySourceWave.TryGetValue(
                sourceWaveKey,
                out int dispatchedCount);
            dispatchedBySourceWave[sourceWaveKey] =
                dispatchedCount + aggregate.DispatchedRequests;
        }

        foreach (KeyValuePair<string, int> entry in dispatchedBySourceWave)
        {
            elementalBuffApplicationAttemptsBySourceWave.TryGetValue(
                entry.Key,
                out int applicationAttempts);

            if (entry.Value != applicationAttempts)
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, int> entry in
                 elementalBuffApplicationAttemptsBySourceWave)
        {
            dispatchedBySourceWave.TryGetValue(
                entry.Key,
                out int dispatchedCount);

            if (entry.Value != dispatchedCount)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsApprovedPrimaryOpportunityScope(
        ElementalOpportunityDiagnosticContext diagnostics)
    {
        if (diagnostics.MemberIdentity !=
                ElementalOpportunityMemberIdentity.Primary ||
            diagnostics.ResultRole !=
                ElementalOpportunityResultRole.InitialDirect)
        {
            return false;
        }

        switch (diagnostics.Provenance)
        {
            case ElementalOpportunityProvenance.ArcherArrow:
            case ElementalOpportunityProvenance.CannonShell:
            case ElementalOpportunityProvenance.MagicOrb:
            case ElementalOpportunityProvenance.DroneOpeningProjectile:
                return true;
            default:
                return false;
        }
    }

    private bool DroneBurstDiagnosticsAreConsistent()
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

    private bool DroneLifecycleDiagnosticsAreConsistent()
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

    private static bool DroneLifecycleDiagnosticsAreConsistent(
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

    private static bool DroneRuntimeDiagnosticsAreConsistent(
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

    private static CombatBalanceArcTargetRelationJson
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

    private static string GetReportPath(
        string outputDirectory,
        string resolvedRunName)
    {
        if (resolvedRunName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            resolvedRunName.Contains(Path.DirectorySeparatorChar.ToString()) ||
            resolvedRunName.Contains(Path.AltDirectorySeparatorChar.ToString()))
        {
            throw new InvalidOperationException(
                "Run Name contains characters that cannot be used in a " +
                "JSON filename: " + resolvedRunName);
        }

        string outputPath = Path.Combine(
            outputDirectory,
            resolvedRunName + ".json");

        if (!File.Exists(outputPath))
        {
            return outputPath;
        }

        for (int suffix = 1; suffix < 1000; suffix++)
        {
            outputPath = Path.Combine(
                outputDirectory,
                resolvedRunName + "_" +
                suffix.ToString("00", CultureInfo.InvariantCulture) +
                ".json");

            if (!File.Exists(outputPath))
            {
                return outputPath;
            }
        }

        return Path.Combine(
            outputDirectory,
            resolvedRunName + "_" + Guid.NewGuid().ToString("N") + ".json");
    }

    private string ResolveRunName(DateTime generatedAt)
    {
        return string.IsNullOrWhiteSpace(runName)
            ? generatedAt.ToString(
                "yyyyMMdd_HHmmss",
                CultureInfo.InvariantCulture)
            : runName.Trim();
    }

    private static void AppendTowerSnapshot(StringBuilder builder)
    {
        TowerInstance[] towerInstances = FindObjectsByType<TowerInstance>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        Array.Sort(
            towerInstances,
            (left, right) => string.CompareOrdinal(
                GetTowerSortKey(left),
                GetTowerSortKey(right)));

        int validTowerCount = 0;
        int basicUpgradeCount = 0;
        int behaviourUpgradeCount = 0;
        int elementalUpgradeCount = 0;

        for (int i = 0; i < towerInstances.Length; i++)
        {
            TowerInstance towerInstance = towerInstances[i];

            if (towerInstance != null &&
                towerInstance.TowerDefinition != null)
            {
                validTowerCount++;
                CountUpgradeLayers(
                    towerInstance.AppliedUpgrades,
                    ref basicUpgradeCount,
                    ref behaviourUpgradeCount,
                    ref elementalUpgradeCount);
            }
        }

        builder.Append("Towers: Count=")
            .Append(validTowerCount)
            .AppendLine();
        builder.Append("Upgrade Layers: Basic=")
            .Append(basicUpgradeCount)
            .Append(", Behaviour=")
            .Append(behaviourUpgradeCount)
            .Append(", Elemental=")
            .Append(elementalUpgradeCount)
            .AppendLine();

        for (int i = 0; i < towerInstances.Length; i++)
        {
            TowerInstance towerInstance = towerInstances[i];

            if (towerInstance == null || towerInstance.TowerDefinition == null)
            {
                continue;
            }

            AppendTower(builder, towerInstance);
        }
    }

    private static void AppendTower(
        StringBuilder builder,
        TowerInstance towerInstance)
    {
        TowerDefinition definition = towerInstance.TowerDefinition;
        TowerCombatBehaviour combatBehaviour =
            towerInstance.GetComponent<TowerCombatBehaviour>();
        IReadOnlyList<TowerUpgradeDefinition> upgrades =
            towerInstance.AppliedUpgrades;

        builder.Append("- ")
            .Append(GetDisplayName(definition.DisplayName, definition.name))
            .Append(": Family=")
            .Append(definition.TowerFamily)
            .Append(", Level=")
            .Append(towerInstance.CurrentLevel);

        if (combatBehaviour != null)
        {
            TowerCombatBaseStats baseStats = new TowerCombatBaseStats(
                combatBehaviour.BaseAttackRange,
                combatBehaviour.BaseAttackCycleDuration);
            ResolvedTowerCombatStats resolvedStats =
                TowerRuntimeStatResolver.Resolve(towerInstance, baseStats);

            builder.Append(", BaseStats=[Damage=")
                .Append(resolvedStats.LevelBasicDamage)
                .Append(", Range=")
                .Append(FormatFloat(combatBehaviour.BaseAttackRange))
                .Append(", Cycle=")
                .Append(FormatSeconds(
                    combatBehaviour.BaseAttackCycleDuration))
                .Append(']')
                .Append(", ResolvedStats=[Damage=")
                .Append(Mathf.RoundToInt(resolvedStats.ResolvedBasicDamage))
                .Append(", Range=")
                .Append(FormatFloat(resolvedStats.AttackRange))
                .Append(", Cycle=")
                .Append(FormatSeconds(resolvedStats.AttackCycleDuration))
                .Append(']');
        }
        else
        {
            builder.Append(", Combat=Missing");
        }

        builder.Append(", Upgrades=");

        if (upgrades == null || upgrades.Count == 0)
        {
            builder.AppendLine("None");
            return;
        }

        builder.Append('[');

        for (int i = 0; i < upgrades.Count; i++)
        {
            if (i > 0)
            {
                builder.Append("; ");
            }

            AppendUpgrade(builder, upgrades[i]);
        }

        builder.AppendLine("]");
    }

    private static void AppendUpgrade(
        StringBuilder builder,
        TowerUpgradeDefinition upgrade)
    {
        if (upgrade == null)
        {
            builder.Append("Missing Upgrade");
            return;
        }

        builder.Append(GetDisplayName(upgrade.DisplayName, upgrade.name))
            .Append(" {Layer=")
            .Append(upgrade.UpgradeLayer)
            .Append(", RequiredLevel=")
            .Append(upgrade.RequiredTowerLevel);

        if (upgrade.UpgradeLayer == TowerUpgradeLayer.Behaviour)
        {
            builder.Append(", Package=")
                .Append(upgrade.BehaviourPackageType);
        }
        else if (upgrade.UpgradeLayer == TowerUpgradeLayer.Elemental)
        {
            builder.Append(", Element=")
                .Append(upgrade.ElementType);
        }

        builder.Append('}');
    }

    private static void CountUpgradeLayers(
        IReadOnlyList<TowerUpgradeDefinition> upgrades,
        ref int basicCount,
        ref int behaviourCount,
        ref int elementalCount)
    {
        if (upgrades == null)
        {
            return;
        }

        for (int i = 0; i < upgrades.Count; i++)
        {
            TowerUpgradeDefinition upgrade = upgrades[i];

            if (upgrade == null)
            {
                continue;
            }

            switch (upgrade.UpgradeLayer)
            {
                case TowerUpgradeLayer.Basic:
                    basicCount++;
                    break;
                case TowerUpgradeLayer.Behaviour:
                    behaviourCount++;
                    break;
                case TowerUpgradeLayer.Elemental:
                    elementalCount++;
                    break;
            }
        }
    }

    private void UnsubscribeFromTrackedMonsters()
    {
        List<MonsterBehaviour> monsters =
            new List<MonsterBehaviour>(trackedMonsters.Keys);

        for (int i = 0; i < monsters.Count; i++)
        {
            UnsubscribeFromMonster(monsters[i]);
        }
    }

    private void UnsubscribeFromMonster(MonsterBehaviour monster)
    {
        if (monster != null)
        {
            monster.OnHealthChanged -= HandleMonsterHealthChanged;
            monster.OnResolved -= HandleMonsterResolved;
            monster.OnDestroyed -= HandleMonsterDestroyed;
            monster.OnBuffRuntimeObserved -= HandleBuffRuntimeObserved;
            monster.OnElementalHitReactionObserved -=
                HandleElementalHitReactionObserved;
        }

        trackedMonsters.Remove(monster);
    }

    private static string GetTowerSortKey(TowerInstance towerInstance)
    {
        if (towerInstance == null || towerInstance.TowerDefinition == null)
        {
            return string.Empty;
        }

        return towerInstance.TowerDefinition.TowerFamily + ":" +
               towerInstance.GetInstanceID();
    }

    private static string GetDisplayName(string displayName, string fallback)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? fallback
            : displayName.Trim();
    }

    private static string ResolveRuntimeTemplateName(string runtimeName)
    {
        const string cloneSuffix = "(Clone)";
        string resolvedName = string.IsNullOrWhiteSpace(runtimeName)
            ? "Unknown Monster Template"
            : runtimeName.Trim();

        if (resolvedName.EndsWith(
                cloneSuffix,
                StringComparison.Ordinal))
        {
            resolvedName = resolvedName.Substring(
                0,
                resolvedName.Length - cloneSuffix.Length).TrimEnd();
        }

        return resolvedName;
    }

    private static string FormatObservedIntRange(int minimum, int maximum)
    {
        if (minimum == int.MaxValue)
        {
            return "NotObserved";
        }

        return minimum == maximum
            ? minimum.ToString(CultureInfo.InvariantCulture)
            : minimum.ToString(CultureInfo.InvariantCulture) + "-" +
              maximum.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatObservedFloatRange(float minimum, float maximum)
    {
        if (float.IsPositiveInfinity(minimum))
        {
            return "NotObserved";
        }

        return Mathf.Approximately(minimum, maximum)
            ? FormatFloat(minimum)
            : FormatFloat(minimum) + "-" + FormatFloat(maximum);
    }

    private static string FormatPercent(float value)
    {
        return (value * 100f).ToString("0.##", CultureInfo.InvariantCulture) +
               "%";
    }

    private static string FormatSeconds(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture) + "s";
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
#endif
