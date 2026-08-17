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

    private readonly Dictionary<MonsterBehaviour, MonsterObservation>
        trackedMonsters =
            new Dictionary<MonsterBehaviour, MonsterObservation>();
    private readonly HashSet<int> seenMonsterInstanceIds = new HashSet<int>();
    private readonly ElementalBuffRunAccumulator buffAccumulator =
        new ElementalBuffRunAccumulator();
    private readonly Dictionary<int, ProjectileRuntimeAggregate>
        projectileRuntimeByTowerInstanceId =
            new Dictionary<int, ProjectileRuntimeAggregate>();

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
    private string waveConfigName;
    private float observedMinimumMonsterSpeed;
    private float observedMaximumMonsterSpeed;
    private float firstSpawnTime;
    private float lastSpawnTime;
    private float lastResolutionTime;
    private float spawningCompletedTime;
    private float observedSpawnIntervalTotal;
    private int observedSpawnIntervalCount;

    private sealed class MonsterObservation
    {
        public MonsterObservation(int currentHealth, int spawnOrdinal)
        {
            LastHealth = currentHealth;
            SpawnOrdinal = spawnOrdinal;
        }

        public int LastHealth { get; set; }
        public int SpawnOrdinal { get; }
        public bool IsResolved { get; set; }
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

    private void OnEnable()
    {
        ResolveReferences();
        WarnAboutMissingReferences();
        SubscribeToRuntime();
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
        }

        if (monsterSpawner != null)
        {
            monsterSpawner.OnSpawningStarted += HandleSpawningStarted;
            monsterSpawner.OnAllSpawningCompleted +=
                HandleAllSpawningCompleted;
        }

        if (monsterManager != null)
        {
            monsterManager.OnMonsterRegistered += HandleMonsterRegistered;
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
        }

        if (monsterSpawner != null)
        {
            monsterSpawner.OnSpawningStarted -= HandleSpawningStarted;
            monsterSpawner.OnAllSpawningCompleted -=
                HandleAllSpawningCompleted;
        }

        if (monsterManager != null)
        {
            monsterManager.OnMonsterRegistered -= HandleMonsterRegistered;
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

        isSubscribed = false;
    }

    private void HandleBattleStateInitialized()
    {
        BeginRun();
    }

    private void HandleSpawningStarted()
    {
        CaptureExpectedMonsterFixture();
    }

    private void BeginRun()
    {
        UnsubscribeFromTrackedMonsters();
        trackedMonsters.Clear();
        seenMonsterInstanceIds.Clear();
        buffAccumulator.Reset();
        projectileRuntimeByTowerInstanceId.Clear();
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
        hasObservedFirstSpawn = false;
        hasObservedSpawningCompletion = false;
        hasLoggedFinalSummary = false;
        hasPendingFinalSummary = false;
        pendingTerminalState = null;
        pendingFailureReason = null;
        isTrackingRun = true;

        if (monsterSpawner != null && monsterSpawner.IsSpawning)
        {
            CaptureExpectedMonsterFixture();
        }
    }

    private void ResetExpectedMonsterFixture()
    {
        expectedMonsterCount = 0;
        hasExpectedMonsterCount = false;
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
            return;
        }

        Debug.LogWarning(
            "Combat balance run recorder could not resolve Expected Monster " +
            "Count from the Monster Spawner's bound Wave Config.",
            this);
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
            TrackMonster(aliveMonsters[i]);
        }
    }

    private void TrackMonster(MonsterBehaviour monster)
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

        MonsterObservation observation =
            new MonsterObservation(
                monster.CurrentHealth,
                spawnedCount + 1);
        trackedMonsters.Add(monster, observation);
        effectiveDamage += Mathf.Max(
            0,
            monster.MaxHealth - monster.CurrentHealth);
        monster.OnHealthChanged += HandleMonsterHealthChanged;
        monster.OnResolved += HandleMonsterResolved;
        monster.OnDestroyed += HandleMonsterDestroyed;
        monster.OnBuffRuntimeObserved += HandleBuffRuntimeObserved;

        float observedTime = Time.time;

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

        effectiveDamage += Mathf.Max(
            0,
            observation.LastHealth - currentHealth);
        observation.LastHealth = currentHealth;
    }

    private void HandleMonsterRegistered(MonsterBehaviour monster)
    {
        if (isTrackingRun)
        {
            TrackMonster(monster);
        }
    }

    private void HandleBuffRuntimeObserved(
        MonsterBehaviour _,
        BuffRuntimeObservation observation)
    {
        if (isTrackingRun)
        {
            buffAccumulator.Consume(observation);
        }
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
            TrackMonster(monster);
            trackedMonsters.TryGetValue(monster, out observation);
        }

        if (observation == null || observation.IsResolved)
        {
            return;
        }

        observation.IsResolved = true;
        resolvedCount++;
        lastResolutionTime = Time.time;

        if (reachedTarget)
        {
            leakedCount++;
            leakedRemainingHealth += monster.CurrentHealth;
            return;
        }

        killedCount++;
    }

    private void HandleMonsterDestroyed(MonsterBehaviour monster)
    {
        UnsubscribeFromMonster(monster);
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
            .AppendLine();

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

        report.integrity.resolutionCountsMatch =
            resolvedCount == killedCount + leakedCount;
        report.integrity.leakCountMatchesPlayerHealthLoss =
            playerSystem != null && leakedCount == observedPlayerHealthLoss;

        report.towers = CreateTowerJsonRecords();
        report.buffs = buffAccumulator.CreateJsonRecords();
        return report;
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
            CombatBalanceTowerJson record = new CombatBalanceTowerJson
            {
                instanceId = towerInstance.GetInstanceID(),
                displayName = GetDisplayName(
                    definition.DisplayName,
                    definition.name),
                family = definition.TowerFamily.ToString(),
                level = towerInstance.CurrentLevel,
                hasCombatRuntime = combatBehaviour != null
            };

            if (combatBehaviour != null)
            {
                TowerCombatBaseStats baseStats = new TowerCombatBaseStats(
                    combatBehaviour.BaseAttackDamage,
                    combatBehaviour.BaseAttackRange,
                    combatBehaviour.BaseAttackCycleDuration);
                ResolvedTowerCombatStats resolvedStats =
                    TowerRuntimeStatResolver.Resolve(towerInstance, baseStats);
                record.baseDamage = combatBehaviour.BaseAttackDamage;
                record.baseRange = combatBehaviour.BaseAttackRange;
                record.baseCycleSeconds =
                    combatBehaviour.BaseAttackCycleDuration;
                record.resolvedDamage = resolvedStats.AttackDamage;
                record.resolvedRange = resolvedStats.AttackRange;
                record.resolvedCycleSeconds = resolvedStats.AttackCycleDuration;
            }

            record.projectileRuntime = CreateProjectileRuntimeJson(
                towerInstance.GetInstanceID());

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
                combatBehaviour.BaseAttackDamage,
                combatBehaviour.BaseAttackRange,
                combatBehaviour.BaseAttackCycleDuration);
            ResolvedTowerCombatStats resolvedStats =
                TowerRuntimeStatResolver.Resolve(towerInstance, baseStats);

            builder.Append(", BaseStats=[Damage=")
                .Append(combatBehaviour.BaseAttackDamage)
                .Append(", Range=")
                .Append(FormatFloat(combatBehaviour.BaseAttackRange))
                .Append(", Cycle=")
                .Append(FormatSeconds(
                    combatBehaviour.BaseAttackCycleDuration))
                .Append(']')
                .Append(", ResolvedStats=[Damage=")
                .Append(resolvedStats.AttackDamage)
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
