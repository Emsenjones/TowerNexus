using System.Collections.Generic;
using UnityEngine;

internal sealed class PreparedRouteConnectivityMap
{
    private readonly Dictionary<GridNodeBehaviour, int> distanceByNode;
    private readonly Dictionary<GridNodeBehaviour, GridNodeBehaviour> joinByNode;
    private readonly Dictionary<GridNodeBehaviour, GridNodeBehaviour> nextByNode;

    internal PreparedRouteConnectivityMap(
        Dictionary<GridNodeBehaviour, int> distanceByNode,
        Dictionary<GridNodeBehaviour, GridNodeBehaviour> joinByNode,
        Dictionary<GridNodeBehaviour, GridNodeBehaviour> nextByNode)
    {
        this.distanceByNode = distanceByNode;
        this.joinByNode = joinByNode;
        this.nextByNode = nextByNode;
    }

    internal bool Contains(GridNodeBehaviour node)
    {
        return node != null && distanceByNode.ContainsKey(node);
    }

    internal bool TryGetConnection(
        GridNodeBehaviour node,
        out int distance,
        out GridNodeBehaviour joinNode)
    {
        joinNode = null;

        if (node == null ||
            !distanceByNode.TryGetValue(node, out distance) ||
            !joinByNode.TryGetValue(node, out joinNode))
        {
            distance = 0;
            return false;
        }

        return joinNode != null;
    }

    internal List<GridNodeBehaviour> BuildPathToJoin(GridNodeBehaviour startNode)
    {
        List<GridNodeBehaviour> path = new List<GridNodeBehaviour>();

        if (!Contains(startNode))
        {
            return path;
        }

        GridNodeBehaviour current = startNode;
        path.Add(current);
        int remainingGuard = distanceByNode.Count + 1;

        while (remainingGuard-- > 0 &&
               nextByNode.TryGetValue(current, out GridNodeBehaviour next) &&
               next != null)
        {
            current = next;
            path.Add(current);
        }

        return remainingGuard >= 0 &&
               joinByNode.TryGetValue(startNode, out GridNodeBehaviour join) &&
               current == join
            ? path
            : new List<GridNodeBehaviour>();
    }
}

public class AStarPathfindingService : MonoBehaviour
{
    private MapGeneratorBehaviour mapGenerator;

    public MapGeneratorBehaviour ActiveMap => mapGenerator;
    public bool HasActiveMap => mapGenerator != null;
    public ulong BindingRevision { get; private set; }
#if UNITY_EDITOR
    public int SearchCount { get; private set; }
#endif

    public bool BindActiveMap(MapGeneratorBehaviour activeMap)
    {
        if (activeMap == null)
        {
            Debug.LogError("A* pathfinding service cannot bind a null Active Map.", this);
            return false;
        }

        BindingRevision++;
        mapGenerator = activeMap;
        return true;
    }

    public void ClearActiveMap()
    {
        BindingRevision++;
        mapGenerator = null;
    }

    public List<GridNodeBehaviour> FindPath(
        GridNodeBehaviour startNode,
        GridNodeBehaviour targetNode)
    {
        return FindPath(startNode, targetNode, null);
    }

    public bool HasValidPath(
        GridNodeBehaviour startNode,
        GridNodeBehaviour targetNode)
    {
        return FindPath(startNode, targetNode).Count > 0;
    }

    public bool HasValidPath(
        GridNodeBehaviour startNode,
        GridNodeBehaviour targetNode,
        IReadOnlyCollection<GridNodeBehaviour> temporaryBlockedNodes)
    {
        return FindPath(startNode, targetNode, temporaryBlockedNodes).Count > 0;
    }

    public List<GridNodeBehaviour> FindPath(
        GridNodeBehaviour startNode,
        GridNodeBehaviour targetNode,
        IReadOnlyCollection<GridNodeBehaviour> temporaryBlockedNodes)
    {
#if UNITY_EDITOR
        SearchCount++;
#endif
        if (mapGenerator == null)
        {
            Debug.LogWarning("A* pathfinding service cannot find path: map generator is not assigned.", this);
            return new List<GridNodeBehaviour>();
        }

        HashSet<GridNodeBehaviour> blockedNodes = temporaryBlockedNodes != null
            ? new HashSet<GridNodeBehaviour>(temporaryBlockedNodes)
            : new HashSet<GridNodeBehaviour>();

        if (!IsNodePathable(startNode, blockedNodes) || !IsNodePathable(targetNode, blockedNodes))
        {
            return new List<GridNodeBehaviour>();
        }

        if (startNode == targetNode)
        {
            return new List<GridNodeBehaviour> { startNode };
        }

        List<GridNodeBehaviour> openSet = new List<GridNodeBehaviour> { startNode };
        HashSet<GridNodeBehaviour> closedSet = new HashSet<GridNodeBehaviour>();
        Dictionary<GridNodeBehaviour, GridNodeBehaviour> cameFrom = new Dictionary<GridNodeBehaviour, GridNodeBehaviour>();
        Dictionary<GridNodeBehaviour, int> gScore = new Dictionary<GridNodeBehaviour, int>
        {
            [startNode] = 0
        };
        Dictionary<GridNodeBehaviour, int> fScore = new Dictionary<GridNodeBehaviour, int>
        {
            [startNode] = GetHeuristicCost(startNode, targetNode)
        };

        while (openSet.Count > 0)
        {
            GridNodeBehaviour currentNode = GetLowestCostNode(openSet, fScore);

            if (currentNode == targetNode)
            {
                return BuildPath(cameFrom, currentNode);
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            List<GridNodeBehaviour> neighbors = mapGenerator.GetNeighborNodes(currentNode.GridPosition);

            for (int i = 0; i < neighbors.Count; i++)
            {
                GridNodeBehaviour neighbor = neighbors[i];

                if (!IsNodePathable(neighbor, blockedNodes) || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int tentativeGScore = GetScore(gScore, currentNode) + 1;

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= GetScore(gScore, neighbor))
                {
                    continue;
                }

                cameFrom[neighbor] = currentNode;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = tentativeGScore + GetHeuristicCost(neighbor, targetNode);
            }
        }

        return new List<GridNodeBehaviour>();
    }

    internal bool TryBuildRouteConnectivityMap(
        IReadOnlyList<GridNodeBehaviour> authoritativeRoute,
        IReadOnlyCollection<GridNodeBehaviour> temporaryBlockedNodes,
        out PreparedRouteConnectivityMap connectivityMap)
    {
        connectivityMap = null;

        if (mapGenerator == null ||
            authoritativeRoute == null ||
            authoritativeRoute.Count < 2)
        {
            return false;
        }

        HashSet<GridNodeBehaviour> blockedNodes = temporaryBlockedNodes != null
            ? new HashSet<GridNodeBehaviour>(temporaryBlockedNodes)
            : new HashSet<GridNodeBehaviour>();
        Dictionary<GridNodeBehaviour, int> routeIndexByNode =
            new Dictionary<GridNodeBehaviour, int>();
        Dictionary<GridNodeBehaviour, int> distanceByNode =
            new Dictionary<GridNodeBehaviour, int>();
        Dictionary<GridNodeBehaviour, GridNodeBehaviour> joinByNode =
            new Dictionary<GridNodeBehaviour, GridNodeBehaviour>();
        Dictionary<GridNodeBehaviour, GridNodeBehaviour> nextByNode =
            new Dictionary<GridNodeBehaviour, GridNodeBehaviour>();
        Queue<GridNodeBehaviour> open = new Queue<GridNodeBehaviour>();

        for (int i = 0; i < authoritativeRoute.Count - 1; i++)
        {
            GridNodeBehaviour routeNode = authoritativeRoute[i];

            if (!IsNodePathable(routeNode, blockedNodes))
            {
                return false;
            }

            routeIndexByNode[routeNode] = i;
            distanceByNode[routeNode] = 0;
            joinByNode[routeNode] = routeNode;
            nextByNode[routeNode] = null;
            open.Enqueue(routeNode);
        }

        while (open.Count > 0)
        {
            GridNodeBehaviour current = open.Dequeue();
            int candidateDistance = distanceByNode[current] + 1;
            GridNodeBehaviour candidateJoin = joinByNode[current];
            List<GridNodeBehaviour> neighbors =
                mapGenerator.GetNeighborNodes(current.GridPosition);

            for (int i = 0; i < neighbors.Count; i++)
            {
                GridNodeBehaviour neighbor = neighbors[i];

                if (!IsNodePathable(neighbor, blockedNodes))
                {
                    continue;
                }

                bool shouldReplace =
                    !distanceByNode.TryGetValue(neighbor, out int currentDistance) ||
                    candidateDistance < currentDistance;

                if (!shouldReplace && candidateDistance == currentDistance)
                {
                    GridNodeBehaviour currentJoin = joinByNode[neighbor];
                    shouldReplace = IsEarlierRouteJoin(
                        candidateJoin,
                        currentJoin,
                        routeIndexByNode);
                }

                if (!shouldReplace)
                {
                    continue;
                }

                distanceByNode[neighbor] = candidateDistance;
                joinByNode[neighbor] = candidateJoin;
                nextByNode[neighbor] = current;
                open.Enqueue(neighbor);
            }
        }

        connectivityMap = new PreparedRouteConnectivityMap(
            distanceByNode,
            joinByNode,
            nextByNode);
        return true;
    }

    private static bool IsEarlierRouteJoin(
        GridNodeBehaviour candidate,
        GridNodeBehaviour current,
        IReadOnlyDictionary<GridNodeBehaviour, int> routeIndexByNode)
    {
        int candidateIndex = routeIndexByNode.TryGetValue(candidate, out int left)
            ? left
            : int.MaxValue;
        int currentIndex = routeIndexByNode.TryGetValue(current, out int right)
            ? right
            : int.MaxValue;

        if (candidateIndex != currentIndex)
        {
            return candidateIndex < currentIndex;
        }

        Vector2Int candidateGrid = candidate.GridPosition;
        Vector2Int currentGrid = current.GridPosition;
        return candidateGrid.x != currentGrid.x
            ? candidateGrid.x < currentGrid.x
            : candidateGrid.y < currentGrid.y;
    }

    private bool IsNodePathable(
        GridNodeBehaviour node,
        HashSet<GridNodeBehaviour> temporaryBlockedNodes)
    {
        return node != null &&
               mapGenerator.GetNode(node.GridPosition) == node &&
               node.IsWalkable &&
               (temporaryBlockedNodes == null || !temporaryBlockedNodes.Contains(node));
    }

    private static GridNodeBehaviour GetLowestCostNode(
        List<GridNodeBehaviour> nodes,
        Dictionary<GridNodeBehaviour, int> fScore)
    {
        GridNodeBehaviour bestNode = nodes[0];
        int bestScore = GetScore(fScore, bestNode);

        for (int i = 1; i < nodes.Count; i++)
        {
            GridNodeBehaviour candidate = nodes[i];
            int candidateScore = GetScore(fScore, candidate);

            if (candidateScore >= bestScore)
            {
                continue;
            }

            bestNode = candidate;
            bestScore = candidateScore;
        }

        return bestNode;
    }

    private static int GetScore(Dictionary<GridNodeBehaviour, int> scores, GridNodeBehaviour node)
    {
        return scores.TryGetValue(node, out int score) ? score : int.MaxValue;
    }

    private static int GetHeuristicCost(GridNodeBehaviour fromNode, GridNodeBehaviour toNode)
    {
        Vector2Int from = fromNode.GridPosition;
        Vector2Int to = toNode.GridPosition;

        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }

    private static List<GridNodeBehaviour> BuildPath(
        Dictionary<GridNodeBehaviour, GridNodeBehaviour> cameFrom,
        GridNodeBehaviour currentNode)
    {
        List<GridNodeBehaviour> path = new List<GridNodeBehaviour> { currentNode };

        while (cameFrom.TryGetValue(currentNode, out GridNodeBehaviour previousNode))
        {
            currentNode = previousNode;
            path.Add(currentNode);
        }

        path.Reverse();
        return path;
    }
}
