#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using static CombatReportIntegrity;
using UnityEngine.Serialization;

internal sealed class CombatRecordingSession
{
    internal enum SpawnOrdinalRelation
    {
        Unknown = 0,
        ImmediateLaterSpawn = 1,
        ImmediateEarlierSpawn = 2,
        NonAdjacent = 3
    }

    internal readonly CombatRouteAccumulator routeAccumulator = new CombatRouteAccumulator();
    internal readonly CombatEntityAccumulator entityAccumulator;
    internal readonly CombatDamageAccumulator damageAccumulator = new CombatDamageAccumulator();
    internal readonly CombatDraftAccumulator draftAccumulator = new CombatDraftAccumulator();
    internal readonly CombatBalanceRunRecorder owner;
    internal string runName;
    internal PlacementRouteForcedRelocationExpectation placementRouteForcedRelocationExpectation;
    internal BattleRuntimeCoordinator battleRuntimeCoordinator;
    internal StageCompositionController stageCompositionController;
    internal MonsterSpawner monsterSpawner;
    internal MonsterManager monsterManager;
    internal PlayerSystem playerSystem;
    internal DraftSystem draftSystem;
    internal BattleHUDUI battleHUDUI;
    internal TowerPlacementSubmission subscribedSubmission;
    internal readonly object identity;
    internal CombatDiagnosticScope.Lease lease;
    internal bool terminalCaptured;
    internal List<CombatBalanceTowerJson> terminalTowers;
    internal int terminalAlive;
    internal int terminalHealth, terminalMaxHealth, terminalLevel, terminalProgress, terminalRequired;
    internal bool disposed;
    internal float frozenTime;
    internal float ObservedTime => IsFrozen ? frozenTime : Time.time;
    internal bool AcceptEvent => !disposed && !IsFrozen && ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity ?? battleRuntimeCoordinator.DiagnosticIdentity);
    internal bool IsFrozen { get; private set; }

    internal CombatRecordingSession(CombatBalanceRunRecorder owner, BattleRuntimeCoordinator battle,
        StageCompositionController stage, string name, PlacementRouteForcedRelocationExpectation expectation)
    {
        entityAccumulator = new CombatEntityAccumulator(GetSpawnOrdinal);
        this.owner = owner; battleRuntimeCoordinator = battle; stageCompositionController = stage;
        runName = name; placementRouteForcedRelocationExpectation = expectation;
        monsterManager = battle.DiagnosticMonsters; monsterSpawner = battle.DiagnosticSpawner;
        playerSystem = battle.DiagnosticPlayer; draftSystem = battle.DiagnosticDraft;
        identity = battle.DiagnosticIdentity;
        lease = CombatDiagnosticScope.Acquire(identity);
        if (lease == null) throw new InvalidOperationException("This Battle already has a recording session or has no identity.");
        lease.Drained = Finish;
        try { BeginRun(); SubscribeToRuntime(); }
        catch { UnsubscribeFromRuntime(); UnsubscribeFromTrackedMonsters(); lease.Dispose(); throw; }
    }
    internal void Tick()
    {
        if (disposed || terminalCaptured) return;
        try
        {
            TrackCurrentMonsters();
            if (monsterManager != null) peakAliveCount = Mathf.Max(peakAliveCount, monsterManager.AliveMonsterCount);
        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }
    internal void Cancel()
    {
        if (disposed) return;
        lease.Closing = true;
        if (lease.Depth == 0) Finish();
    }
    internal void Finish()
    {
        if (disposed) return;
        try
        {
            if (hasPendingFinalSummary && !lease.Failed) FlushPendingFinalSummary();
            else if (lease.Failed) Debug.LogError("Combat recording failed during capture; no acceptance report was exported.", owner);
        }
        finally
        {
            disposed = true; isTrackingRun = false;
            UnsubscribeFromRuntime(); UnsubscribeFromTrackedMonsters(); lease.Dispose();
        }
    }
    internal void ManualSnapshot() { if (!disposed) Debug.Log(BuildSummary("Manual Snapshot", null, DateTime.Now), owner); }

    internal readonly Dictionary<MonsterBehaviour, MonsterObservation>
        trackedMonsters =
            new Dictionary<MonsterBehaviour, MonsterObservation>();
    internal readonly List<MonsterObservation> monsterObservations =
        new List<MonsterObservation>();
    internal readonly HashSet<int> seenMonsterInstanceIds = new HashSet<int>();
    internal readonly List<int> progressionRequirementsSnapshot =
        new List<int>();
    internal readonly List<CombatBalanceProgressionEventJson>
        progressionEvents =
            new List<CombatBalanceProgressionEventJson>();
    internal readonly List<CombatBalanceWaveEventJson> waveEvents =
        new List<CombatBalanceWaveEventJson>();

    internal readonly List<CombatBalanceDraftItemJson> towerDraftPoolSnapshot =
        new List<CombatBalanceDraftItemJson>();
    internal readonly List<CombatBalanceDraftItemJson>
        towerUpgradeDraftPoolSnapshot =
            new List<CombatBalanceDraftItemJson>();
    internal readonly List<PendingLevelUpObservation> pendingLevelUps =
        new List<PendingLevelUpObservation>();
    internal readonly ElementalBuffRunAccumulator buffAccumulator =
        new ElementalBuffRunAccumulator();
    internal readonly ElementalHitReactionRunAccumulator
        elementalHitReactionAccumulator =
            new ElementalHitReactionRunAccumulator();

    internal readonly Dictionary<int, TowerDeploymentObservation>
        towerDeploymentByInstanceId =
            new Dictionary<int, TowerDeploymentObservation>();

    internal readonly Dictionary<string, TowerWaveDamageAggregate>
        towerWaveDamageByScope =
            new Dictionary<string, TowerWaveDamageAggregate>();

    internal readonly Dictionary<string, ElementalOpportunityAggregate>
        elementalOpportunityByScope =
            new Dictionary<string, ElementalOpportunityAggregate>();
    internal readonly Dictionary<string, int>
        elementalBuffApplicationAttemptsBySourceWave =
            new Dictionary<string, int>();

    internal bool isSubscribed;
    internal bool isTrackingRun;
    internal bool hasLoggedFinalSummary;
    internal bool hasPendingFinalSummary;
    internal bool hasObservedFirstSpawn;
    internal bool hasObservedSpawningCompletion;
    internal string pendingTerminalState;
    internal string pendingFailureReason;
    internal int spawnedCount;
    internal int resolvedCount;
    internal int killedCount;
    internal int leakedCount;
    internal int peakAliveCount;
    internal int effectiveDamage;
    internal int leakedRemainingHealth;
    internal int observedTotalMonsterMaxHealth;
    internal int initialPlayerHealth;
    internal int observedMinimumMonsterHealth;
    internal int observedMaximumMonsterHealth;
    internal int expectedMonsterCount;
    internal bool hasExpectedMonsterCount;
    internal int expectedWaveCount;
    internal bool hasExpectedWaveCount;
    internal string waveConfigName;
    internal float observedMinimumMonsterSpeed;
    internal float observedMaximumMonsterSpeed;
    internal float firstSpawnTime;
    internal float lastSpawnTime;
    internal float lastResolutionTime;
    internal float spawningCompletedTime;
    internal float observedSpawnIntervalTotal;
    internal int observedSpawnIntervalCount;
    internal float runStartedAtTime;
    internal int initialDraftCompletionCount;
    internal string configuredDraftGenerationMode;
    internal int configuredFixedDraftStepCount;
    internal int configuredDraftChoiceCount;
    internal float configuredTowerDraftSlotProbability;
    internal int configuredDraftSeed;
    internal bool configuredFixedDraftSeedEnabled;
    internal string configuredGenerationContractVersion;
    internal string configuredDraftRandomAlgorithmVersion;
    internal CombatBalanceFixtureJson fixtureSnapshot =
        new CombatBalanceFixtureJson();

    internal sealed class PendingLevelUpObservation
    {
        public int PlayerLevel { get; set; }
        public int CurrentProgress { get; set; }
        public int RequiredProgress { get; set; }
        public float ActiveTimeSeconds { get; set; }
        public int TriggerResolutionNode { get; set; }
    }

    internal sealed class TowerWaveDamageAggregate
    {
        public int TowerInstanceId { get; set; }
        public string TowerDisplayName { get; set; }
        public string TowerFamily { get; set; }
        public int WaveNumber { get; set; }
        public int SuccessfulDamageApplications { get; set; }
        public int EffectiveTowerScaledDamage { get; set; }
        public int KillingBlows { get; set; }
    }

    internal sealed class TowerDeploymentObservation
    {
        public int DeploymentOrdinal { get; set; }
        public float DeployedAtSeconds { get; set; }
        public Vector3 WorldPosition { get; set; }
        public List<Vector2Int> GridPositions { get; } =
            new List<Vector2Int>();
    }

    internal sealed class ElementalOpportunityAggregate
    {
        public ElementalOpportunityAggregate(
            ElementalOpportunityObservation observation,
            int waveNumber)
        {
            Source = CombatDiagnosticScope.Source(observation.SourceTower);
            UpgradeName = observation.ElementalUpgrade != null ? observation.ElementalUpgrade.name : string.Empty;
            SourceWaveKey = CreateElementalSourceWaveKey(observation.SourceTower, observation.ElementalUpgrade, waveNumber);
            ElementalUpgrade = observation.ElementalUpgrade;
            WaveNumber = waveNumber;
            Diagnostics = observation.Diagnostics;
            MinimumResultOrdinal = observation.Diagnostics.ResultOrdinal;
            MaximumResultOrdinal = observation.Diagnostics.ResultOrdinal;
        }

        public CombatDiagnosticSource Source { get; }
        public string UpgradeName { get; }
        public string SourceWaveKey { get; }
        public TowerUpgradeDefinition ElementalUpgrade { get; }
        public int WaveNumber { get; }
        public ElementalOpportunityDiagnosticContext Diagnostics { get; }
        public int MinimumResultOrdinal { get; set; }
        public int MaximumResultOrdinal { get; set; }
        public int CandidateResults { get; set; }
        public int EligibleResults { get; set; }
        public int DispatchedRequests { get; set; }
    }

    internal sealed class MonsterObservation
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

    internal sealed class WaveResolutionAggregate
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

    internal sealed class MonsterTypeAggregate
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

    internal void SubscribeToRuntime()
    {
        if (isSubscribed)
        {
            return;
        }

        if (playerSystem != null)
        {

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

        if (subscribedSubmission == null && battleRuntimeCoordinator != null)
        {
            subscribedSubmission = battleRuntimeCoordinator.Submission;
            subscribedSubmission.OnTowerDeploymentCommitted +=
                HandleTowerDeploymentCommitted;
            subscribedSubmission.OnInvestmentEvidenceCommitted +=
                HandleTowerInvestmentCommitted;
            subscribedSubmission.OnPlacementRouteRevisionCommitted +=
                HandlePlacementRouteRevisionCommitted;
        }

        if (battleRuntimeCoordinator != null)
        {
            battleRuntimeCoordinator.OnBeforeBattleCleanup += CaptureTerminalPendingDraftSnapshot;
            battleRuntimeCoordinator.OnBattleResultEvidence +=
                HandleBattleResultPublished;
            battleRuntimeCoordinator.OnBattleFailureEvidence +=
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

    internal void WarnAboutMissingReferences()
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

        if (missingReferences.Count > 0)
        {
            Debug.LogWarning(
                "Combat balance run recorder is missing runtime references: " +
                string.Join(", ", missingReferences) + ".",
                owner);
        }
    }

    internal void UnsubscribeFromRuntime()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (playerSystem != null)
        {

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

        if (subscribedSubmission != null)
        {
            subscribedSubmission.OnTowerDeploymentCommitted -=
                HandleTowerDeploymentCommitted;
            subscribedSubmission.OnInvestmentEvidenceCommitted -=
                HandleTowerInvestmentCommitted;
            subscribedSubmission.OnPlacementRouteRevisionCommitted -=
                HandlePlacementRouteRevisionCommitted;
            subscribedSubmission = null;
        }

        if (battleRuntimeCoordinator != null)
        {
            battleRuntimeCoordinator.OnBeforeBattleCleanup -= CaptureTerminalPendingDraftSnapshot;
            battleRuntimeCoordinator.OnBattleResultEvidence -=
                HandleBattleResultPublished;
            battleRuntimeCoordinator.OnBattleFailureEvidence -=
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

    internal void HandleInitialDraftCompleted(DraftAttemptToken attemptToken)
    {
        if (!AcceptEvent) return;
        try
        {
            if (!isTrackingRun || !attemptToken.IsValid)
            {
                return;
            }

            initialDraftCompletionCount++;
            progressionEvents.Add(CreateProgressionEvent(
                "Initial",
                (terminalCaptured || playerSystem != null) ? (terminalCaptured ? terminalLevel : playerSystem.CurrentLevel) : 0,
                (terminalCaptured || playerSystem != null) ? (terminalCaptured ? terminalProgress : playerSystem.CurrentProgress) : 0,
                (terminalCaptured || playerSystem != null) ? (terminalCaptured ? terminalRequired : playerSystem.RequiredProgress) : 0,
                GetRunActiveTimeSeconds()));

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandlePlayerLevelUp(int playerLevel)
    {
        if (!AcceptEvent) return;
        try
        {
            if (!isTrackingRun)
            {
                return;
            }

            pendingLevelUps.Add(new PendingLevelUpObservation
            {
                PlayerLevel = playerLevel,
                CurrentProgress = (terminalCaptured || playerSystem != null)
                    ? (terminalCaptured ? terminalProgress : playerSystem.CurrentProgress)
                    : 0,
                RequiredProgress = (terminalCaptured || playerSystem != null)
                    ? (terminalCaptured ? terminalRequired : playerSystem.RequiredProgress)
                    : 0,
                ActiveTimeSeconds = GetRunActiveTimeSeconds(),
                TriggerResolutionNode = resolvedCount + 1
            });

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void FlushPendingProgressionEvents()
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

    internal void CaptureDraftFixture()
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
        configuredDraftChoiceCount = draftSystem.ConfiguredDraftChoiceCount;
        configuredTowerDraftSlotProbability =
            draftSystem.BoundTowerDraftSlotProbability;
        configuredDraftSeed = draftSystem.ActiveDraftSeed;
        configuredFixedDraftSeedEnabled = draftSystem.UseFixedDraftSeed;
        configuredGenerationContractVersion =
            DraftSystem.GenerationContractVersion;
        configuredDraftRandomAlgorithmVersion =
            DraftSystem.DraftRandomAlgorithmVersion;

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

    internal void HandleDraftChoicesOpened(
        DraftChoicesOpenedObservation observation)
    {
        if (!AcceptEvent) return;
        try
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

            configuredDraftChoiceCount = observation.ConfiguredChoiceCount;
            configuredTowerDraftSlotProbability =
                observation.TowerDraftSlotProbability;
            configuredDraftSeed = observation.DraftSeed;
            configuredGenerationContractVersion =
                observation.GenerationContractVersion;
            configuredDraftRandomAlgorithmVersion =
                observation.DraftRandomAlgorithmVersion;

            CombatBalanceDraftAttemptJson attempt =
                new CombatBalanceDraftAttemptJson
                {
                    attemptToken = observation.AttemptToken.ToString(),
                    ordinal = observation.DraftOrdinal,
                    sessionKind = observation.SessionKind.ToString(),
                    generationMode = observation.GenerationMode.ToString(),
                    resolvedMonsterCount = ResolveDraftResolutionNode(
                        observation),
                    playerLevel = (terminalCaptured || playerSystem != null)
                        ? (terminalCaptured ? terminalLevel : playerSystem.CurrentLevel)
                        : 0,
                    currentProgress = (terminalCaptured || playerSystem != null)
                        ? (terminalCaptured ? terminalProgress : playerSystem.CurrentProgress)
                        : 0,
                    requiredProgress = (terminalCaptured || playerSystem != null)
                        ? (terminalCaptured ? terminalRequired : playerSystem.RequiredProgress)
                        : 0,
                    activeTimeSeconds = GetRunActiveTimeSeconds(),
                    requestedTowerCount = observation.RequestedTowerCount,
                    requestedUpgradeCount = observation.RequestedUpgradeCount,
                    availableDistinctTowerCount =
                        observation.AvailableDistinctTowerCount,
                    availableDistinctUpgradeCount =
                        observation.AvailableDistinctUpgradeCount,
                    realizedTowerCount = observation.RealizedTowerCount,
                    realizedUpgradeCount = observation.RealizedUpgradeCount,
                    towerSlotsBackfilledByUpgrade =
                        observation.TowerSlotsBackfilledByUpgrade,
                    upgradeSlotsBackfilledByTower =
                        observation.UpgradeSlotsBackfilledByTower,
                    backfillReason = observation.BackfillReason
                };

            for (int i = 0; i < observation.RequestedCategories.Count; i++)
            {
                attempt.requestedCategories.Add(
                    observation.RequestedCategories[i].ToString());
            }

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

            draftAccumulator.draftAttempts.Add(attempt);

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleDraftChoiceCommitted(
        DraftChoiceCommittedObservation observation)
    {
        if (!AcceptEvent) return;
        try
        {
            if (!isTrackingRun ||
                observation == null ||
                observation.SelectedChoice == null)
            {
                return;
            }

            string attemptToken = observation.AttemptToken.ToString();

            for (int i = draftAccumulator.draftAttempts.Count - 1; i >= 0; i--)
            {
                CombatBalanceDraftAttemptJson attempt = draftAccumulator.draftAttempts[i];

                if (!string.Equals(
                        attempt.attemptToken,
                        attemptToken,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                attempt.selectionAttempted = true;
                attempt.heldItemCreationSucceeded =
                    observation.HeldItemCreationSucceeded;
                attempt.heldItemCreationFailureReason = observation.FailureReason;
                attempt.selectionCommitted =
                    observation.HeldItemCreationSucceeded;
                attempt.selectionCommittedAtSeconds =
                    observation.HeldItemCreationSucceeded
                        ? GetRunActiveTimeSeconds()
                        : 0f;
                attempt.selectedChoice = CreateDraftItemJson(
                    observation.SelectedChoice,
                    ResolveDisplayedMultiplicity(
                        attempt.displayedChoices,
                        observation.SelectedChoice));
                return;
            }

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal int ResolveDraftResolutionNode(
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
                pending.PlayerLevel == (terminalCaptured ? terminalLevel : playerSystem.CurrentLevel))
            {
                return pending.TriggerResolutionNode;
            }
        }

        return resolvedCount;
    }

    internal static int ResolveNaturalMultiplicity(
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

    internal static int ResolveDisplayedMultiplicity(
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

    internal static CombatBalanceDraftItemJson CreateDraftItemJson(
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

    internal CombatBalanceProgressionEventJson CreateProgressionEvent(
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
            alive = terminalCaptured ? terminalAlive : monsterManager != null
                ? monsterManager.AliveMonsterCount
                : Mathf.Max(0, spawnedCount - resolvedCount),
            playerHealth = (terminalCaptured || playerSystem != null)
                ? (terminalCaptured ? terminalHealth : playerSystem.CurrentHealth)
                : 0
        };
    }

    internal void HandleSpawningStarted()
    {
        if (!AcceptEvent) return;
        try
        {
            if (!isTrackingRun)
            {
                BeginRun();
                return;
            }

            CaptureStageFixture();
            CaptureExpectedMonsterFixture();

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void BeginRun()
    {
        UnsubscribeFromTrackedMonsters();
        trackedMonsters.Clear();
        monsterObservations.Clear();
        seenMonsterInstanceIds.Clear();
        progressionRequirementsSnapshot.Clear();
        progressionEvents.Clear();
        waveEvents.Clear();
        draftAccumulator.draftAttempts.Clear();
        hasCapturedTerminalPendingDraftSnapshot = false;
        draftAccumulator.terminalPendingDraftSnapshot.Clear();
        draftAccumulator.investmentCommits.Clear();
        towerDraftPoolSnapshot.Clear();
        towerUpgradeDraftPoolSnapshot.Clear();
        pendingLevelUps.Clear();
        buffAccumulator.Reset();
        elementalHitReactionAccumulator.Reset();
        entityAccumulator.projectileRuntimeByTowerInstanceId.Clear();
        entityAccumulator.droneRuntimeByTowerInstanceId.Clear();
        towerDeploymentByInstanceId.Clear();
        damageAccumulator.towerScaledDamageBySignature.Clear();
        damageAccumulator.towerScaledRejectionBySignature.Clear();
        damageAccumulator.fixedBuffDamageBySignature.Clear();
        towerWaveDamageByScope.Clear();
        routeAccumulator.towerRouteDamageCoverageByTowerInstanceId.Clear();
        elementalOpportunityByScope.Clear();
        elementalBuffApplicationAttemptsBySourceWave.Clear();
        routeAccumulator.placementRouteCommits.Clear();
        routeAccumulator.placementRouteLifecycle.Clear();
        ResetExpectedMonsterFixture();
        spawnedCount = 0;
        resolvedCount = 0;
        killedCount = 0;
        leakedCount = 0;
        peakAliveCount = 0;
        effectiveDamage = 0;
        leakedRemainingHealth = 0;
        observedTotalMonsterMaxHealth = 0;
        initialPlayerHealth = (terminalCaptured || playerSystem != null)
            ? (terminalCaptured ? terminalHealth : playerSystem.CurrentHealth)
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
        configuredDraftChoiceCount = 0;
        configuredTowerDraftSlotProbability = 0f;
        configuredDraftSeed = 0;
        configuredFixedDraftSeedEnabled = false;
        configuredGenerationContractVersion = string.Empty;
        configuredDraftRandomAlgorithmVersion = string.Empty;
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

    internal void ResetExpectedMonsterFixture()
    {
        expectedMonsterCount = 0;
        hasExpectedMonsterCount = false;
        expectedWaveCount = 0;
        hasExpectedWaveCount = false;
        waveConfigName = monsterSpawner != null
            ? monsterSpawner.BoundWaveConfigName
            : string.Empty;
    }

    internal void CaptureStageFixture()
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

    internal void CaptureExpectedMonsterFixture()
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
                owner);
        }

        if (monsterSpawner != null &&
            monsterSpawner.TryGetExpectedWaveCount(
                out int resolvedExpectedWaveCount))
        {
            expectedWaveCount = resolvedExpectedWaveCount;
            hasExpectedWaveCount = true;
        }
    }

    internal void TrackCurrentMonsters()
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

    internal void TrackMonster(
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

    internal void HandleMonsterHealthChanged(
        MonsterBehaviour monster,
        int currentHealth,
        int _)
    {
        if (!AcceptEvent) return;
        try
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleMonsterRegistered(MonsterBehaviour monster)
    {
        if (!AcceptEvent) return;
        try
        {
            if (isTrackingRun)
            {
                TrackMonster(
                    monster,
                    observedThroughRegistrationEvent: true);
            }

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleTowerDeploymentCommitted(TowerInstance towerInstance)
    {
        if (!AcceptEvent) return;
        try
        {
            using (CombatDiagnosticScope.Enter(identity)) { CombatDiagnosticScope.Source(towerInstance); }
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandlePlacementRouteRevisionCommitted(
        TowerInstance towerInstance,
        TowerPlacementTopologyPlan topologyPlan,
        MonsterRouteRevisionBatch revisionBatch)
    {
        if (!AcceptEvent) return;
        try
        {
            if (!isTrackingRun || topologyPlan == null || revisionBatch == null)
            {
                return;
            }

            CombatBalancePlacementRouteCommitJson commit =
                new CombatBalancePlacementRouteCommitJson
                {
                    ordinal = routeAccumulator.placementRouteCommits.Count + 1,
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

            routeAccumulator.placementRouteCommits.Add(commit);

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandlePlacementRouteLifecycleObserved(
        MonsterPlacementRouteLifecycleObservation observation)
    {
        if (!AcceptEvent) return;
        try
        {
            if (!isTrackingRun)
            {
                return;
            }

            routeAccumulator.placementRouteLifecycle.Add(
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleTowerInvestmentCommitted(
        TowerInvestmentCommitObservation observation)
    {
        if (!AcceptEvent) return;
        try
        {
            if (!isTrackingRun ||
                !observation.DraftAttemptToken.IsValid)
            {
                return;
            }

            int draftOrdinal = draftAccumulator.ResolveDraftOrdinal(
                observation.DraftAttemptToken);
            draftAccumulator.investmentCommits.Add(new CombatBalanceInvestmentCommitJson
            {
                ordinal = draftAccumulator.investmentCommits.Count + 1,
                kind = observation.Kind.ToString(),
                authoritySource = draftOrdinal > 0
                    ? "Draft"
                    : "EditorDebug",
                draftAttemptToken = observation.DraftAttemptToken.ToString(),
                draftOrdinal = draftOrdinal,
                draftResultType = observation.DraftResultTypeName,
                draftAssetName = observation.DraftAssetName,
                towerInstanceId = observation.TowerInstanceId,
                towerDisplayName = observation.TowerDisplayName,
                towerFamily = observation.TowerFamilyName,
                previousLevel = observation.PreviousLevel,
                currentLevel = observation.CurrentLevel,
                upgradeLayer = observation.UpgradeLayerName,
                activeTimeSeconds = GetRunActiveTimeSeconds(),
                resolvedMonsterCount = resolvedCount,
                spawned = spawnedCount,
                resolved = resolvedCount,
                killed = killedCount,
                leaked = leakedCount,
                alive = terminalCaptured ? terminalAlive : monsterManager != null
                ? monsterManager.AliveMonsterCount
                    : Mathf.Max(0, spawnedCount - resolvedCount),
                playerHealth = (terminalCaptured || playerSystem != null)
                    ? (terminalCaptured ? terminalHealth : playerSystem.CurrentHealth)
                    : 0
            });

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal static int CompareGridPositions(
        Vector2Int left,
        Vector2Int right)
    {
        int xComparison = left.x.CompareTo(right.x);
        return xComparison != 0
            ? xComparison
            : left.y.CompareTo(right.y);
    }

    internal void HandleBuffRuntimeObserved(
        MonsterBehaviour monster,
        BuffRuntimeObservation observation)
    {
        if (!AcceptEvent) return;
        try
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleElementalOpportunityObserved(
        ElementalOpportunityObservation observation)
    {
        if (!AcceptEvent) return;
        try
        {
            if (!ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity)) return;
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleElementalHitReactionObserved(
        MonsterBehaviour monster,
        ElementalHitReactionObservation observation)
    {
        if (!AcceptEvent) return;
        try
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleTowerOwnedDamageResolutionObserved(TowerOwnedDamageResolutionObservation observation)
    {
        if (!AcceptEvent) return;
        if (!isTrackingRun || !ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity)) return;
        try { damageAccumulator.HandleTowerOwnedDamageResolutionObserved(observation); }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleTowerOwnedDamageApplicationObserved(TowerOwnedDamageApplicationObservation observation)
    {
        if (!AcceptEvent) return;
        if (!isTrackingRun || !ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity)) return;
        try { damageAccumulator.HandleTowerOwnedDamageApplicationObserved(observation); }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleTowerOwnedTargetDamageObserved(
        TowerOwnedTargetDamageObservation observation)
    {
        if (!AcceptEvent) return;
        try
        {
            if (!ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity)) return;
            if (!isTrackingRun ||
                ReferenceEquals(observation.Target, null) ||
                observation.AppliedDamage <= 0 ||
                observation.Resolution.DiagnosticSource.Id == 0)
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
            CombatDiagnosticSource tower = observation.Resolution.DiagnosticSource;
            string scopeKey = tower.Id.ToString(
                CultureInfo.InvariantCulture) + ":" +
                waveNumber.ToString(CultureInfo.InvariantCulture);

            if (!towerWaveDamageByScope.TryGetValue(
                    scopeKey,
                    out TowerWaveDamageAggregate aggregate))
            {
                aggregate = new TowerWaveDamageAggregate
                {
                    TowerInstanceId = tower.Id,
                    TowerDisplayName = tower.WaveDisplayName,
                    TowerFamily = tower.Family,
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void RecordTowerRouteDamageCoverage(
        TowerOwnedTargetDamageObservation observation,
        CombatDiagnosticSource tower)
    {
        int towerInstanceId = tower.Id;

        if (!routeAccumulator.towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
                towerInstanceId,
                out CombatRouteAccumulator.TowerRouteDamageCoverageAggregate aggregate))
        {
            aggregate = new CombatRouteAccumulator.TowerRouteDamageCoverageAggregate
            {
                TowerInstanceId = towerInstanceId,
                TowerDisplayName = tower.WaveDisplayName,
                TowerFamily = tower.Family
            };
            routeAccumulator.towerRouteDamageCoverageByTowerInstanceId.Add(
                towerInstanceId,
                aggregate);
        }

        var target = CombatDiagnosticScope.Target(observation.Target);
        int targetInstanceId = target.Id;
        aggregate.SuccessfulDamageApplications++;
        aggregate.EffectiveDamage += observation.AppliedDamage;
        aggregate.DamagedMonsterInstanceIds.Add(targetInstanceId);

        if (observation.KillingBlow)
        {
            aggregate.KillingBlows++;
        }

        if (!target.HasNode)
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

        Vector2Int gridPosition = target.Position;
        int placementCommitOrdinal = routeAccumulator.placementRouteCommits.Count;
        string cellKey = placementCommitOrdinal.ToString(
            CultureInfo.InvariantCulture) + ":" +
            gridPosition.x.ToString(CultureInfo.InvariantCulture) + ":" +
            gridPosition.y.ToString(CultureInfo.InvariantCulture);

        if (!aggregate.Cells.TryGetValue(
                cellKey,
                out CombatRouteAccumulator.TowerRouteDamageCellAggregate cell))
        {
            cell = new CombatRouteAccumulator.TowerRouteDamageCellAggregate
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

    internal void HandleFixedBuffDamageObserved(FixedBuffDamageObservation observation)
    {
        if (!AcceptEvent) return;
        if (!isTrackingRun || !ReferenceEquals(identity, observation.BattleIdentity)) return;
        try { damageAccumulator.HandleFixedBuffDamageObserved(observation); }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal static string CreateFixedBuffDamageSignature(
        FixedBuffDamageObservation observation)
    {
        return CreateFixedBuffDamageSignature(
            observation.EffectDefinition,
            observation.ActionOrdinal,
            observation.FixedDamage);
    }

    internal static string CreateFixedBuffDamageSignature(
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

    internal static string CreateElementalOpportunityScopeKey(
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

    internal static string CreateElementalSourceWaveKey(
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

    internal static string GetEffectIdentity(EffectDefinition effectDefinition)
    {
        return effectDefinition != null
            ? effectDefinition.GetInstanceID().ToString(
                CultureInfo.InvariantCulture)
            : "0";
    }

    internal void HandleProjectileRuntimeObserved(ProjectileRuntimeObservation observation)
    {
        if (!AcceptEvent) return;
        if (!isTrackingRun || !ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity)) return;
        try { entityAccumulator.HandleProjectileRuntimeObserved(observation); }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleDroneLifecycleRuntimeObserved(DroneLifecycleRuntimeObservation observation)
    {
        if (!AcceptEvent) return;
        if (!isTrackingRun || !ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity)) return;
        try { entityAccumulator.HandleDroneLifecycleRuntimeObserved(observation); }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleDroneBurstRuntimeObserved(DroneBurstRuntimeObservation observation)
    {
        if (!AcceptEvent) return;
        if (!isTrackingRun || !ReferenceEquals(identity, CombatDiagnosticScope.CurrentIdentity)) return;
        try { entityAccumulator.HandleDroneBurstRuntimeObserved(observation); }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal int GetSpawnOrdinal(MonsterBehaviour monster)
    {
        return monster != null &&
               trackedMonsters.TryGetValue(
                   monster,
                   out MonsterObservation observation)
            ? observation.SpawnOrdinal
            : 0;
    }

    internal void HandleMonsterResolved(
        MonsterBehaviour monster,
        bool reachedTarget)
    {
        if (!AcceptEvent) return;
        try
        {
            if (ReferenceEquals(monster, null))
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleMonsterDestroyed(MonsterBehaviour monster)
    {
        if (!AcceptEvent) return;
        try
        {
            UnsubscribeFromMonster(monster);

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleWaveSpawningStarted(
        int waveIndex,
        MonsterWaveEntry wave)
    {
        if (!AcceptEvent) return;
        try
        {
            CaptureWaveEvent("SpawnStarted", waveIndex, wave);

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleWaveSpawningCompleted(
        int waveIndex,
        MonsterWaveEntry wave)
    {
        if (!AcceptEvent) return;
        try
        {
            CaptureWaveEvent("SpawnCompleted", waveIndex, wave);

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleMonsterSpawnedFromWave(
        int waveIndex,
        int spawnIndex,
        MonsterBehaviour monster)
    {
        if (!AcceptEvent) return;
        try
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
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void CaptureWaveEvent(
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
            alive = terminalCaptured ? terminalAlive : monsterManager != null
                ? monsterManager.AliveMonsterCount
                : Mathf.Max(0, spawnedCount - resolvedCount),
            playerHealth = (terminalCaptured || playerSystem != null)
                ? (terminalCaptured ? terminalHealth : playerSystem.CurrentHealth)
                : 0
        });
    }

    internal void HandleAllSpawningCompleted()
    {
        if (!AcceptEvent) return;
        try
        {
            hasObservedSpawningCompletion = true;
            spawningCompletedTime = Time.time;

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleBattleResultPublished(BattleResult result)
    {
        if (!AcceptEvent) return;
        try
        {
            QueueFinalSummary(result.ToString(), null);

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void HandleBattleRuntimeFailed(string failureReason)
    {
        if (!AcceptEvent) return;
        try
        {
            QueueFinalSummary("TechnicalFailure", failureReason);

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal bool hasCapturedTerminalPendingDraftSnapshot;

    internal void CaptureTerminalPendingDraftSnapshot()
    {
        if (!AcceptEvent) return;
        try
        {
            lease.Closing = true;
            if (!terminalCaptured)
            {
                terminalAlive = monsterManager != null ? monsterManager.AliveMonsterCount : Mathf.Max(0, spawnedCount - resolvedCount);
                terminalTowers = CreateTowerJsonRecords();
                terminalHealth = playerSystem != null ? playerSystem.CurrentHealth : 0;
                terminalMaxHealth = playerSystem != null ? playerSystem.MaxHealth : 0;
                terminalLevel = playerSystem != null ? playerSystem.CurrentLevel : 0;
                terminalProgress = playerSystem != null ? playerSystem.CurrentProgress : 0;
                terminalRequired = playerSystem != null ? playerSystem.RequiredProgress : 0;
                terminalCaptured = true;
            }
            if (!isTrackingRun || hasCapturedTerminalPendingDraftSnapshot) return;
            hasCapturedTerminalPendingDraftSnapshot = true;
            draftAccumulator.terminalPendingDraftSnapshot.Clear();

            IReadOnlyList<PendingDraftEntry> pendingItems =
                draftSystem != null ? draftSystem.PendingDrafts : null;

            if (pendingItems == null)
            {
                return;
            }

            for (int i = 0; i < pendingItems.Count; i++)
            {
                PendingDraftEntry pendingItem = pendingItems[i];

                if (pendingItem == null ||
                    !pendingItem.DraftAttemptToken.IsValid ||
                    pendingItem.DraftResult == null)
                {
                    continue;
                }

                draftAccumulator.terminalPendingDraftSnapshot.Add(
                    new CombatBalancePendingDraftJson
                    {
                        draftAttemptToken =
                            pendingItem.DraftAttemptToken.ToString(),
                        item = CreateDraftItemJson(
                            pendingItem.DraftResult,
                            1)
                    });
            }

        }
        catch (Exception error) { CombatDiagnosticScope.Fail(identity, error); }
    }

    internal void QueueFinalSummary(string terminalState, string failureReason)
    {
        if (hasLoggedFinalSummary || hasPendingFinalSummary)
        {
            return;
        }

        pendingTerminalState = terminalState;
        pendingFailureReason = failureReason;
        hasPendingFinalSummary = true;
        lease.Closing = true;
    }

    internal void FlushPendingFinalSummary()
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

    internal void LogFinalSummary(string terminalState, string failureReason)
    {
        if (hasLoggedFinalSummary) return;
        FlushPendingProgressionEvents();
        hasLoggedFinalSummary = true;
        DateTime generatedAt = DateTime.Now;
        frozenTime = Time.time;
        IsFrozen = true;
        var report = new CombatReportBuilder(this).Build(terminalState, failureReason, generatedAt);
        string json = JsonUtility.ToJson(report, true);
        string path = GetReportPath(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Doc", "GamePlayRecord")), report.runLabel);
        CombatReportExporter.Enqueue(path, json, terminalState);
    }

    internal string BuildSummary(
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
            ? Mathf.Max(0, initialPlayerHealth - (terminalCaptured ? terminalHealth : playerSystem.CurrentHealth))
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
            .Append((terminalCaptured || playerSystem != null) ? (terminalCaptured ? terminalHealth : playerSystem.CurrentHealth) : 0)
            .Append('/')
            .Append((terminalCaptured || playerSystem != null) ? (terminalCaptured ? terminalMaxHealth : playerSystem.MaxHealth) : 0)
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

    internal bool ElementalHitReactionDamageReconciliationMatches()
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

            if (!damageAccumulator.fixedBuffDamageBySignature.TryGetValue(
                    signature,
                    out CombatDamageAccumulator.FixedBuffDamageAggregate aggregate) ||
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

    internal static string GetAssetName(UnityEngine.Object asset)
    {
        return asset != null ? asset.name : string.Empty;
    }

    internal CombatBalanceDraftRuntimeJson CreateDraftRuntimeJson()
    {
        CombatBalanceDraftRuntimeJson runtime =
            new CombatBalanceDraftRuntimeJson
            {
                configuredGenerationMode =
                    configuredDraftGenerationMode ?? string.Empty,
                configuredFixedStepCount = configuredFixedDraftStepCount,
                configuredChoiceCount = configuredDraftChoiceCount,
                towerDraftSlotProbability =
                    configuredTowerDraftSlotProbability,
                draftSeed = configuredDraftSeed,
                fixedDraftSeedEnabled = configuredFixedDraftSeedEnabled,
                generationContractVersion =
                    configuredGenerationContractVersion ?? string.Empty,
                draftRandomAlgorithmVersion =
                    configuredDraftRandomAlgorithmVersion ?? string.Empty,
                towerDraftPool =
                    new List<CombatBalanceDraftItemJson>(
                        towerDraftPoolSnapshot),
                towerUpgradeDraftPool =
                    new List<CombatBalanceDraftItemJson>(
                        towerUpgradeDraftPoolSnapshot),
                terminalPendingDrafts =
                    new List<CombatBalancePendingDraftJson>(
                        draftAccumulator.terminalPendingDraftSnapshot),
                attempts = new List<CombatBalanceDraftAttemptJson>(
                    draftAccumulator.draftAttempts)
            };
        runtime.observedAttemptCount = runtime.attempts.Count;

        for (int i = 0; i < runtime.attempts.Count; i++)
        {
            if (runtime.attempts[i].selectionCommitted)
            {
                runtime.committedSelectionCount++;
            }

            runtime.attempts[i].consumptionStatus =
                draftAccumulator.ResolveDraftConsumptionStatus(runtime.attempts[i]);
        }

        return runtime;
    }

    internal List<CombatBalanceTowerWaveJson> CreateTowerWaveSummaries()
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

    internal bool FixtureSnapshotIsComplete(
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

    internal static bool InvestmentCommitsMatchDraftSelections(
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

    internal static bool DraftGenerationTraceIsConsistent(
        CombatBalanceDraftRuntimeJson draftRuntime)
    {
        if (draftRuntime == null || draftRuntime.configuredChoiceCount <= 0)
        {
            return false;
        }

        for (int i = 0; i < draftRuntime.attempts.Count; i++)
        {
            CombatBalanceDraftAttemptJson attempt = draftRuntime.attempts[i];

            if (attempt == null ||
                attempt.displayedChoices == null ||
                attempt.realizedTowerCount + attempt.realizedUpgradeCount !=
                    attempt.displayedChoices.Count ||
                attempt.displayedChoices.Count >
                    draftRuntime.configuredChoiceCount)
            {
                return false;
            }

            bool isNaturalLevelUp = string.Equals(
                    attempt.generationMode,
                    DraftChoiceGenerationMode.Natural.ToString(),
                    StringComparison.Ordinal) &&
                string.Equals(
                    attempt.sessionKind,
                    DraftChoiceSessionKind.LevelUp.ToString(),
                    StringComparison.Ordinal);

            if (!isNaturalLevelUp)
            {
                if (attempt.requestedCategories.Count != 0)
                {
                    return false;
                }

                continue;
            }

            if (attempt.requestedCategories.Count !=
                    draftRuntime.configuredChoiceCount ||
                attempt.requestedTowerCount + attempt.requestedUpgradeCount !=
                    draftRuntime.configuredChoiceCount ||
                attempt.towerSlotsBackfilledByUpgrade < 0 ||
                attempt.upgradeSlotsBackfilledByTower < 0)
            {
                return false;
            }

            int displayedTowerCount = 0;
            int displayedUpgradeCount = 0;
            HashSet<string> displayedIdentities = new HashSet<string>(
                StringComparer.Ordinal);

            for (int choiceIndex = 0;
                 choiceIndex < attempt.displayedChoices.Count;
                 choiceIndex++)
            {
                CombatBalanceDraftItemJson choice =
                    attempt.displayedChoices[choiceIndex];

                if (choice == null ||
                    string.IsNullOrEmpty(choice.assetName) ||
                    !displayedIdentities.Add(
                        choice.resultType + ":" + choice.assetName))
                {
                    return false;
                }

                if (string.Equals(
                        choice.resultType,
                        DraftResultType.TowerDraft.ToString(),
                        StringComparison.Ordinal))
                {
                    displayedTowerCount++;
                }
                else if (string.Equals(
                             choice.resultType,
                             DraftResultType.TowerUpgradeDraft.ToString(),
                             StringComparison.Ordinal))
                {
                    displayedUpgradeCount++;
                }
                else
                {
                    return false;
                }
            }

            if (displayedTowerCount != attempt.realizedTowerCount ||
                displayedUpgradeCount != attempt.realizedUpgradeCount ||
                attempt.displayedChoices.Count <
                    draftRuntime.configuredChoiceCount &&
                attempt.availableDistinctTowerCount +
                    attempt.availableDistinctUpgradeCount >=
                    draftRuntime.configuredChoiceCount)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool DraftConsumptionIsReconciled(
        CombatBalanceDraftRuntimeJson draftRuntime)
    {
        if (draftRuntime == null)
        {
            return false;
        }

        for (int i = 0; i < draftRuntime.attempts.Count; i++)
        {
            CombatBalanceDraftAttemptJson attempt = draftRuntime.attempts[i];

            if (attempt == null ||
                string.Equals(
                    attempt.consumptionStatus,
                    "MissingInvestmentCommit",
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool TowerWaveAttributionMatches(
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

    internal bool TowerRouteDamageCoverageMatches(
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

                routeAccumulator.towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
                    left.towerInstanceId,
                    out CombatRouteAccumulator.TowerRouteDamageCoverageAggregate leftAggregate);
                routeAccumulator.towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
                    right.towerInstanceId,
                    out CombatRouteAccumulator.TowerRouteDamageCoverageAggregate rightAggregate);

                if (overlap == null ||
                    overlap.towerAInstanceId != left.towerInstanceId ||
                    overlap.towerADeploymentOrdinal !=
                        left.towerDeploymentOrdinal ||
                    overlap.towerBInstanceId != right.towerInstanceId ||
                    overlap.towerBDeploymentOrdinal !=
                        right.towerDeploymentOrdinal ||
                    overlap.sharedDamageRouteCellCount !=
                        CombatRouteAccumulator.CountSharedRouteGridCells(
                            leftAggregate,
                            rightAggregate) ||
                    overlap.sharedRoutePhaseCellCount !=
                        CombatRouteAccumulator.CountSharedRoutePhaseCells(
                            leftAggregate,
                            rightAggregate) ||
                    overlap.sharedDamagedMonsterCount !=
                        CombatRouteAccumulator.CountSharedMonsterInstances(
                            leftAggregate,
                            rightAggregate))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal bool TowerRouteDamageCoverageEntryMatches(
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

        routeAccumulator.towerRouteDamageCoverageByTowerInstanceId.TryGetValue(
            coverage.towerInstanceId,
            out CombatRouteAccumulator.TowerRouteDamageCoverageAggregate aggregate);
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
            distinctGridCells.Add(CombatRouteAccumulator.CreateRouteGridCellKey(cell.x, cell.z));
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

    internal CombatBalanceWaveRuntimeJson CreateWaveRuntimeJson()
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

    internal static WaveResolutionAggregate
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

    internal static float ResolveSecondsAfterFinalDraft(
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

    internal CombatBalanceProgressionJson CreateProgressionJson()
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
                finalLevel = (terminalCaptured || playerSystem != null)
                    ? (terminalCaptured ? terminalLevel : playerSystem.CurrentLevel)
                    : 0,
                finalProgress = (terminalCaptured || playerSystem != null)
                    ? (terminalCaptured ? terminalProgress : playerSystem.CurrentProgress)
                    : 0,
                finalRequiredProgress = (terminalCaptured || playerSystem != null)
                    ? (terminalCaptured ? terminalRequired : playerSystem.RequiredProgress)
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

    internal static bool LevelUpResolutionNodesMatch(
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

    internal float GetRunActiveTimeSeconds()
    {
        return Mathf.Max(0f, ObservedTime - runStartedAtTime);
    }

    internal CombatBalanceMonsterRuntimeJson CreateMonsterRuntimeJson()
    {
        CombatBalanceMonsterRuntimeJson runtime =
            new CombatBalanceMonsterRuntimeJson();
        List<MonsterTypeAggregate> aggregates =
            new List<MonsterTypeAggregate>();
        float observedAtTime = ObservedTime;

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

    internal static MonsterTypeAggregate GetOrCreateMonsterTypeAggregate(
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

    internal static float ResolveMonsterLifetimeSeconds(
        MonsterObservation observation,
        float observedAtTime)
    {
        float resolvedTime = observation.IsResolved
            ? observation.ResolutionTime
            : observedAtTime;
        return Mathf.Max(0f, resolvedTime - observation.SpawnTime);
    }

    internal static string ResolveMonsterResolutionType(
        MonsterObservation observation)
    {
        if (!observation.IsResolved)
        {
            return "Unresolved";
        }

        return observation.ReachedTarget ? "Leaked" : "Killed";
    }

    internal void AppendMonsterRuntimeSummary(
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

    internal static string FormatMetricAverageSeconds(
        CombatBalanceMetricJson metric)
    {
        return metric != null && metric.samples > 0
            ? FormatSeconds(metric.average)
            : "N/A";
    }

    internal bool MonsterRuntimeCountsMatch(
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

    internal bool WaveMonsterAttributionMatches(
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

    internal bool MonsterRuntimeDamageMatches(
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

    internal bool MonsterRuntimeRegistrationCoverageMatches(
        CombatBalanceMonsterRuntimeJson runtime)
    {
        return runtime != null &&
               runtime.instanceSamples == spawnedCount &&
               runtime.registrationObservedInstances == spawnedCount &&
               runtime.fallbackObservedInstances == 0;
    }

    internal static bool MonsterRuntimeStartedAtFullHealth(
        CombatBalanceMonsterRuntimeJson runtime)
    {
        return runtime != null &&
               runtime.instancesObservedAtFullHealth ==
                   runtime.instanceSamples &&
               runtime.instancesObservedAfterDamage == 0 &&
               runtime.unobservedDamageAtObservationStart == 0;
    }

    internal bool TowerDeploymentCoverageMatches(
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

    internal static CombatBalanceVector3Json CreateVector3Json(Vector3 value)
    {
        return new CombatBalanceVector3Json
        {
            x = SanitizeFinite(value.x),
            y = SanitizeFinite(value.y),
            z = SanitizeFinite(value.z)
        };
    }

    internal static float SanitizeFinite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }

    internal static CombatBalanceGridPositionJson CreateGridPositionJson(
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

    internal static void AppendGridPositions(
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

    internal static string CreatePlacementGameplayStateFingerprint(
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

    internal List<CombatBalanceTowerJson> CreateTowerJsonRecords()
    {
        if (terminalCaptured)
        {
            foreach (var tower in terminalTowers)
            {
                tower.projectileRuntime = entityAccumulator.CreateProjectileRuntimeJson(tower.instanceId);
                tower.droneBurstRuntime = entityAccumulator.CreateDroneBurstRuntimeJson(tower.instanceId);
                tower.droneLifecycleRuntime = entityAccumulator.CreateDroneLifecycleRuntimeJson(tower.instanceId);
            }
            return terminalTowers;
        }

        TowerInstance[] towerInstances = new List<TowerInstance>(battleRuntimeCoordinator.Submission.DeployedTowerInstances).ToArray();
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

            record.projectileRuntime = entityAccumulator.CreateProjectileRuntimeJson(
                towerInstanceId);
            record.droneBurstRuntime = entityAccumulator.CreateDroneBurstRuntimeJson(
                towerInstanceId);
            record.droneLifecycleRuntime = entityAccumulator.CreateDroneLifecycleRuntimeJson(
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

    internal CombatBalanceElementalOpportunityDiagnosticsJson
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
            result.candidateResults += aggregate.CandidateResults;
            result.eligibleResults += aggregate.EligibleResults;
            result.dispatchedRequests += aggregate.DispatchedRequests;
            result.scopes.Add(new CombatBalanceElementalOpportunityScopeJson
            {
                sourceTowerInstanceId = aggregate.Source.Id,
                towerFamily = aggregate.Source.Family,
                elementalUpgradeName = aggregate.UpgradeName,
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

    internal bool ElementalOpportunityDiagnosticsAreConsistent()
    {
        Dictionary<string, int> dispatchedBySourceWave =
            new Dictionary<string, int>();

        foreach (KeyValuePair<string, ElementalOpportunityAggregate> entry in
                 elementalOpportunityByScope)
        {
            ElementalOpportunityAggregate aggregate = entry.Value;

            if (aggregate == null ||
                aggregate.Source.Id == 0 ||
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

            string sourceWaveKey = aggregate.SourceWaveKey;
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

    internal static bool IsApprovedPrimaryOpportunityScope(
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

    internal static CombatBalanceMetricJson CreateMetricJson(
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

    internal static string GetReportPath(
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

    internal string ResolveRunName(DateTime generatedAt)
    {
        return string.IsNullOrWhiteSpace(runName)
            ? generatedAt.ToString(
                "yyyyMMdd_HHmmss",
                CultureInfo.InvariantCulture)
            : runName.Trim();
    }

    internal void AppendTowerSnapshot(StringBuilder builder)
    {
        TowerInstance[] towerInstances = new List<TowerInstance>(battleRuntimeCoordinator.Submission.DeployedTowerInstances).ToArray();
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

    internal static void AppendTower(
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

    internal static void AppendUpgrade(
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

    internal static void CountUpgradeLayers(
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

    internal void UnsubscribeFromTrackedMonsters()
    {
        List<MonsterBehaviour> monsters =
            new List<MonsterBehaviour>(trackedMonsters.Keys);

        for (int i = 0; i < monsters.Count; i++)
        {
            UnsubscribeFromMonster(monsters[i]);
        }
        trackedMonsters.Clear();
    }

    internal void UnsubscribeFromMonster(MonsterBehaviour monster)
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

        // Retain captured identity until the outer resolution/hit has drained.
    }

    internal static string GetTowerSortKey(TowerInstance towerInstance)
    {
        if (towerInstance == null || towerInstance.TowerDefinition == null)
        {
            return string.Empty;
        }

        return towerInstance.TowerDefinition.TowerFamily + ":" +
               towerInstance.GetInstanceID();
    }

    internal static string GetDisplayName(string displayName, string fallback)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? fallback
            : displayName.Trim();
    }

    internal static string ResolveRuntimeTemplateName(string runtimeName)
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

    internal static string FormatObservedIntRange(int minimum, int maximum)
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

    internal static string FormatObservedFloatRange(float minimum, float maximum)
    {
        if (float.IsPositiveInfinity(minimum))
        {
            return "NotObserved";
        }

        return Mathf.Approximately(minimum, maximum)
            ? FormatFloat(minimum)
            : FormatFloat(minimum) + "-" + FormatFloat(maximum);
    }

    internal static string FormatPercent(float value)
    {
        return (value * 100f).ToString("0.##", CultureInfo.InvariantCulture) +
               "%";
    }

    internal static string FormatSeconds(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture) + "s";
    }

    internal static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
#endif
