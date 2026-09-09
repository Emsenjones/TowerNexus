using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public enum MonsterPlacementRouteRevisionMode
{
    AlreadyOnNewRoute = 0,
    ReachableRouteRejoin = 1,
    ForcedRelocation = 2
}

public enum MonsterForcedRelocationReason
{
    None = 0,
    NonFiniteTransform = 1,
    UnresolvablePhysicalGrid = 2,
    CoveredByNewFootprint = 3,
    DisconnectedFromNewRoute = 4
}

public enum MonsterPlacementRouteLifecycleKind
{
    Started = 0,
    Joined = 1,
    Superseded = 2,
    MonsterResolvedBeforeJoin = 3,
    ActiveAtRunEnd = 4,
    RelocationApplied = 5
}

public enum MonsterPlacementRouteResolutionReason
{
    None = 0,
    Killed = 1,
    Leaked = 2,
    TechnicalCleanup = 3
}

public readonly struct MonsterPlacementRouteLifecycleObservation
{
    public MonsterPlacementRouteLifecycleObservation(
        long revisionId,
        MonsterBehaviour monster,
        MonsterPlacementRouteLifecycleKind kind,
        MonsterPlacementRouteResolutionReason resolutionReason,
        bool joinedAtCommit,
        long replacementRevisionId)
    {
        RevisionId = revisionId;
        Monster = monster;
        Kind = kind;
        ResolutionReason = resolutionReason;
        JoinedAtCommit = joinedAtCommit;
        ReplacementRevisionId = replacementRevisionId;
    }

    public long RevisionId { get; }
    public MonsterBehaviour Monster { get; }
    public MonsterPlacementRouteLifecycleKind Kind { get; }
    public MonsterPlacementRouteResolutionReason ResolutionReason { get; }
    public bool JoinedAtCommit { get; }
    public long ReplacementRevisionId { get; }
}

internal sealed class MonsterPlacementGameplayStateSnapshot
{
    internal int CurrentHealth { get; set; }
    internal int MaximumHealth { get; set; }
    internal float MoveSpeedMultiplier { get; set; }
    internal bool IsMovementLocked { get; set; }
    internal int LaneIdentity { get; set; }
    internal bool IsGameplayTargetable { get; set; }
    internal string BuffFingerprint { get; set; }
}

internal sealed class MonsterRouteRevisionEntry
{
    private readonly ReadOnlyCollection<GridNodeBehaviour> preparedRoute;
    private readonly ReadOnlyCollection<GridNodeBehaviour> connectorPath;
    private readonly ReadOnlyCollection<GridNodeBehaviour> routeSuffix;

    internal MonsterRouteRevisionEntry(
        long revisionId,
        MonsterBehaviour monster,
        MonsterPlacementRouteRevisionMode mode,
        MonsterForcedRelocationReason relocationReason,
        GridNodeBehaviour physicalCurrentGrid,
        GridNodeBehaviour joinGrid,
        GridNodeBehaviour recoveryGrid,
        GridNodeBehaviour targetNode,
        IReadOnlyList<GridNodeBehaviour> preparedRoute,
        IReadOnlyList<GridNodeBehaviour> connectorPath,
        IReadOnlyList<GridNodeBehaviour> routeSuffix,
        Vector3 capturedWorldPosition,
        Vector3 preparedWorldPosition,
        bool hasComparableCapturedPosition,
        bool hasComparableRelocationDistance,
        float relocationDistance,
        float plannedConnectorDistance,
        bool requiresExactTargetApproach,
        bool requiresConnector,
        bool joinedAtCommit,
        MonsterPlacementGameplayStateSnapshot preState)
    {
        RevisionId = revisionId;
        Monster = monster;
        Mode = mode;
        RelocationReason = relocationReason;
        PhysicalCurrentGrid = physicalCurrentGrid;
        JoinGrid = joinGrid;
        RecoveryGrid = recoveryGrid;
        TargetNode = targetNode;
        CapturedWorldPosition = capturedWorldPosition;
        PreparedWorldPosition = preparedWorldPosition;
        HasComparableCapturedPosition = hasComparableCapturedPosition;
        HasComparableRelocationDistance = hasComparableRelocationDistance;
        RelocationDistance = relocationDistance;
        PlannedConnectorDistance = plannedConnectorDistance;
        RequiresExactTargetApproach = requiresExactTargetApproach;
        RequiresConnector = requiresConnector;
        JoinedAtCommit = joinedAtCommit;
        PreState = preState;
        this.preparedRoute = CopyNodes(preparedRoute);
        this.connectorPath = CopyNodes(connectorPath);
        this.routeSuffix = CopyNodes(routeSuffix);
    }

    internal long RevisionId { get; }
    internal MonsterBehaviour Monster { get; }
    internal MonsterPlacementRouteRevisionMode Mode { get; }
    internal MonsterForcedRelocationReason RelocationReason { get; }
    internal GridNodeBehaviour PhysicalCurrentGrid { get; }
    internal GridNodeBehaviour JoinGrid { get; }
    internal GridNodeBehaviour RecoveryGrid { get; }
    internal GridNodeBehaviour TargetNode { get; }
    internal IReadOnlyList<GridNodeBehaviour> PreparedRoute => preparedRoute;
    internal IReadOnlyList<GridNodeBehaviour> ConnectorPath => connectorPath;
    internal IReadOnlyList<GridNodeBehaviour> RouteSuffix => routeSuffix;
    internal Vector3 CapturedWorldPosition { get; }
    internal Vector3 PreparedWorldPosition { get; }
    internal bool HasComparableCapturedPosition { get; }
    internal bool HasComparableRelocationDistance { get; }
    internal float RelocationDistance { get; }
    internal float PlannedConnectorDistance { get; }
    internal bool RequiresExactTargetApproach { get; }
    internal bool RequiresConnector { get; }
    internal bool JoinedAtCommit { get; }
    internal MonsterPlacementGameplayStateSnapshot PreState { get; }
    internal MonsterPlacementGameplayStateSnapshot PostState { get; set; }
    internal Vector3 ImmediatePostCommitPosition { get; set; }

    private static ReadOnlyCollection<GridNodeBehaviour> CopyNodes(
        IReadOnlyList<GridNodeBehaviour> source)
    {
        GridNodeBehaviour[] copy = source != null
            ? new GridNodeBehaviour[source.Count]
            : Array.Empty<GridNodeBehaviour>();

        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = source[i];
        }

        return Array.AsReadOnly(copy);
    }
}

internal sealed class MonsterRouteRevisionBatch
{
    private readonly ReadOnlyCollection<MonsterRouteRevisionEntry> entries;

    internal MonsterRouteRevisionBatch(
        IReadOnlyList<MonsterRouteRevisionEntry> entries,
        int playerHealthBefore,
        int playerProgressBefore,
        int aliveMonsterCountBefore,
        int resolvedMonsterCountBefore)
    {
        MonsterRouteRevisionEntry[] copy = entries != null
            ? new MonsterRouteRevisionEntry[entries.Count]
            : Array.Empty<MonsterRouteRevisionEntry>();

        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = entries[i];
        }

        this.entries = Array.AsReadOnly(copy);
        PlayerHealthBefore = playerHealthBefore;
        PlayerProgressBefore = playerProgressBefore;
        AliveMonsterCountBefore = aliveMonsterCountBefore;
        ResolvedMonsterCountBefore = resolvedMonsterCountBefore;
    }

    internal IReadOnlyList<MonsterRouteRevisionEntry> Entries => entries;
    internal int LivingMonsterCount => entries.Count;
    internal int PlayerHealthBefore { get; }
    internal int PlayerProgressBefore { get; }
    internal int AliveMonsterCountBefore { get; }
    internal int ResolvedMonsterCountBefore { get; }
    internal int PlayerHealthAfter { get; set; }
    internal int PlayerProgressAfter { get; set; }
    internal int AliveMonsterCountAfter { get; set; }
    internal int ResolvedMonsterCountAfter { get; set; }
    internal int AlreadyOnNewRouteCount { get; set; }
    internal int ReachableRouteRejoinCount { get; set; }
    internal int ForcedRelocationCount { get; set; }
    internal string CombatOwnershipFingerprintBefore { get; set; } =
        string.Empty;
    internal string CombatOwnershipFingerprintAfter { get; set; } =
        string.Empty;
    internal List<MonsterPlacementRouteLifecycleObservation>
        InitialLifecycleObservations { get; } =
            new List<MonsterPlacementRouteLifecycleObservation>();
}

public class MonsterManager : MonoBehaviour
{
    private const float ComparisonEpsilon = 0.0001f;

    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private PlayerSystem playerSystem;

    private readonly List<MonsterBehaviour> aliveMonsters =
        new List<MonsterBehaviour>();
    private MonsterRouteRevisionBatch applyingPlacementRevisionBatch;
    private bool isBattleActive;
    private long nextPlacementRevisionId = 1;
    private int completedMonsterResolutionCount;

    public bool IsBattleActive => isBattleActive;
    public int AliveMonsterCount => aliveMonsters.Count;
    internal MapGeneratorBehaviour ActiveMap =>
        pathfindingService != null ? pathfindingService.ActiveMap : null;

    public event Action OnMonsterResolutionCompleted;
    public event Action<MonsterBehaviour> OnMonsterRegistered;
    public event Action<MonsterPlacementRouteLifecycleObservation>
        OnPlacementRouteLifecycleObserved;

    public IReadOnlyList<MonsterBehaviour> GetAliveMonsters()
    {
        return aliveMonsters;
    }

    public BattleCombatBinding CombatBinding { get; internal set; }

    public void BeginBattle()
    {
        isBattleActive = true;
        completedMonsterResolutionCount = 0;
        nextPlacementRevisionId = 1;
    }

    public bool CanBeginBattle(out string failureReason)
    {
        if (pathfindingService == null)
        {
            failureReason = "A* pathfinding service is not assigned.";
            return false;
        }

        if (!pathfindingService.HasActiveMap)
        {
            failureReason = "A* pathfinding has no Active Map.";
            return false;
        }

        if (playerSystem == null)
        {
            failureReason = "Player System is not assigned.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public void CloseBattleGate()
    {
        CombatBinding?.Close();
        isBattleActive = false;
    }

    public void ForceCleanupAllMonsters()
    {
        List<MonsterBehaviour> snapshot =
            new List<MonsterBehaviour>(aliveMonsters);

        for (int i = 0; i < snapshot.Count; i++)
        {
            MonsterBehaviour monster = snapshot[i];

            if (monster != null)
            {
                monster.ForceCleanup();
            }
        }

        aliveMonsters.Clear();
    }

    public void StopBattle()
    {
        CloseBattleGate();
        ForceCleanupAllMonsters();
    }

    public bool RegisterMonster(MonsterBehaviour monster)
    {
        if (!isBattleActive || monster == null || aliveMonsters.Contains(monster))
        {
            return false;
        }

        if (CombatBinding == null || !CombatBinding.IsUsable ||
            !ReferenceEquals(monster.CombatBinding, CombatBinding)) return false;
        aliveMonsters.Add(monster);
        monster.OnResolved += HandleMonsterResolved;
        monster.OnPlacementRouteLifecycleObserved +=
            HandlePlacementRouteLifecycleObserved;
        PublishMonsterRegistered(monster);
        return true;
    }

    public void UnregisterMonster(MonsterBehaviour monster)
    {
        if (monster == null)
        {
            return;
        }

        monster.OnResolved -= HandleMonsterResolved;
        monster.OnPlacementRouteLifecycleObserved -=
            HandlePlacementRouteLifecycleObserved;
        aliveMonsters.Remove(monster);
    }

    public void RecalculateAllMonsterPaths()
    {
        if (!isBattleActive)
        {
            return;
        }

        for (int i = aliveMonsters.Count - 1; i >= 0; i--)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster == null)
            {
                aliveMonsters.RemoveAt(i);
                continue;
            }

            if (monster.IsDead())
            {
                UnregisterMonster(monster);
                continue;
            }

            RequestPathRecalculation(monster);
        }
    }

    public void RequestPathRecalculation(MonsterBehaviour monster)
    {
        if (!isBattleActive || monster == null || monster.IsDead() ||
            !aliveMonsters.Contains(monster))
        {
            return;
        }

        if (pathfindingService == null)
        {
            Debug.LogWarning(
                "Monster manager cannot recalculate path: pathfinding service is not assigned.",
                this);
            monster.StopMovement();
            return;
        }

        GridNodeBehaviour currentNode = monster.GetCurrentNode();
        GridNodeBehaviour targetNode = monster.GetTargetNode();

        if (currentNode == null || targetNode == null)
        {
            monster.StopMovement();
            return;
        }

        List<GridNodeBehaviour> path =
            pathfindingService.FindPath(currentNode, targetNode);

        if (path == null || path.Count == 0)
        {
            monster.StopMovement();
            return;
        }

        monster.SetPath(path);
    }

    internal bool TryPrepareTopologyRevision(
        TowerPlacementTopologyPlan topologyPlan,
        out MonsterRouteRevisionBatch revisionBatch,
        out string failureReason)
    {
        revisionBatch = null;

        if (topologyPlan == null)
        {
            failureReason = "the Tower placement topology plan is missing.";
            return false;
        }

        MapGeneratorBehaviour activeMap =
            pathfindingService != null ? pathfindingService.ActiveMap : null;

        if (activeMap == null || activeMap.NodesRoot == null)
        {
            failureReason =
                "Monster Manager has no Active Map with a usable Nodes Root.";
            return false;
        }

        if (!TryValidateTopologyPlan(
                topologyPlan,
                activeMap,
                out GridNodeBehaviour targetNode,
                out failureReason) ||
            !pathfindingService.TryBuildRouteConnectivityMap(
                topologyPlan.AuthoritativeRoute,
                topologyPlan.Footprint,
                out PreparedRouteConnectivityMap connectivityMap))
        {
            if (string.IsNullOrEmpty(failureReason))
            {
                failureReason =
                    "the authoritative Route connectivity query could not be prepared.";
            }

            return false;
        }

        HashSet<GridNodeBehaviour> footprint =
            new HashSet<GridNodeBehaviour>(topologyPlan.Footprint);
        Dictionary<GridNodeBehaviour, int> routeIndexByNode =
            BuildRouteIndex(topologyPlan.AuthoritativeRoute);
        Dictionary<string, List<GridNodeBehaviour>> connectorCache =
            new Dictionary<string, List<GridNodeBehaviour>>();
        List<MonsterRouteRevisionEntry> entries =
            new List<MonsterRouteRevisionEntry>();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster == null || !monster.IsGameplayTargetable)
            {
                continue;
            }

            MonsterMovementSnapshot snapshot =
                monster.CaptureMovementSnapshot(activeMap);

            if (!TryPrepareMonsterRevision(
                    monster,
                    snapshot,
                    topologyPlan,
                    activeMap,
                    targetNode,
                    footprint,
                    routeIndexByNode,
                    connectivityMap,
                    connectorCache,
                    out MonsterRouteRevisionEntry entry))
            {
                failureReason =
                    $"no total placement-route revision could be prepared for Monster '{monster.name}'.";
                return false;
            }

            entries.Add(entry);
        }

        revisionBatch = new MonsterRouteRevisionBatch(
            entries,
            playerSystem != null ? playerSystem.CurrentHealth : 0,
            playerSystem != null ? playerSystem.CurrentProgress : 0,
            entries.Count,
            completedMonsterResolutionCount);
        CountModes(revisionBatch);
        failureReason = string.Empty;
        return true;
    }

    internal void ApplyPreparedMovementRevisionBatch(
        MonsterRouteRevisionBatch revisionBatch)
    {
        IReadOnlyList<MonsterRouteRevisionEntry> entries = revisionBatch.Entries;
        applyingPlacementRevisionBatch = revisionBatch;

        try
        {
            for (int i = 0; i < entries.Count; i++)
            {
                MonsterRouteRevisionEntry entry = entries[i];
                entry.Monster.ApplyPreparedMovementRevision(entry);
                entry.ImmediatePostCommitPosition =
                    entry.Monster.transform.position;
                entry.PostState =
                    entry.Monster.CapturePlacementGameplayState();
            }
        }
        finally
        {
            applyingPlacementRevisionBatch = null;
        }

        revisionBatch.PlayerHealthAfter =
            playerSystem != null ? playerSystem.CurrentHealth : 0;
        revisionBatch.PlayerProgressAfter =
            playerSystem != null ? playerSystem.CurrentProgress : 0;
        revisionBatch.AliveMonsterCountAfter =
            CountGameplayTargetableMonsters();
        revisionBatch.ResolvedMonsterCountAfter =
            completedMonsterResolutionCount;
    }

    internal void PublishCommittedPlacementRouteLifecycle(
        MonsterRouteRevisionBatch revisionBatch)
    {
        if (revisionBatch == null)
        {
            return;
        }

        for (int i = 0;
             i < revisionBatch.InitialLifecycleObservations.Count;
             i++)
        {
            PublishPlacementRouteLifecycleObservation(
                revisionBatch.InitialLifecycleObservations[i]);
        }
    }

    private bool TryPrepareMonsterRevision(
        MonsterBehaviour monster,
        MonsterMovementSnapshot snapshot,
        TowerPlacementTopologyPlan topologyPlan,
        MapGeneratorBehaviour activeMap,
        GridNodeBehaviour targetNode,
        HashSet<GridNodeBehaviour> footprint,
        IReadOnlyDictionary<GridNodeBehaviour, int> routeIndexByNode,
        PreparedRouteConnectivityMap connectivityMap,
        IDictionary<string, List<GridNodeBehaviour>> connectorCache,
        out MonsterRouteRevisionEntry entry)
    {
        entry = null;
        MonsterPlacementRouteRevisionMode mode;
        MonsterForcedRelocationReason relocationReason =
            MonsterForcedRelocationReason.None;
        GridNodeBehaviour physicalGrid = null;

        if (!snapshot.HasFiniteWorldPosition)
        {
            mode = MonsterPlacementRouteRevisionMode.ForcedRelocation;
            relocationReason =
                MonsterForcedRelocationReason.NonFiniteTransform;
        }
        else if (!activeMap.TryGetNodeByWorldPosition(
                     snapshot.WorldPosition,
                     out physicalGrid))
        {
            mode = MonsterPlacementRouteRevisionMode.ForcedRelocation;
            relocationReason =
                MonsterForcedRelocationReason.UnresolvablePhysicalGrid;
        }
        else if (footprint.Contains(physicalGrid))
        {
            mode = MonsterPlacementRouteRevisionMode.ForcedRelocation;
            relocationReason =
                MonsterForcedRelocationReason.CoveredByNewFootprint;
        }
        else if (routeIndexByNode.ContainsKey(physicalGrid))
        {
            mode = MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute;
        }
        else if (connectivityMap.Contains(physicalGrid))
        {
            mode = MonsterPlacementRouteRevisionMode.ReachableRouteRejoin;
        }
        else
        {
            mode = MonsterPlacementRouteRevisionMode.ForcedRelocation;
            relocationReason =
                MonsterForcedRelocationReason.DisconnectedFromNewRoute;
        }

        GridNodeBehaviour anchorGrid = physicalGrid;
        GridNodeBehaviour joinGrid = null;
        GridNodeBehaviour recoveryGrid = null;
        Vector3 preparedWorldPosition = snapshot.WorldPosition;
        bool hasComparableRelocationDistance = false;
        float relocationDistance = 0f;
        List<GridNodeBehaviour> connectorPath = new List<GridNodeBehaviour>();
        List<GridNodeBehaviour> routeSuffix = new List<GridNodeBehaviour>();
        bool requiresExactTargetApproach =
            mode == MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute &&
            physicalGrid == targetNode;

        if (requiresExactTargetApproach)
        {
            joinGrid = targetNode;
        }
        else if (mode == MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute)
        {
            joinGrid = physicalGrid;
            routeSuffix = SliceRoute(
                topologyPlan.AuthoritativeRoute,
                routeIndexByNode[joinGrid]);
        }
        else if (mode == MonsterPlacementRouteRevisionMode.ReachableRouteRejoin)
        {
            joinGrid = SelectSpatiallyNearestRouteJoin(
                monster,
                snapshot,
                topologyPlan.AuthoritativeRoute,
                activeMap);
            connectorPath = GetOrCreateConnector(
                physicalGrid,
                joinGrid,
                topologyPlan.Footprint,
                connectorCache);

            if (connectorPath.Count == 0)
            {
                return false;
            }

            routeSuffix = SliceRoute(
                topologyPlan.AuthoritativeRoute,
                routeIndexByNode[joinGrid]);
        }
        else
        {
            if (!TrySelectRecovery(
                    monster,
                    snapshot,
                    activeMap,
                    targetNode,
                    footprint,
                    connectivityMap,
                    out recoveryGrid,
                    out joinGrid,
                    out preparedWorldPosition,
                    out hasComparableRelocationDistance,
                    out relocationDistance,
                    out connectorPath))
            {
                return false;
            }

            anchorGrid = recoveryGrid;
            routeSuffix = SliceRoute(
                topologyPlan.AuthoritativeRoute,
                routeIndexByNode[joinGrid]);
        }

        List<GridNodeBehaviour> preparedRoute = requiresExactTargetApproach
            ? new List<GridNodeBehaviour>()
            : mode == MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute
                ? new List<GridNodeBehaviour>(routeSuffix)
                : CombineConnectorAndSuffix(connectorPath, routeSuffix);
        bool requiresConnector =
            mode != MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute &&
            connectorPath.Count > 1;
        bool joinedAtCommit =
            mode == MonsterPlacementRouteRevisionMode.ForcedRelocation &&
            connectorPath.Count == 1 &&
            recoveryGrid == joinGrid;
        float plannedConnectorDistance = CalculateConnectorDistance(
            monster,
            activeMap,
            mode == MonsterPlacementRouteRevisionMode.ForcedRelocation
                ? preparedWorldPosition
                : snapshot.WorldPosition,
            connectorPath);

        entry = new MonsterRouteRevisionEntry(
            nextPlacementRevisionId++,
            monster,
            mode,
            relocationReason,
            physicalGrid,
            joinGrid,
            recoveryGrid,
            targetNode,
            preparedRoute,
            connectorPath,
            routeSuffix,
            snapshot.WorldPosition,
            preparedWorldPosition,
            snapshot.HasFiniteWorldPosition,
            hasComparableRelocationDistance,
            relocationDistance,
            plannedConnectorDistance,
            requiresExactTargetApproach,
            requiresConnector,
            joinedAtCommit,
            monster.CapturePlacementGameplayState());
        return true;
    }

    private int CountGameplayTargetableMonsters()
    {
        int count = 0;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            if (aliveMonsters[i] != null &&
                aliveMonsters[i].IsGameplayTargetable)
            {
                count++;
            }
        }

        return count;
    }

    private static float CalculateConnectorDistance(
        MonsterBehaviour monster,
        MapGeneratorBehaviour activeMap,
        Vector3 startPosition,
        IReadOnlyList<GridNodeBehaviour> connectorPath)
    {
        if (monster == null || activeMap == null ||
            connectorPath == null ||
            connectorPath.Count <= 1 ||
            !IsFinite(startPosition))
        {
            return 0f;
        }

        if (!TryGetMapLocalXz(activeMap, startPosition, out Vector2 from))
        {
            return 0f;
        }

        float distance = 0f;

        for (int i = 1; i < connectorPath.Count; i++)
        {
            Vector3 to = monster.ResolveMovementTargetPosition(
                connectorPath[i],
                startPosition.y);
            if (!TryGetMapLocalXz(activeMap, to, out Vector2 toLocal))
            {
                return 0f;
            }

            distance += Vector2.Distance(from, toLocal);
            from = toLocal;
        }

        return distance;
    }

    private bool TrySelectRecovery(
        MonsterBehaviour monster,
        MonsterMovementSnapshot snapshot,
        MapGeneratorBehaviour activeMap,
        GridNodeBehaviour targetNode,
        HashSet<GridNodeBehaviour> footprint,
        PreparedRouteConnectivityMap connectivityMap,
        out GridNodeBehaviour recoveryGrid,
        out GridNodeBehaviour joinGrid,
        out Vector3 recoveryPosition,
        out bool hasComparableDistance,
        out float relocationDistance,
        out List<GridNodeBehaviour> connectorPath)
    {
        recoveryGrid = null;
        joinGrid = null;
        recoveryPosition = default;
        connectorPath = new List<GridNodeBehaviour>();
        relocationDistance = 0f;
        Vector2 capturedLocal = default;
        hasComparableDistance =
            snapshot.HasFiniteWorldPosition &&
            TryGetMapLocalXz(
                activeMap,
                snapshot.WorldPosition,
                out capturedLocal);
        float bestDistanceSquared = float.PositiveInfinity;
        int bestConnectorCost = int.MaxValue;

        for (int x = 0; x < activeMap.Width; x++)
        {
            for (int y = 0; y < activeMap.Lengh; y++)
            {
                GridNodeBehaviour candidate = activeMap.GetNode(x, y);

                if (candidate == null ||
                    candidate == targetNode ||
                    footprint.Contains(candidate) ||
                    !connectivityMap.TryGetConnection(
                        candidate,
                        out int connectorCost,
                        out GridNodeBehaviour candidateJoin))
                {
                    continue;
                }

                float resolvedY = snapshot.HasFiniteWorldY
                    ? snapshot.WorldPosition.y
                    : candidate.WorldPosition.y;
                Vector3 candidatePosition =
                    monster.ResolveMovementTargetPosition(candidate, resolvedY);
                float distanceSquared = 0f;

                if (hasComparableDistance &&
                    TryGetMapLocalXz(
                        activeMap,
                        candidatePosition,
                        out Vector2 candidateLocal))
                {
                    distanceSquared =
                        (candidateLocal - capturedLocal).sqrMagnitude;
                }

                if (!IsBetterRecoveryCandidate(
                        candidate,
                        distanceSquared,
                        connectorCost,
                        recoveryGrid,
                        bestDistanceSquared,
                        bestConnectorCost,
                        hasComparableDistance))
                {
                    continue;
                }

                recoveryGrid = candidate;
                joinGrid = candidateJoin;
                recoveryPosition = candidatePosition;
                bestDistanceSquared = distanceSquared;
                bestConnectorCost = connectorCost;
            }
        }

        if (recoveryGrid == null || joinGrid == null)
        {
            return false;
        }

        connectorPath = connectivityMap.BuildPathToJoin(recoveryGrid);

        if (connectorPath.Count == 0)
        {
            return false;
        }

        relocationDistance = hasComparableDistance
            ? Mathf.Sqrt(bestDistanceSquared)
            : 0f;
        return true;
    }

    private static bool IsBetterRecoveryCandidate(
        GridNodeBehaviour candidate,
        float candidateDistanceSquared,
        int candidateConnectorCost,
        GridNodeBehaviour current,
        float currentDistanceSquared,
        int currentConnectorCost,
        bool hasComparableDistance)
    {
        if (current == null)
        {
            return true;
        }

        if (hasComparableDistance)
        {
            if (candidateDistanceSquared <
                currentDistanceSquared - ComparisonEpsilon)
            {
                return true;
            }

            if (Mathf.Abs(candidateDistanceSquared - currentDistanceSquared) >
                ComparisonEpsilon)
            {
                return false;
            }
        }

        if (candidateConnectorCost != currentConnectorCost)
        {
            return candidateConnectorCost < currentConnectorCost;
        }

        Vector2Int left = candidate.GridPosition;
        Vector2Int right = current.GridPosition;
        return left.x != right.x ? left.x < right.x : left.y < right.y;
    }

    private GridNodeBehaviour SelectSpatiallyNearestRouteJoin(
        MonsterBehaviour monster,
        MonsterMovementSnapshot snapshot,
        IReadOnlyList<GridNodeBehaviour> route,
        MapGeneratorBehaviour activeMap)
    {
        TryGetMapLocalXz(
            activeMap,
            snapshot.WorldPosition,
            out Vector2 capturedLocal);
        GridNodeBehaviour best = route[0];
        float bestDistanceSquared = float.PositiveInfinity;

        for (int i = 0; i < route.Count - 1; i++)
        {
            GridNodeBehaviour candidate = route[i];
            Vector3 candidatePosition = monster.ResolveMovementTargetPosition(
                candidate,
                snapshot.WorldPosition.y);
            TryGetMapLocalXz(
                activeMap,
                candidatePosition,
                out Vector2 candidateLocal);
            float distanceSquared =
                (candidateLocal - capturedLocal).sqrMagnitude;

            if (distanceSquared < bestDistanceSquared - ComparisonEpsilon)
            {
                best = candidate;
                bestDistanceSquared = distanceSquared;
            }
        }

        return best;
    }

    private List<GridNodeBehaviour> GetOrCreateConnector(
        GridNodeBehaviour start,
        GridNodeBehaviour join,
        IReadOnlyCollection<GridNodeBehaviour> footprint,
        IDictionary<string, List<GridNodeBehaviour>> connectorCache)
    {
        string key =
            $"{start.GridPosition.x},{start.GridPosition.y}>" +
            $"{join.GridPosition.x},{join.GridPosition.y}";

        if (!connectorCache.TryGetValue(key, out List<GridNodeBehaviour> path))
        {
            path = pathfindingService.FindPath(start, join, footprint);
            connectorCache[key] = path;
        }

        return new List<GridNodeBehaviour>(path);
    }

    private static List<GridNodeBehaviour> CombineConnectorAndSuffix(
        IReadOnlyList<GridNodeBehaviour> connector,
        IReadOnlyList<GridNodeBehaviour> suffix)
    {
        List<GridNodeBehaviour> combined =
            new List<GridNodeBehaviour>();

        for (int i = 0; i < connector.Count; i++)
        {
            combined.Add(connector[i]);
        }

        int suffixStart = combined.Count > 0 &&
                          suffix.Count > 0 &&
                          combined[combined.Count - 1] == suffix[0]
            ? 1
            : 0;

        for (int i = suffixStart; i < suffix.Count; i++)
        {
            combined.Add(suffix[i]);
        }

        return combined;
    }

    private static List<GridNodeBehaviour> SliceRoute(
        IReadOnlyList<GridNodeBehaviour> route,
        int startIndex)
    {
        List<GridNodeBehaviour> result =
            new List<GridNodeBehaviour>(route.Count - startIndex);

        for (int i = startIndex; i < route.Count; i++)
        {
            result.Add(route[i]);
        }

        return result;
    }

    private static Dictionary<GridNodeBehaviour, int> BuildRouteIndex(
        IReadOnlyList<GridNodeBehaviour> route)
    {
        Dictionary<GridNodeBehaviour, int> result =
            new Dictionary<GridNodeBehaviour, int>();

        for (int i = 0; i < route.Count; i++)
        {
            result[route[i]] = i;
        }

        return result;
    }

    private static void CountModes(MonsterRouteRevisionBatch batch)
    {
        for (int i = 0; i < batch.Entries.Count; i++)
        {
            switch (batch.Entries[i].Mode)
            {
                case MonsterPlacementRouteRevisionMode.AlreadyOnNewRoute:
                    batch.AlreadyOnNewRouteCount++;
                    break;
                case MonsterPlacementRouteRevisionMode.ReachableRouteRejoin:
                    batch.ReachableRouteRejoinCount++;
                    break;
                case MonsterPlacementRouteRevisionMode.ForcedRelocation:
                    batch.ForcedRelocationCount++;
                    break;
            }
        }
    }

    private static bool TryValidateTopologyPlan(
        TowerPlacementTopologyPlan topologyPlan,
        MapGeneratorBehaviour activeMap,
        out GridNodeBehaviour targetNode,
        out string failureReason)
    {
        targetNode = activeMap.GetTargetNode();
        GridNodeBehaviour spawnNode = activeMap.GetSpawnNode();
        IReadOnlyList<GridNodeBehaviour> route = topologyPlan.AuthoritativeRoute;

        if (topologyPlan.Footprint.Count == 0)
        {
            failureReason = "the topology plan footprint is empty.";
            return false;
        }

        if (spawnNode == null || targetNode == null)
        {
            failureReason =
                "the Active Map does not provide both Spawn and Target Grid Nodes.";
            return false;
        }

        if (route.Count < 2 ||
            route[0] != spawnNode ||
            route[route.Count - 1] != targetNode)
        {
            failureReason =
                "the authoritative Route is not a complete Spawn-to-Target Route with an eligible non-Target Grid.";
            return false;
        }

        for (int i = 0; i < topologyPlan.Footprint.Count; i++)
        {
            if (topologyPlan.Footprint[i] == null)
            {
                failureReason =
                    "the topology plan footprint contains a null Grid Node.";
                return false;
            }
        }

        for (int i = 0; i < route.Count; i++)
        {
            GridNodeBehaviour routeNode = route[i];

            if (routeNode == null ||
                activeMap.GetNode(routeNode.GridPosition) != routeNode)
            {
                failureReason =
                    "the authoritative Route contains a Grid Node outside the Active Map.";
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    private static bool TryGetMapLocalXz(
        MapGeneratorBehaviour activeMap,
        Vector3 worldPosition,
        out Vector2 localPosition)
    {
        localPosition = default;

        if (!IsFinite(worldPosition))
        {
            return false;
        }

        Vector3 local = activeMap.NodesRoot.InverseTransformPoint(worldPosition);

        if (!IsFinite(local))
        {
            return false;
        }

        localPosition = new Vector2(local.x, local.z);
        return true;
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

    private void OnDisable()
    {
        isBattleActive = false;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster != null)
            {
                monster.OnResolved -= HandleMonsterResolved;
                monster.OnPlacementRouteLifecycleObserved -=
                    HandlePlacementRouteLifecycleObserved;
            }
        }

        aliveMonsters.Clear();
    }

    private void HandleMonsterResolved(
        MonsterBehaviour monster,
        bool reachedTarget)
    {
        UnregisterMonster(monster);

        if (!isBattleActive || playerSystem == null)
        {
            return;
        }

        if (playerSystem.TryResolveMonster(reachedTarget))
        {
            completedMonsterResolutionCount++;
            OnMonsterResolutionCompleted?.Invoke();
        }
    }

    private void HandlePlacementRouteLifecycleObserved(
        MonsterPlacementRouteLifecycleObservation observation)
    {
        if (applyingPlacementRevisionBatch != null)
        {
            applyingPlacementRevisionBatch.InitialLifecycleObservations.Add(
                observation);
            return;
        }

        PublishPlacementRouteLifecycleObservation(observation);
    }

    private void PublishPlacementRouteLifecycleObservation(
        MonsterPlacementRouteLifecycleObservation observation)
    {
        Action<MonsterPlacementRouteLifecycleObservation> handlers =
            OnPlacementRouteLifecycleObserved;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<MonsterPlacementRouteLifecycleObservation>)
                    invocationList[i]).Invoke(observation);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }

    private void PublishMonsterRegistered(MonsterBehaviour monster)
    {
        Action<MonsterBehaviour> handlers = OnMonsterRegistered;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<MonsterBehaviour>)invocationList[i]).Invoke(monster);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
