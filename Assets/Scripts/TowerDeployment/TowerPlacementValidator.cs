using System.Collections.Generic;
using UnityEngine;

public class TowerPlacementValidator : MonoBehaviour
{
    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private MonsterManager monsterManager;

    private MapGeneratorBehaviour mapGenerator;

    public void Initialize(MapGeneratorBehaviour mapGenerator)
    {
        this.mapGenerator = mapGenerator;
    }

    public void Initialize(
        MapGeneratorBehaviour mapGenerator,
        AStarPathfindingService pathfindingService,
        MonsterManager monsterManager)
    {
        this.mapGenerator = mapGenerator;
        this.pathfindingService = pathfindingService;
        this.monsterManager = monsterManager;
    }

    public bool TryGetOccupiedNodes(TowerPlacementPreview preview, out List<GridNodeBehaviour> occupiedNodes)
    {
        occupiedNodes = new List<GridNodeBehaviour>();

        if (preview == null || mapGenerator == null)
        {
            return false;
        }

        TowerAnchorSet anchorSet = preview.TowerAnchorSet;

        if (anchorSet == null || !anchorSet.IsValid())
        {
            return false;
        }

        IReadOnlyList<Transform> occupiedAnchors = anchorSet.OccupiedAnchors;
        HashSet<GridNodeBehaviour> uniqueNodes = new HashSet<GridNodeBehaviour>();

        for (int i = 0; i < occupiedAnchors.Count; i++)
        {
            Transform occupiedAnchor = occupiedAnchors[i];

            if (occupiedAnchor == null)
            {
                occupiedNodes.Clear();
                return false;
            }

            if (!mapGenerator.TryGetNodeByWorldPosition(occupiedAnchor.position, out GridNodeBehaviour node))
            {
                occupiedNodes.Clear();
                return false;
            }

            if (uniqueNodes.Add(node))
            {
                occupiedNodes.Add(node);
            }
        }

        return occupiedNodes.Count > 0;
    }

    public bool CanPlaceTower(TowerPlacementPreview preview, out List<GridNodeBehaviour> occupiedNodes)
    {
        if (!TryGetOccupiedNodes(preview, out occupiedNodes))
        {
            return false;
        }

        for (int i = 0; i < occupiedNodes.Count; i++)
        {
            GridNodeBehaviour node = occupiedNodes[i];

            if (node == null || !node.IsWalkable)
            {
                return false;
            }
        }

        return ValidatePathBlocking(occupiedNodes);
    }

    private bool ValidatePathBlocking(List<GridNodeBehaviour> occupiedNodes)
    {
        if (pathfindingService == null || mapGenerator == null)
        {
            return false;
        }

        GridNodeBehaviour spawnNode = mapGenerator.GetSpawnNode();
        GridNodeBehaviour targetNode = mapGenerator.GetTargetNode();

        if (spawnNode == null || targetNode == null)
        {
            return false;
        }

        HashSet<GridNodeBehaviour> temporaryBlockedNodes = new HashSet<GridNodeBehaviour>();

        for (int i = 0; i < occupiedNodes.Count; i++)
        {
            GridNodeBehaviour occupiedNode = occupiedNodes[i];

            if (occupiedNode == null)
            {
                return false;
            }

            temporaryBlockedNodes.Add(occupiedNode);
        }

        if (!pathfindingService.HasValidPath(spawnNode, targetNode, temporaryBlockedNodes))
        {
            return false;
        }

        if (monsterManager == null)
        {
            return true;
        }

        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (monster == null || monster.IsDead())
            {
                continue;
            }

            GridNodeBehaviour monsterCurrentNode = monster.GetCurrentNode();

            if (monsterCurrentNode == null)
            {
                continue;
            }

            if (temporaryBlockedNodes.Contains(monsterCurrentNode))
            {
                return false;
            }

            if (!pathfindingService.HasValidPath(monsterCurrentNode, targetNode, temporaryBlockedNodes))
            {
                return false;
            }
        }

        return true;
    }
}
