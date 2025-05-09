using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class MapController : MonoBehaviour
{
    [Serializable]private class GridInfo
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
    [InfoBox("The road sprite and its relevant index.")]
    [SerializeField] List<GridInfo> gridInfoList;

    Sprite GetGridSprite(int index)
    {
        var match = gridInfoList.FirstOrDefault(item => item.Index == index);
        if (match != null)
            return match.Sprite;

        Debug.LogWarning($"No sprite found for road index {index}");
        return null;
    }

    /// <summary>
    /// if nodeBehaviours have changed, call this function to update their sprites.
    /// </summary>
    
    float GetGridSpacing()
    {
        var nodeList = GetComponentsInChildren<NodeBehaviour>().ToList();
        if (nodeList.Count < 2)
            return 0f;

        // 找到两个不同位置的节点
        for (int i = 0; i < nodeList.Count - 1; i++)
        {
            for (int j = i + 1; j < nodeList.Count; j++)
            {
                float dist = Vector3.Distance(nodeList[i].Transform.position, nodeList[j].Transform.position);
                if (dist > 0.01f)
                {
                    return Mathf.Round(dist * 100f) / 100f; // 保留两位小数
                }
            }
        }

        return 0f;
    } 
    /// <summary>
    /// Generate a path for the enemy.
    /// </summary>
    /// <param name="enemyTransform">Enemy's transform</param>
    /// <returns>a list of transform.The count of this list will be 0 if enemy cannot find the path.</returns>
    public List<Transform> FindPath(Transform enemyTransform)
    {
        List<NodeBehaviour> allNodes = GetComponentsInChildren<NodeBehaviour>().ToList();
        NodeBehaviour targetNode = allNodes.FirstOrDefault(n => n.ThisType == NodeBehaviour.Type.Target);

        if (targetNode == null)
        {
            Debug.LogError("No Target Node found in map.");
            return null;
        }

        NodeBehaviour startNode = allNodes
            .Where(n => n.IsWalkable)
            .OrderBy(n => Vector3.Distance(enemyTransform.position, n.Transform.position))
            .FirstOrDefault();

        if (startNode == null)
        {
            Debug.LogError("No valid start node found.");
            return null;
        }
        _debugPathList = AStar(startNode, targetNode, allNodes)
            .Select(n => n.Transform)
            .ToList();

        return _debugPathList;
    } 
    
    #region Support methods.
    void UpdateGridsInfo()
    {
        float gridSpacing = GetGridSpacing();
        if (gridSpacing <= 0f)
        {
            Debug.LogError("Grid spacing is invalid.");
            return;
        }
        var nodeList = GetComponentsInChildren<NodeBehaviour>().ToList();
        if (!nodeList.Any())
        {
            Debug.LogError("Cannot find any NodeBehaviour in " + gameObject.name+" .");
            return;
        }

        foreach (var node in nodeList)
        {
            var spriteRenderer = node.Transform.GetComponent<SpriteRenderer>();
            if (!spriteRenderer)
            {
                Debug.LogWarning($"No SpriteRenderer found on Node at {node.gameObject.name} .");
                continue;
            }
            if (!node.IsWalkable)
            {
                spriteRenderer.sprite = null;
                continue;
            }

            Vector3 pos = node.Transform.position;
            int index = 0;
            foreach (var neighbor in nodeList)
            {
                if (!neighbor.IsWalkable || neighbor == node)
                    continue;

                Vector3 dir = neighbor.Transform.position - pos;

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

    List<NodeBehaviour> AStar(NodeBehaviour start, NodeBehaviour goal, List<NodeBehaviour> allNodes)
    {
        var openSet = new List<NodeBehaviour> { start };
        var cameFrom = new Dictionary<NodeBehaviour, NodeBehaviour>();
        var gScore = new Dictionary<NodeBehaviour, float>();
        var fScore = new Dictionary<NodeBehaviour, float>();

        foreach (var node in allNodes)
        {
            gScore[node] = float.MaxValue;
            fScore[node] = float.MaxValue;
        }

        gScore[start] = 0;
        fScore[start] = Vector3.Distance(start.Transform.position, goal.Transform.position);

        while (openSet.Count > 0)
        {
            NodeBehaviour current = openSet.OrderBy(n => fScore[n]).First();

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(current, allNodes))
            {
                if (!neighbor.IsWalkable) continue;

                float tentativeGScore = gScore[current] + Vector3.Distance(current.Transform.position, neighbor.Transform.position);

                if (tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Vector3.Distance(neighbor.Transform.position, goal.Transform.position);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        Debug.LogWarning("No path found.");
        return new List<NodeBehaviour>(); // 找不到路径时返回空列表
    }

    List<NodeBehaviour> GetNeighbors(NodeBehaviour node, List<NodeBehaviour> allNodes)
    {
        List<NodeBehaviour> neighbors = new List<NodeBehaviour>();
        float gridSpacing = GetGridSpacing();
        Vector3 pos = node.Transform.position;

        foreach (var other in allNodes)
        {
            if (other == node || !other.IsWalkable) continue;

            Vector3 dir = other.Transform.position - pos;

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
    private List<NodeBehaviour> ReconstructPath(Dictionary<NodeBehaviour, NodeBehaviour> cameFrom, NodeBehaviour current)
    {
        List<NodeBehaviour> path = new List<NodeBehaviour> { current };

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
        var targets = GetComponentsInChildren<NodeBehaviour>()
            .Where(n => n.ThisType == NodeBehaviour.Type.Target)
            .ToList();

        
        if (targets.Count > 1)
        {
            Debug.LogWarning($"[MapController] detected multiple target, Current target count：{targets.Count}.");
        }
    }
#endif
    #endregion

    [SerializeField]
    Transform enemyTrans;
    void Start()
    {
        FindPath(enemyTrans);
    }
}


