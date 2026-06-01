using System.Collections.Generic;
using UnityEngine;

public class TowerInstance : MonoBehaviour
{
    private TowerDefinition towerDefinition;

    private List<GridNodeBehaviour> occupiedNodes = new List<GridNodeBehaviour>();

    public TowerDefinition TowerDefinition => towerDefinition;
    public IReadOnlyList<GridNodeBehaviour> OccupiedNodes => occupiedNodes;

    public void Initialize(TowerDefinition towerDefinition, List<GridNodeBehaviour> occupiedNodes)
    {
        this.towerDefinition = towerDefinition;
        this.occupiedNodes.Clear();

        if (occupiedNodes == null)
        {
            return;
        }

        HashSet<GridNodeBehaviour> uniqueNodes = new HashSet<GridNodeBehaviour>();

        for (int i = 0; i < occupiedNodes.Count; i++)
        {
            GridNodeBehaviour node = occupiedNodes[i];

            if (node != null && uniqueNodes.Add(node))
            {
                this.occupiedNodes.Add(node);
            }
        }
    }

    public IReadOnlyList<GridNodeBehaviour> GetOccupiedNodes()
    {
        return occupiedNodes;
    }
}
