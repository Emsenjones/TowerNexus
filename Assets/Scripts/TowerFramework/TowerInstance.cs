using System.Collections.Generic;
using UnityEngine;

public class TowerInstance : MonoBehaviour
{
    private const int DefaultLevel = 1;

    private TowerDefinition towerDefinition;
    private int currentLevel = DefaultLevel;

    private List<GridNodeBehaviour> occupiedNodes = new List<GridNodeBehaviour>();

    public TowerDefinition TowerDefinition => towerDefinition;
    public int CurrentLevel => currentLevel;
    public TowerLevelConfig CurrentLevelConfig => towerDefinition != null ? towerDefinition.GetLevelConfig(currentLevel) : null;
    public int BasicDamage => CurrentLevelConfig != null ? CurrentLevelConfig.BasicDamage : 0;
    public IReadOnlyList<GridNodeBehaviour> OccupiedNodes => occupiedNodes;

    public void Initialize(TowerDefinition towerDefinition, List<GridNodeBehaviour> occupiedNodes)
    {
        this.towerDefinition = towerDefinition;
        currentLevel = DefaultLevel;
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

    public int GetMaxConfiguredLevel()
    {
        if (towerDefinition == null || towerDefinition.TowerLevelConfigs == null)
        {
            return 0;
        }

        int maxLevel = 0;
        IReadOnlyList<TowerLevelConfig> levelConfigs = towerDefinition.TowerLevelConfigs;

        for (int i = 0; i < levelConfigs.Count; i++)
        {
            TowerLevelConfig levelConfig = levelConfigs[i];

            if (levelConfig != null && levelConfig.Level > maxLevel)
            {
                maxLevel = levelConfig.Level;
            }
        }

        return maxLevel;
    }

    public bool CanSetLevel(int level)
    {
        return towerDefinition != null &&
               level > 0 &&
               towerDefinition.GetLevelConfig(level) != null;
    }

    public bool TrySetLevel(int level)
    {
        if (!CanSetLevel(level))
        {
            return false;
        }

        currentLevel = level;
        return true;
    }

    public IReadOnlyList<GridNodeBehaviour> GetOccupiedNodes()
    {
        return occupiedNodes;
    }
}
