#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Runtime References")]
    [SerializeField] private BattleRuntimeCoordinator battleRuntimeCoordinator;
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
    private readonly List<CombatBalanceDraftItemJson> towerDraftPoolSnapshot =
        new List<CombatBalanceDraftItemJson>();
    private readonly List<CombatBalanceDraftItemJson>
        towerUpgradeDraftPoolSnapshot =
            new List<CombatBalanceDraftItemJson>();
    private readonly List<PendingLevelUpObservation> pendingLevelUps =
        new List<PendingLevelUpObservation>();
    private readonly ElementalBuffRunAccumulator buffAccumulator =
        new ElementalBuffRunAccumulator();
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

    private sealed class PendingLevelUpObservation
    {
        public int PlayerLevel { get; set; }
        public int CurrentProgress { get; set; }
        public int RequiredProgress { get; set; }
        public float ActiveTimeSeconds { get; set; }
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
        public int EligibleOpeningProjectilesReleased { get; set; }
        public int EligibleOpeningProjectileDirectHits { get; set; }
        public int EligibleOpeningProjectilesEndedWithoutImpact { get; set; }
        public int LaterProjectilesReleased { get; set; }
        public int LaterProjectileDirectHits { get; set; }
        public int DirectElementalOpportunities { get; set; }
        public int BlastTargetElementalOpportunities { get; set; }
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
        public int FinalDiveDirectElementalOpportunities { get; set; }
        public int FinalDiveBlastTargetElementalOpportunities { get; set; }
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
        }

        if (towerPlacementController != null)
        {
            towerPlacementController.OnTowerDeploymentCommitted +=
                HandleTowerDeploymentCommitted;
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
        TowerRuntimeStatResolver.OnTowerOwnedDamageResolutionObserved +=
            HandleTowerOwnedDamageResolutionObserved;
        TowerRuntimeStatResolver.OnTowerOwnedDamageApplicationObserved +=
            HandleTowerOwnedDamageApplicationObserved;
        EffectExecutor.OnFixedBuffDamageObserved +=
            HandleFixedBuffDamageObserved;

        isSubscribed = true;
    }

    private void WarnAboutMissingReferences()
    {
        List<string> missingReferences = new List<string>();

        if (battleRuntimeCoordinator == null)
        {
            missingReferences.Add(nameof(battleRuntimeCoordinator));
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
        }

        if (towerPlacementController != null)
        {
            towerPlacementController.OnTowerDeploymentCommitted -=
                HandleTowerDeploymentCommitted;
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
        TowerRuntimeStatResolver.OnTowerOwnedDamageResolutionObserved -=
            HandleTowerOwnedDamageResolutionObserved;
        TowerRuntimeStatResolver.OnTowerOwnedDamageApplicationObserved -=
            HandleTowerOwnedDamageApplicationObserved;
        EffectExecutor.OnFixedBuffDamageObserved -=
            HandleFixedBuffDamageObserved;

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
            ActiveTimeSeconds = GetRunActiveTimeSeconds()
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
                observation.ActiveTimeSeconds));
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
                resolvedMonsterCount = resolvedCount,
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
            attempt.selectedChoice = CreateDraftItemJson(
                observation.SelectedChoice,
                ResolveDisplayedMultiplicity(
                    attempt.displayedChoices,
                    observation.SelectedChoice));
            return;
        }
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
        float activeTimeSeconds)
    {
        return new CombatBalanceProgressionEventJson
        {
            ordinal = progressionEvents.Count + 1,
            kind = kind,
            resolvedMonsterCount = resolvedCount,
            playerLevel = playerLevel,
            currentProgress = currentProgress,
            requiredProgress = requiredProgress,
            activeTimeSeconds = activeTimeSeconds,
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
        };
    }

    private void HandleSpawningStarted()
    {
        if (!isTrackingRun)
        {
            BeginRun();
            return;
        }

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
        towerDraftPoolSnapshot.Clear();
        towerUpgradeDraftPoolSnapshot.Clear();
        pendingLevelUps.Clear();
        buffAccumulator.Reset();
        projectileRuntimeByTowerInstanceId.Clear();
        droneRuntimeByTowerInstanceId.Clear();
        towerDeploymentByInstanceId.Clear();
        towerScaledDamageBySignature.Clear();
        towerScaledRejectionBySignature.Clear();
        fixedBuffDamageBySignature.Clear();
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
        return string.Join(
            "|",
            GetEffectIdentity(observation.EffectDefinition),
            observation.ActionOrdinal.ToString(CultureInfo.InvariantCulture),
            observation.FixedDamage.ToString(CultureInfo.InvariantCulture));
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

        int towerInstanceId = observation.SourceTower.GetInstanceID();

        if (!droneRuntimeByTowerInstanceId.TryGetValue(
                towerInstanceId,
                out Dictionary<int, DroneRuntimeAggregate> drones))
        {
            drones = new Dictionary<int, DroneRuntimeAggregate>();
            droneRuntimeByTowerInstanceId.Add(towerInstanceId, drones);
        }

        if (!drones.TryGetValue(
                observation.SourceDroneInstanceId,
                out DroneRuntimeAggregate drone))
        {
            drone = new DroneRuntimeAggregate(
                observation.SourceDroneInstanceId,
                observation.IsAdditionalDrone);
            drones.Add(observation.SourceDroneInstanceId, drone);
        }
        else if (drone.IsAdditionalAttackEntity != observation.IsAdditionalDrone)
        {
            drone.DiagnosticsConsistent = false;
        }

        switch (observation.ObservationType)
        {
            case DroneBurstRuntimeObservationType.FinalDiveDirectElementalOpportunity:
                if (observation.BurstId != 0)
                {
                    drone.DiagnosticsConsistent = false;
                }

                drone.FinalDiveDirectElementalOpportunities +=
                    observation.Count;
                return;
            case DroneBurstRuntimeObservationType.FinalDiveBlastTargetElementalOpportunity:
                if (observation.BurstId != 0)
                {
                    drone.DiagnosticsConsistent = false;
                }

                drone.FinalDiveBlastTargetElementalOpportunities +=
                    observation.Count;
                return;
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
                if (observation.IsBurstOpenerEligible)
                {
                    burst.EligibleOpeningProjectilesReleased +=
                        observation.Count;
                }
                else
                {
                    burst.LaterProjectilesReleased += observation.Count;
                }

                break;
            case DroneBurstRuntimeObservationType.ProjectileDirectHit:
                if (observation.IsBurstOpenerEligible)
                {
                    burst.EligibleOpeningProjectileDirectHits +=
                        observation.Count;
                }
                else
                {
                    burst.LaterProjectileDirectHits += observation.Count;
                }

                break;
            case DroneBurstRuntimeObservationType.ProjectileEndedWithoutImpact:
                if (observation.IsBurstOpenerEligible)
                {
                    burst.EligibleOpeningProjectilesEndedWithoutImpact +=
                        observation.Count;
                }

                break;
            case DroneBurstRuntimeObservationType.DirectElementalOpportunity:
                if (!observation.IsBurstOpenerEligible)
                {
                    drone.DiagnosticsConsistent = false;
                }

                burst.DirectElementalOpportunities += observation.Count;
                break;
            case DroneBurstRuntimeObservationType.BlastTargetElementalOpportunity:
                if (!observation.IsBurstOpenerEligible)
                {
                    drone.DiagnosticsConsistent = false;
                }

                burst.BlastTargetElementalOpportunities += observation.Count;
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

        report.fixture.waveConfigName = waveConfigName ?? string.Empty;
        report.fixture.expectedWaveCountAvailable = hasExpectedWaveCount;
        report.fixture.expectedWaveCount = expectedWaveCount;
        report.fixture.expectedMonsterCountAvailable =
            hasExpectedMonsterCount;
        report.fixture.expectedMonsterCount = expectedMonsterCount;
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

        report.monsterRuntime = CreateMonsterRuntimeJson();
        report.damageDiagnostics = CreateDamageDiagnosticsJson();
        report.towers = CreateTowerJsonRecords();
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
        report.integrity.initialDraftCountMatches =
            initialDraftCompletionCount == 1;
        report.integrity.levelUpCountMatches =
            report.progression.observedLevelUpCount ==
            report.progression.expectedLevelUpCount;
        report.integrity.levelUpResolutionNodesMatch =
            LevelUpResolutionNodesMatch(report.progression);
        report.integrity.finalPlayerLevelMatches =
            report.progression.finalLevel ==
            report.progression.expectedLevelUpCount + 1;
        report.integrity.postFinalDraftCombatObserved =
            report.progression.resolutionsAfterFinalDraft > 0;
        report.integrity.waveEventCountsMatch =
            !hasExpectedWaveCount ||
            (report.waveRuntime.observedWaveStartCount == expectedWaveCount &&
             report.waveRuntime.observedWaveCompletionCount ==
             expectedWaveCount);
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
        report.integrity.damageDiagnosticsCountsMatch =
            DamageDiagnosticsCountsMatch(report.damageDiagnostics);
        report.integrity.droneBurstDiagnosticsConsistent =
            DroneBurstDiagnosticsAreConsistent();
        report.buffs = buffAccumulator.CreateJsonRecords();
        report.integrity.buffDiagnosticsConsistent =
            buffAccumulator.DiagnosticsConsistent;
        report.integrity.buffWaveAttributionMatches =
            buffAccumulator.WaveAttributionMatches;
        return report;
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
            progression.observedLevelUpCount !=
            progression.expectedLevelUpCount)
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

            expectedResolutionNode += progression.requirements[levelUpIndex];

            if (progressionEvent.resolvedMonsterCount !=
                expectedResolutionNode)
            {
                return false;
            }

            levelUpIndex++;
        }

        return levelUpIndex == progression.expectedLevelUpCount;
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
            x = value.x,
            y = value.y,
            z = value.z
        };
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
                                : string.Empty
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
                        drone.IsAdditionalAttackEntity,
                    finalDiveDirectElementalOpportunities =
                        drone.FinalDiveDirectElementalOpportunities,
                    finalDiveBlastTargetElementalOpportunities =
                        drone.FinalDiveBlastTargetElementalOpportunities
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
                droneJson.eligibleOpeningProjectilesReleased +=
                    burst.EligibleOpeningProjectilesReleased;
                droneJson.eligibleOpeningProjectileDirectHits +=
                    burst.EligibleOpeningProjectileDirectHits;
                droneJson.eligibleOpeningProjectilesEndedWithoutImpact +=
                    burst.EligibleOpeningProjectilesEndedWithoutImpact;
                droneJson.laterProjectilesReleased +=
                    burst.LaterProjectilesReleased;
                droneJson.laterProjectileDirectHits +=
                    burst.LaterProjectileDirectHits;
                droneJson.directElementalOpportunities +=
                    burst.DirectElementalOpportunities;
                droneJson.blastTargetElementalOpportunities +=
                    burst.BlastTargetElementalOpportunities;
                droneJson.bursts.Add(new CombatBalanceDroneBurstJson
                {
                    burstId = burst.BurstId,
                    started = burst.Started,
                    eligibleOpeningProjectilesReleased =
                        burst.EligibleOpeningProjectilesReleased,
                    eligibleOpeningProjectileDirectHits =
                        burst.EligibleOpeningProjectileDirectHits,
                    eligibleOpeningProjectilesEndedWithoutImpact =
                        burst.EligibleOpeningProjectilesEndedWithoutImpact,
                    laterProjectilesReleased =
                        burst.LaterProjectilesReleased,
                    laterProjectileDirectHits =
                        burst.LaterProjectileDirectHits,
                    directElementalOpportunities =
                        burst.DirectElementalOpportunities,
                    blastTargetElementalOpportunities =
                        burst.BlastTargetElementalOpportunities
                });
            }

            result.diagnosticsConsistent &=
                DroneRuntimeDiagnosticsAreConsistent(drone);
            result.drones.Add(droneJson);
        }

        return result;
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
                burst.EligibleOpeningProjectilesReleased > 1 ||
                burst.EligibleOpeningProjectileDirectHits +
                burst.EligibleOpeningProjectilesEndedWithoutImpact >
                burst.EligibleOpeningProjectilesReleased ||
                burst.DirectElementalOpportunities >
                burst.EligibleOpeningProjectileDirectHits ||
                (burst.BlastTargetElementalOpportunities > 0 &&
                 burst.EligibleOpeningProjectileDirectHits <= 0))
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
