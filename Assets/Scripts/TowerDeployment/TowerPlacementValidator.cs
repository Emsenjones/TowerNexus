using System.Collections.Generic;
using UnityEngine;

public class TowerPlacementValidator : MonoBehaviour
{
    private MapGeneratorBehaviour mapGenerator;

    public void Initialize(MapGeneratorBehaviour mapGenerator)
    {
        this.mapGenerator = mapGenerator;
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
        return true;
    }
}
