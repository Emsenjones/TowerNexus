#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatBalanceRunRecorder : MonoBehaviour
{
    [Header("Run Identity")]
    [SerializeField] private string runLabel = "Task002 / L / HP120";
    [Min(0)]
    [SerializeField] private int expectedMonsterCount = 40;

    [Header("Runtime References")]
    [SerializeField] private BattleRuntimeCoordinator battleRuntimeCoordinator;
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private PlayerSystem playerSystem;

    private readonly Dictionary<MonsterBehaviour, MonsterObservation>
        trackedMonsters =
            new Dictionary<MonsterBehaviour, MonsterObservation>();
    private readonly HashSet<int> seenMonsterInstanceIds = new HashSet<int>();

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
        public MonsterObservation(int currentHealth)
        {
            LastHealth = currentHealth;
        }

        public int LastHealth { get; set; }
        public bool IsResolved { get; set; }
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
        Debug.Log(BuildSummary("Manual Snapshot", null), this);
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
            monsterSpawner.OnAllSpawningCompleted +=
                HandleAllSpawningCompleted;
        }

        if (battleRuntimeCoordinator != null)
        {
            battleRuntimeCoordinator.OnBattleResultPublished +=
                HandleBattleResultPublished;
            battleRuntimeCoordinator.OnBattleRuntimeFailed +=
                HandleBattleRuntimeFailed;
        }

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
            monsterSpawner.OnAllSpawningCompleted -=
                HandleAllSpawningCompleted;
        }

        if (battleRuntimeCoordinator != null)
        {
            battleRuntimeCoordinator.OnBattleResultPublished -=
                HandleBattleResultPublished;
            battleRuntimeCoordinator.OnBattleRuntimeFailed -=
                HandleBattleRuntimeFailed;
        }

        isSubscribed = false;
    }

    private void HandleBattleStateInitialized()
    {
        BeginRun();
    }

    private void BeginRun()
    {
        UnsubscribeFromTrackedMonsters();
        trackedMonsters.Clear();
        seenMonsterInstanceIds.Clear();
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
            new MonsterObservation(monster.CurrentHealth);
        trackedMonsters.Add(monster, observation);
        effectiveDamage += Mathf.Max(
            0,
            monster.MaxHealth - monster.CurrentHealth);
        monster.OnHealthChanged += HandleMonsterHealthChanged;
        monster.OnResolved += HandleMonsterResolved;
        monster.OnDestroyed += HandleMonsterDestroyed;

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
        hasLoggedFinalSummary = true;
        Debug.Log(BuildSummary(terminalState, failureReason), this);
        isTrackingRun = false;
        UnsubscribeFromTrackedMonsters();
    }

    private string BuildSummary(string terminalState, string failureReason)
    {
        StringBuilder builder = new StringBuilder(1024);
        int unresolvedCount = Mathf.Max(0, spawnedCount - resolvedCount);
        int notSpawnedCount = expectedMonsterCount > 0
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
            .AppendLine(string.IsNullOrWhiteSpace(runLabel)
                ? "Unlabeled"
                : runLabel.Trim());
        builder.Append("Terminal State: ").AppendLine(terminalState);

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            builder.Append("Failure: ").AppendLine(failureReason);
        }

        builder.Append("Monsters: Expected=")
            .Append(expectedMonsterCount)
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
        builder.Append("Monster Fixture: HP=")
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
        return builder.ToString().TrimEnd();
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
