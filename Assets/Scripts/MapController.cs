using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
public class MapController : MonoBehaviour
{
    [Serializable] public class GridConfig
    {
        [SerializeField]
        int index;
        public int Index
        {
            get {
                return index;
            }
        }

        [SerializeField]
        Sprite sprite;
        public Sprite Sprite
        {
            get { return sprite; }
        }

        [SerializeField] string description;
    }
    [Title("Configs")]
    [SerializeField] List<GridConfig> gridConfigList;
    [SerializeField] GridBehaviour destinationGrid;
    List<GridBehaviour> spawnGridList;
    List<GridBehaviour> gridList;
    public void Initialize(List<GridBehaviour> spawnGridBehaviourList)
    {
        gridList = GetComponentsInChildren<GridBehaviour>().ToList();
        if (!gridList.Any())
        {
            Debug.Log($"{gameObject.name} doesn't have any gridBehaviours!");
            return;
        }
        gridList.ForEach(grid => grid.Initialize());
        spawnGridList = spawnGridBehaviourList;
        SynchronizeGridStates();
    }
    public void Dispose()
    {
        if (!gridList.Any())
        {
            Debug.Log($"{gameObject.name} doesn't have any gridBehaviours!");
            return;
        }
        gridList.ForEach(grid => grid.Dispose());
        gridList.Clear();
        
        spawnGridList.Clear();
    }

    /// <summary>
    ///     Generate a path for the enemy.
    /// </summary>
    /// <param name="monsterTransform">The monster's transform</param>
    /// <returns> A list of transform. Will return an empty list if the monster cannot find a path.</returns>
    public List<Transform> FindOnePath(Transform monsterTransform)
    {
        if (monsterTransform == null)
        {
            Debug.LogError($"{gameObject.name} cannot find the transform of {monsterTransform.name}.");
            return null;
        }
        if (gridList == null || gridList.Count <= 0)
        {
            Debug.LogError($"{gameObject.name} cannot find GridBehaviour in its children.");
            return null;
        }

        if (destinationGrid == null)
        {
            Debug.LogError("No Target Node found in map.");
            return null;
        }

        GridBehaviour startGrid = gridList
            .Where(n => n.IsWalkable)
            .OrderBy(n => Vector3.Distance(monsterTransform.position, n.transform.position))
            .FirstOrDefault();

        if (startGrid == null)
        {
            Debug.LogError("No valid start node found.");
            return null;
        }
        debugPathList = AStar(startGrid, destinationGrid, gridList)
            .Select(n => n.transform)
            .ToList();

        return debugPathList;
    }
    /// <summary>
    ///     if nodeBehaviours have changed, call this function to update their sprites.
    /// </summary>
    void SynchronizeGridStates()
    {
        if (!gridList.Any())
        {
            Debug.LogError($"{gameObject.name} does's has any children who attach GridBehaviour!");
            return;
        }

        float gridSpacing = GetGridSpacing();
        if (gridSpacing <= 0f)
        {
            Debug.LogError("Grid spacing is invalid.");
            return;
        }

        foreach (GridBehaviour grid in gridList)
        {
            #region Set grid's visual.

            SpriteRenderer spriteRenderer = grid.transform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"No SpriteRenderer found on Grid at {grid.gameObject.name}.");
                continue;
            }

            Vector3 pos = grid.transform.position;
            int index = 0;

            foreach (GridBehaviour neighbor in gridList)
            {
                if (neighbor == grid) continue;

                Vector3 dir = neighbor.transform.position - pos;

                // 排除非正交方向（如斜对角）
                if (Mathf.Abs(dir.x) > 0.1f && Mathf.Abs(dir.y) > 0.1f)
                    continue;

                // ✅ 只与“同类节点”建立连接（即都 Walkable 或都非 Walkable）
                if (neighbor.IsWalkable != grid.IsWalkable) continue;

                // 上（y+）
                if (Mathf.Abs(dir.x) < 0.1f && Mathf.Abs(dir.y - gridSpacing) < 0.1f)
                    index += 1;

                // 下（y-）
                else if (Mathf.Abs(dir.x) < 0.1f && Mathf.Abs(dir.y + gridSpacing) < 0.1f)
                    index += 2;

                // 左（x-）
                else if (Mathf.Abs(dir.y) < 0.1f && Mathf.Abs(dir.x + gridSpacing) < 0.1f)
                    index += 4;

                // 右（x+）
                else if (Mathf.Abs(dir.y) < 0.1f && Mathf.Abs(dir.x - gridSpacing) < 0.1f)
                    index += 8;
            }

            spriteRenderer.sprite = GetGridSprite(index);

            #endregion
        }


    }
    public GridBehaviour GetClosestGrid(Vector2 targetPosition)
    {
        float closestDistance = float.MaxValue;
        GridBehaviour closestGrid = null;
        foreach (GridBehaviour grid in gridList)
        {
            float distance = Vector2.Distance(targetPosition, grid.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGrid = grid;
            }
        }
        return closestGrid;
    }
    public bool CanDeployTower(List<Transform> towerTransformList)
    {
        if (towerTransformList is not { Count: > 0 })
        {
            Debug.LogError($"cannot find any tower transforms in {towerTransformList.GetType()}!");
            return true;
        }


        // To get the relevant GridBehaviour via towerTransformList.
        var occupiedGridList = new List<GridBehaviour>();
        foreach (Transform tf in towerTransformList)
        {
            GridBehaviour grid = GetClosestGrid(tf.position);

            // Return false if it cannot find the relevant GridBehaviour.
            if (grid == null)
                return false;

            // Return false if the relevant GridBehaviour's IsWalkable = false.
            if (!grid.IsWalkable)
                return false;

            if (!occupiedGridList.Contains(grid))
                occupiedGridList.Add(grid);
        }

        // To back up the data of IsWalkable in each GridBehaviour.
        var originalState = new Dictionary<GridBehaviour, bool>();
        foreach (GridBehaviour grid in occupiedGridList)
        {
            originalState[grid] = grid.IsWalkable;
            grid.IsWalkable = false;
        }

        // To check if all paths that are from SpawnGrids to TargetGrids are accessible.
        bool allReachable = true;
        foreach (GridBehaviour spawn in spawnGridList)
        {
            if (spawn == null || !spawn.IsWalkable) continue;

            List<GridBehaviour> path = AStar(spawn, destinationGrid, gridList);
            if (path == null || path.Count == 0)
            {
                allReachable = false;
                break;
            }
        }

        // To reset the data of IsWalkable in each GridBehaviour.
        foreach (KeyValuePair<GridBehaviour, bool> kv in originalState)
            kv.Key.IsWalkable = kv.Value;

        return allReachable;
    }
    public void Deploy(List<Transform> towerTransformList)
    {
        if (towerTransformList is not { Count: > 0 })
        {
            Debug.LogError($"cannot find any tower transforms in {towerTransformList.GetType()}!");
            return;
        }

        foreach (Transform tf in towerTransformList)
        {
            GridBehaviour grid = GetClosestGrid(tf.position);

            if (grid == null)
            {
                Debug.LogError($"[MapController] No Grid found near position {tf.position}, cannot deploy here.");
                continue;
            }

            if (!grid.IsWalkable)
            {
                Debug.LogError($"[MapController] Grid at {grid.transform.position} is already occupied.");
                continue;
            }

            grid.IsWalkable = false;
        }

        SynchronizeGridStates();
        OnDeployOneTower?.Invoke();
    }
    public static event Action OnDeployOneTower;

    #region Support methods.

    /// <summary>
    ///     Get Grid sprite via the index.
    /// </summary>
    /// <param name="index">the index number</param>
    /// <returns></returns>
    Sprite GetGridSprite(int index)
    {
        GridConfig match = gridConfigList.FirstOrDefault(item => item.Index == index);
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

        foreach (GridBehaviour node in allNodes)
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

            foreach (GridBehaviour neighbor in GetNeighbors(current, allNodes))
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
        var neighbors = new List<GridBehaviour>();
        float gridSpacing = GetGridSpacing();
        Vector3 pos = grid.transform.position;

        foreach (GridBehaviour other in allNodes)
        {
            if (other == grid || !other.IsWalkable) continue;

            Vector3 dir = other.transform.position - pos;

            // 仅支持 4 向（上下左右），非对角
            if (Mathf.Abs(dir.x - gridSpacing) < 0.1f && Mathf.Abs(dir.y) < 0.1f ||
                Mathf.Abs(dir.x + gridSpacing) < 0.1f && Mathf.Abs(dir.y) < 0.1f ||
                Mathf.Abs(dir.y - gridSpacing) < 0.1f && Mathf.Abs(dir.x) < 0.1f ||
                Mathf.Abs(dir.y + gridSpacing) < 0.1f && Mathf.Abs(dir.x) < 0.1f)
            {
                neighbors.Add(other);
            }
        }

        return neighbors;
    }
    List<GridBehaviour> ReconstructPath(Dictionary<GridBehaviour, GridBehaviour> cameFrom, GridBehaviour current)
    {
        var path = new List<GridBehaviour> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }


        return path;
    }

    List<Transform> debugPathList;

    /// <summary>
    ///     This method is to draw Gizmos of the path.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (debugPathList == null || debugPathList.Count == 0) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < debugPathList.Count; i++)
        {
            if (debugPathList[i] != null)
            {
                Gizmos.DrawSphere(debugPathList[i].position, 0.15f);

                if (i < debugPathList.Count - 1 && debugPathList[i + 1] != null)
                {
                    Gizmos.DrawLine(debugPathList[i].position, debugPathList[i + 1].position);
                }
            }
        }
    } //

    #endregion

}
