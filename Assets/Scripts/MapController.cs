using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class MapController : MonoBehaviour
{
    [Serializable]public class GridConfig
    {
        [SerializeField]
        private int index;
        public int Index => index;

        [SerializeField]
        Sprite sprite;
        public Sprite Sprite
        {
            get { return sprite; }
        }

        [SerializeField] string description;
    }
    [FormerlySerializedAs("gridList")]
    [FormerlySerializedAs("gridInfoList")]
    [Title("Configs")]
    [SerializeField] List<GridConfig> gridConfigList;
    List<GridBehaviour> _gridList;
    public void Initialize()
    {
        _gridList = GetComponentsInChildren<GridBehaviour>().ToList();
        UpdateGrids();
    }

    /// <summary>
    /// Generate a path for the enemy.
    /// </summary>
    /// <param name="monsterTransform">The monster's transform</param>
    /// <returns> A list of transform. Will return an empty list if the monster cannot find a path.</returns>
    public List<Transform> FindOnePath(Transform monsterTransform)
    {
        if(_gridList == null) return null;
        GridBehaviour targetGrid = GetTargetGrid();

        if (targetGrid == null)
        {
            Debug.LogError("No Target Node found in map.");
            return null;
        }

        GridBehaviour startGrid = _gridList
            .Where(n => n.IsWalkable)
            .OrderBy(n => Vector3.Distance(monsterTransform.position, n.transform.position))
            .FirstOrDefault();

        if (startGrid == null)
        {
            Debug.LogError("No valid start node found.");
            return null;
        }
        _debugPathList = AStar(startGrid, targetGrid, _gridList)
            .Select(n => n.transform)
            .ToList();

        return _debugPathList;
    } 
    /// <summary>
    /// if nodeBehaviours have changed, call this function to update their sprites.
    /// </summary>
    void UpdateGrids()
    {
        float gridSpacing = GetGridSpacing();
        if (gridSpacing <= 0f)
        {
            Debug.LogError("Grid spacing is invalid.");
            return;
        }
        var nodeList = GetComponentsInChildren<GridBehaviour>().ToList();
        if (!nodeList.Any())
        {
            Debug.LogError("Cannot find any NodeBehaviour in " + gameObject.name+" .");
            return;
        }

        foreach (var node in nodeList)
        {
            var spriteRenderer = node.transform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"No SpriteRenderer found on Node at {node.gameObject.name} .");
                continue;
            }
            if (!node.IsWalkable)
            {
                spriteRenderer.sprite = null;
                continue;
            }

            Vector3 pos = node.transform.position;
            int index = 0;
            foreach (var neighbor in nodeList)
            {
                if (!neighbor.IsWalkable || neighbor == node)
                    continue;

                Vector3 dir = neighbor.transform.position - pos;

                // 排除非正交方向（如斜对角）
                if (Mathf.Abs(dir.x) > 0.1f && Mathf.Abs(dir.y) > 0.1f)
                    continue;

                // 判断上（y+）
                if (Mathf.Abs(dir.x) < 0.1f && Mathf.Abs(dir.y - gridSpacing) < 0.1f)
                {
                    index += 1;
                }
                // 判断下（y-）
                else if (Mathf.Abs(dir.x) < 0.1f && Mathf.Abs(dir.y + gridSpacing) < 0.1f)
                {
                    index += 2;
                }
                // 判断左（x-）
                else if (Mathf.Abs(dir.y) < 0.1f && Mathf.Abs(dir.x + gridSpacing) < 0.1f)
                {
                    index += 4;
                }
                // 判断右（x+）
                else if (Mathf.Abs(dir.y) < 0.1f && Mathf.Abs(dir.x - gridSpacing) < 0.1f)
                {
                    index += 8;
                }
            }

            spriteRenderer.sprite = GetGridSprite(index);
        }
    }
    public GridBehaviour GetTargetGrid()
    {
        return _gridList?.FirstOrDefault(n => n.ThisType == GridBehaviour.Type.Target);
    }

    #region Support methods.
    /// <summary>
    /// Get Grid sprite via the index.
    /// </summary>
    /// <param name="index">the index number</param>
    /// <returns></returns>
    Sprite GetGridSprite(int index)
    {
        var match = gridConfigList.FirstOrDefault(item => item.Index == index);
        if (match != null)
            return match.Sprite;

        Debug.LogWarning($"No sprite found for road index {index}");
        return null;
    }


    
    float GetGridSpacing()
    {
        var nodeList = GetComponentsInChildren<GridBehaviour>().ToList();
        if (nodeList.Count < 2)
            return 0f;

        // 找到两个不同位置的节点
        for (int i = 0; i < nodeList.Count - 1; i++)
        {
            for (int j = i + 1; j < nodeList.Count; j++)
            {
                float dist = Vector3.Distance(nodeList[i].transform.position, nodeList[j].transform.position);
                if (dist > 0.01f)
                {
                    return Mathf.Round(dist * 100f) / 100f; // 保留两位小数
                }
            }
        }

        return 0f;
    } 

    List<GridBehaviour> AStar(GridBehaviour start, GridBehaviour goal, List<GridBehaviour> allNodes)
    {
        var openSet = new List<GridBehaviour> { start };
        var cameFrom = new Dictionary<GridBehaviour, GridBehaviour>();
        var gScore = new Dictionary<GridBehaviour, float>();
        var fScore = new Dictionary<GridBehaviour, float>();

        foreach (var node in allNodes)
        {
            gScore[node] = float.MaxValue;
            fScore[node] = float.MaxValue;
        }

        gScore[start] = 0;
        fScore[start] = Vector3.Distance(start.transform.position, goal.transform.position);

        while (openSet.Count > 0)
        {
            GridBehaviour current = openSet.OrderBy(n => fScore[n]).First();

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(current, allNodes))
            {
                if (!neighbor.IsWalkable) continue;

                float tentativeGScore = gScore[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);

                if (tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Vector3.Distance(neighbor.transform.position, goal.transform.position);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        Debug.LogWarning("No path found.");
        return new List<GridBehaviour>(); // 找不到路径时返回空列表
    }

    List<GridBehaviour> GetNeighbors(GridBehaviour grid, List<GridBehaviour> allNodes)
    {
        List<GridBehaviour> neighbors = new List<GridBehaviour>();
        float gridSpacing = GetGridSpacing();
        Vector3 pos = grid.transform.position;

        foreach (var other in allNodes)
        {
            if (other == grid || !other.IsWalkable) continue;

            Vector3 dir = other.transform.position - pos;

            // 仅支持 4 向（上下左右），非对角
            if ((Mathf.Abs(dir.x - gridSpacing) < 0.1f && Mathf.Abs(dir.y) < 0.1f) ||
                (Mathf.Abs(dir.x + gridSpacing) < 0.1f && Mathf.Abs(dir.y) < 0.1f) ||
                (Mathf.Abs(dir.y - gridSpacing) < 0.1f && Mathf.Abs(dir.x) < 0.1f) ||
                (Mathf.Abs(dir.y + gridSpacing) < 0.1f && Mathf.Abs(dir.x) < 0.1f))
            {
                neighbors.Add(other);
            }
        }

        return neighbors;
    }
    private List<GridBehaviour> ReconstructPath(Dictionary<GridBehaviour, GridBehaviour> cameFrom, GridBehaviour current)
    {
        List<GridBehaviour> path = new List<GridBehaviour> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        
        return path;
    }

    List<Transform> _debugPathList; //For drawing Gizmos.

    void OnDrawGizmosSelected()
    {
        if (_debugPathList == null || _debugPathList.Count == 0) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < _debugPathList.Count; i++)
        {
            if (_debugPathList[i] != null)
            {
                Gizmos.DrawSphere(_debugPathList[i].position, 0.15f);

                if (i < _debugPathList.Count - 1 && _debugPathList[i + 1] != null)
                {
                    Gizmos.DrawLine(_debugPathList[i].position, _debugPathList[i + 1].position);
                }
            }
        }
    } //The function of drawing Gizmos.
#if UNITY_EDITOR
    private void OnValidate()
    {
        var targets = GetComponentsInChildren<GridBehaviour>()
            .Where(n => n.ThisType == GridBehaviour.Type.Target)
            .ToList();

        
        if (targets.Count > 1)
        {
            Debug.LogWarning($"[MapController] detected multiple target type, multiple count：{targets.Count}.");
        }
    }
#endif
    #endregion

}


