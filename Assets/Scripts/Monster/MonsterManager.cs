using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

internal sealed class MonsterRouteRevisionEntry
{
    private readonly ReadOnlyCollection<GridNodeBehaviour> currentRoute;

    internal MonsterRouteRevisionEntry(
        MonsterBehaviour monster,
        GridNodeBehaviour reachedNode,
        GridNodeBehaviour activeNextNode,
        GridNodeBehaviour targetNode,
        IReadOnlyList<GridNodeBehaviour> currentRoute,
        int routeIndex,
        Vector3 projectedWorldPosition,
        Vector3 prePlacementWorldPosition,
        float worldDisplacementDistance,
        float remainingCenterlineDistanceDelta,
        bool hasComparableRemainingDistance,
        bool usedDeterministicFallback)
    {
        Monster = monster;
        ReachedNode = reachedNode;
        ActiveNextNode = activeNextNode;
        TargetNode = targetNode;
        RouteIndex = routeIndex;
        ProjectedWorldPosition = projectedWorldPosition;
        PrePlacementWorldPosition = prePlacementWorldPosition;
        WorldDisplacementDistance = worldDisplacementDistance;
        RemainingCenterlineDistanceDelta = remainingCenterlineDistanceDelta;
        HasComparableRemainingDistance = hasComparableRemainingDistance;
        UsedDeterministicFallback = usedDeterministicFallback;

        GridNodeBehaviour[] routeCopy = currentRoute != null
            ? new GridNodeBehaviour[currentRoute.Count]
            : Array.Empty<GridNodeBehaviour>();

        for (int i = 0; i < routeCopy.Length; i++)
        {
            routeCopy[i] = currentRoute[i];
        }

        this.currentRoute = Array.AsReadOnly(routeCopy);
    }

    internal MonsterBehaviour Monster { get; }
    internal GridNodeBehaviour ReachedNode { get; }
    internal GridNodeBehaviour ActiveNextNode { get; }
    internal GridNodeBehaviour TargetNode { get; }
    internal IReadOnlyList<GridNodeBehaviour> CurrentRoute => currentRoute;
    internal int RouteIndex { get; }
    internal Vector3 ProjectedWorldPosition { get; }
    internal Vector3 PrePlacementWorldPosition { get; }
    internal float WorldDisplacementDistance { get; }
    internal float RemainingCenterlineDistanceDelta { get; }
    internal bool HasComparableRemainingDistance { get; }
    internal bool UsedDeterministicFallback { get; }
}

internal sealed class MonsterRouteRevisionBatch
{
    private readonly ReadOnlyCollection<MonsterRouteRevisionEntry> entries;

    internal MonsterRouteRevisionBatch(
        IReadOnlyList<MonsterRouteRevisionEntry> entries)
    {
        MonsterRouteRevisionEntry[] entryCopy = entries != null
            ? new MonsterRouteRevisionEntry[entries.Count]
            : Array.Empty<MonsterRouteRevisionEntry>();

        for (int i = 0; i < entryCopy.Length; i++)
        {
            entryCopy[i] = entries[i];
        }

        this.entries = Array.AsReadOnly(entryCopy);
    }

    internal IReadOnlyList<MonsterRouteRevisionEntry> Entries => entries;
    internal int AffectedMonsterCount => entries.Count;
}

public class MonsterManager : MonoBehaviour
{
    private const float ComparisonEpsilon = 0.0001f;

    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private PlayerSystem playerSystem;

    private readonly List<MonsterBehaviour> aliveMonsters = new List<MonsterBehaviour>();
    private bool isBattleActive;

    public bool IsBattleActive => isBattleActive;
    public int AliveMonsterCount => aliveMonsters.Count;
    internal MapGeneratorBehaviour ActiveMap =>
        pathfindingService != null ? pathfindingService.ActiveMap : null;

    public event Action OnMonsterResolutionCompleted;
    public event Action<MonsterBehaviour> OnMonsterRegistered;

    public IReadOnlyList<MonsterBehaviour> GetAliveMonsters()
    {
        return aliveMonsters;
    }

    public void BeginBattle()
    {
        isBattleActive = true;
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
        isBattleActive = false;
    }

    public void ForceCleanupAllMonsters()
    {
        List<MonsterBehaviour> snapshot = new List<MonsterBehaviour>(aliveMonsters);

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

        aliveMonsters.Add(monster);
        monster.OnResolved += HandleMonsterResolved;
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
        if (!isBattleActive || monster == null || monster.IsDead())
        {
            return;
        }

        if (!aliveMonsters.Contains(monster))
        {
            return;
        }

        if (pathfindingService == null)
        {
            Debug.LogWarning("Monster manager cannot recalculate path: pathfinding service is not assigned.", this);
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

        List<GridNodeBehaviour> path = pathfindingService.FindPath(currentNode, targetNode);

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
                out failureReason))
        {
            return false;
        }

        if (!TryBuildAuthoritativeRemainingDistances(
                topologyPlan.AuthoritativeRoute,
                activeMap,
                out float[] remainingDistances))
        {
            failureReason =
                "the authoritative route contains an invalid Map-local centerline.";
            return false;
        }

        HashSet<GridNodeBehaviour> footprint =
            new HashSet<GridNodeBehaviour>(topologyPlan.Footprint);
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

            if (!IsAffected(snapshot, footprint))
            {
                continue;
            }

            int projectionIndex = SelectProjectionIndex(
                snapshot,
                topologyPlan.AuthoritativeRoute,
                remainingDistances,
                activeMap,
                out bool usedDeterministicFallback);
            GridNodeBehaviour projectionNode =
                topologyPlan.AuthoritativeRoute[projectionIndex];
            GridNodeBehaviour activeNextNode =
                topologyPlan.AuthoritativeRoute[projectionIndex + 1];
            List<GridNodeBehaviour> routeSlice = new List<GridNodeBehaviour>(
                topologyPlan.AuthoritativeRoute.Count - projectionIndex);

            for (int routeIndex = projectionIndex;
                 routeIndex < topologyPlan.AuthoritativeRoute.Count;
                 routeIndex++)
            {
                routeSlice.Add(topologyPlan.AuthoritativeRoute[routeIndex]);
            }

            Vector3 projectedWorldPosition =
                monster.ResolveMovementTargetPosition(projectionNode);
            float worldDisplacementDistance =
                snapshot.HasComparableWorldPosition
                    ? Vector3.Distance(
                        snapshot.WorldPosition,
                        projectedWorldPosition)
                    : 0f;
            float remainingDistanceDelta =
                snapshot.HasComparableRemainingDistance
                    ? remainingDistances[projectionIndex] -
                      snapshot.RemainingCenterlineDistance
                    : 0f;

            entries.Add(new MonsterRouteRevisionEntry(
                monster,
                projectionNode,
                activeNextNode,
                targetNode,
                routeSlice,
                routeIndex: 1,
                projectedWorldPosition,
                snapshot.WorldPosition,
                worldDisplacementDistance,
                remainingDistanceDelta,
                snapshot.HasComparableRemainingDistance,
                usedDeterministicFallback));
        }

        revisionBatch = new MonsterRouteRevisionBatch(entries);
        failureReason = string.Empty;
        return true;
    }

    internal void ApplyPreparedMovementRevisionBatch(
        MonsterRouteRevisionBatch revisionBatch)
    {
        IReadOnlyList<MonsterRouteRevisionEntry> entries = revisionBatch.Entries;

        for (int i = 0; i < entries.Count; i++)
        {
            MonsterRouteRevisionEntry entry = entries[i];
            entry.Monster.ApplyPreparedMovementRevision(entry);
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
        IReadOnlyList<GridNodeBehaviour> route =
            topologyPlan.AuthoritativeRoute;

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
                "the authoritative route is not a complete Spawn-to-Target route " +
                "with an eligible non-Target projection Grid.";
            return false;
        }

        for (int i = 0; i < topologyPlan.Footprint.Count; i++)
        {
            if (topologyPlan.Footprint[i] == null)
            {
                failureReason = "the topology plan footprint contains a null Grid Node.";
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
                    "the authoritative route contains a Grid Node outside the Active Map.";
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    private static bool IsAffected(
        MonsterMovementSnapshot snapshot,
        HashSet<GridNodeBehaviour> footprint)
    {
        if (!snapshot.IsValid ||
            footprint.Contains(snapshot.ReachedNode) ||
            footprint.Contains(snapshot.ActiveNextNode))
        {
            return true;
        }

        for (int i = snapshot.RouteIndex;
             i < snapshot.CurrentRoute.Count;
             i++)
        {
            if (footprint.Contains(snapshot.CurrentRoute[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static int SelectProjectionIndex(
        MonsterMovementSnapshot snapshot,
        IReadOnlyList<GridNodeBehaviour> authoritativeRoute,
        IReadOnlyList<float> remainingDistances,
        MapGeneratorBehaviour activeMap,
        out bool usedDeterministicFallback)
    {
        usedDeterministicFallback = !snapshot.HasComparableWorldPosition;

        if (usedDeterministicFallback ||
            !TryGetMapLocalXz(
                activeMap,
                snapshot.WorldPosition,
                out Vector2 monsterPosition))
        {
            usedDeterministicFallback = true;
            return 0;
        }

        int bestIndex = 0;
        float bestDistanceSquared = float.PositiveInfinity;
        float bestRemainingDistanceDelta = float.PositiveInfinity;
        bool bestAdvancesForFree = false;

        for (int i = 0; i < authoritativeRoute.Count - 1; i++)
        {
            if (!TryGetMapLocalXz(
                    activeMap,
                    authoritativeRoute[i].WorldPosition,
                    out Vector2 candidatePosition))
            {
                usedDeterministicFallback = true;
                return 0;
            }

            float distanceSquared =
                (candidatePosition - monsterPosition).sqrMagnitude;
            float remainingDistanceDelta =
                snapshot.HasComparableRemainingDistance
                    ? Mathf.Abs(
                        remainingDistances[i] -
                        snapshot.RemainingCenterlineDistance)
                    : 0f;
            bool advancesForFree =
                snapshot.HasComparableRemainingDistance &&
                remainingDistances[i] <
                snapshot.RemainingCenterlineDistance - ComparisonEpsilon;

            if (i == 0 ||
                IsBetterProjectionCandidate(
                    distanceSquared,
                    remainingDistanceDelta,
                    advancesForFree,
                    bestDistanceSquared,
                    bestRemainingDistanceDelta,
                    bestAdvancesForFree))
            {
                bestIndex = i;
                bestDistanceSquared = distanceSquared;
                bestRemainingDistanceDelta = remainingDistanceDelta;
                bestAdvancesForFree = advancesForFree;
            }
        }

        return bestIndex;
    }

    private static bool IsBetterProjectionCandidate(
        float candidateDistanceSquared,
        float candidateRemainingDistanceDelta,
        bool candidateAdvancesForFree,
        float bestDistanceSquared,
        float bestRemainingDistanceDelta,
        bool bestAdvancesForFree)
    {
        if (candidateDistanceSquared < bestDistanceSquared - ComparisonEpsilon)
        {
            return true;
        }

        if (Mathf.Abs(candidateDistanceSquared - bestDistanceSquared) >
            ComparisonEpsilon)
        {
            return false;
        }

        if (candidateRemainingDistanceDelta <
            bestRemainingDistanceDelta - ComparisonEpsilon)
        {
            return true;
        }

        if (Mathf.Abs(
                candidateRemainingDistanceDelta -
                bestRemainingDistanceDelta) > ComparisonEpsilon)
        {
            return false;
        }

        return bestAdvancesForFree && !candidateAdvancesForFree;
    }

    private static bool TryBuildAuthoritativeRemainingDistances(
        IReadOnlyList<GridNodeBehaviour> route,
        MapGeneratorBehaviour activeMap,
        out float[] remainingDistances)
    {
        remainingDistances = new float[route.Count];

        for (int i = route.Count - 2; i >= 0; i--)
        {
            if (!TryGetMapLocalXz(
                    activeMap,
                    route[i].WorldPosition,
                    out Vector2 fromPosition) ||
                !TryGetMapLocalXz(
                    activeMap,
                    route[i + 1].WorldPosition,
                    out Vector2 toPosition))
            {
                remainingDistances = Array.Empty<float>();
                return false;
            }

            remainingDistances[i] =
                Vector2.Distance(fromPosition, toPosition) +
                remainingDistances[i + 1];
        }

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
            }
        }

        aliveMonsters.Clear();
    }

    private void HandleMonsterResolved(MonsterBehaviour monster, bool reachedTarget)
    {
        UnregisterMonster(monster);

        if (!isBattleActive || playerSystem == null)
        {
            return;
        }

        if (playerSystem.TryResolveMonster(reachedTarget))
        {
            OnMonsterResolutionCompleted?.Invoke();
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
