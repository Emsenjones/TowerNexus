using System.Collections.Generic;
using UnityEngine;

public class AStarPathfindingService : MonoBehaviour
{
    [SerializeField] private MapGeneratorBehaviour mapGenerator;

    public void Initialize(MapGeneratorBehaviour mapGenerator)
    {
        this.mapGenerator = mapGenerator;
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

    private List<GridNodeBehaviour> FindPath(
        GridNodeBehaviour startNode,
        GridNodeBehaviour targetNode,
        IReadOnlyCollection<GridNodeBehaviour> temporaryBlockedNodes)
    {
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
