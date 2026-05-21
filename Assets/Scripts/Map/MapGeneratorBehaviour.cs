using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class MapGeneratorBehaviour : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float nodeSize = 1f;
    [SerializeField] private GridNodeBehaviour nodePrefab;
    [SerializeField] private Transform generatedNodesParent;
    [Header("Tile Visual Prefabs")]
    [SerializeField] private GameObject allDirectionsTilePrefab;
    [SerializeField] private GameObject noDirectionsTilePrefab;
    [SerializeField] private GameObject upDownTilePrefab;
    [SerializeField] private GameObject leftRightTilePrefab;
    [SerializeField] private GameObject upLeftTilePrefab;
    [SerializeField] private GameObject upRightTilePrefab;
    [SerializeField] private GameObject downLeftTilePrefab;
    [SerializeField] private GameObject downRightTilePrefab;
    [SerializeField] private GameObject downLeftRightTilePrefab;
    [SerializeField] private GameObject upLeftRightTilePrefab;
    [SerializeField] private GameObject upDownRightTilePrefab;
    [SerializeField] private GameObject upDownLeftTilePrefab;

    private const string GeneratedNodesParentName = "Nodes";
    private const string TileVisualInstanceName = "TileVisualInstance";

    private readonly Dictionary<Vector2Int, GridNodeBehaviour> nodeDictionary = new Dictionary<Vector2Int, GridNodeBehaviour>();

    public int Width => width;
    public int Height => height;
    public IReadOnlyDictionary<Vector2Int, GridNodeBehaviour> NodeDictionary => nodeDictionary;

    private void OnEnable()
    {
        RebuildNodeDictionary();
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (nodePrefab == null)
        {
            Debug.LogWarning("Map generation failed: node prefab is not assigned.", this);
            return;
        }

        ClearMap();

        Transform parent = GetOrCreateGeneratedNodesParent();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                GridNodeBehaviour node = Instantiate(nodePrefab, parent);

                node.transform.localPosition = new Vector3(x * nodeSize, 0f, y * nodeSize);
                node.name = $"Node_{x}_{y}";
                node.Initialize(gridPosition, true);

                nodeDictionary[gridPosition] = node;
            }
        }

        RefreshMapVisual();
    }

    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        nodeDictionary.Clear();

        Transform parent = GetGeneratedNodesParent();

        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child.GetComponent<GridNodeBehaviour>() == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    [ContextMenu("Rebuild Node Dictionary")]
    public void RebuildNodeDictionary()
    {
        nodeDictionary.Clear();

        Transform parent = GetGeneratedNodesParent();

        if (parent == null)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (!child.TryGetComponent(out GridNodeBehaviour node))
            {
                continue;
            }

            Vector2Int gridPosition = node.GridPosition;

            if (nodeDictionary.ContainsKey(gridPosition))
            {
                Debug.LogWarning($"Map node dictionary rebuild skipped duplicate node at {gridPosition}.", child);
                continue;
            }

            nodeDictionary.Add(gridPosition, node);
        }
    }

    public GridNodeBehaviour GetNode(Vector2Int gridPosition)
    {
        return nodeDictionary.TryGetValue(gridPosition, out GridNodeBehaviour node) ? node : null;
    }

    public GridNodeBehaviour GetNode(int x, int y)
    {
        return GetNode(new Vector2Int(x, y));
    }

    public GridNodeBehaviour GetSpawnNode()
    {
        if (nodeDictionary.Count == 0)
        {
            RebuildNodeDictionary();
        }

        foreach (KeyValuePair<Vector2Int, GridNodeBehaviour> nodeEntry in nodeDictionary)
        {
            GridNodeBehaviour node = nodeEntry.Value;

            if (node != null && node.NodeType == GridNodeType.Spawn)
            {
                return node;
            }
        }

        return null;
    }

    public bool TryGetNodeByWorldPosition(Vector3 worldPosition, out GridNodeBehaviour node)
    {
        node = null;

        if (nodeDictionary.Count == 0)
        {
            RebuildNodeDictionary();
        }

        if (nodeSize <= 0f)
        {
            Debug.LogWarning("Map query failed: node size must be greater than zero.", this);
            return false;
        }

        Transform gridSpace = GetGridSpaceTransform();
        Vector3 localPosition = gridSpace.InverseTransformPoint(worldPosition) - GetGridOriginLocalPosition(gridSpace);

        Vector2Int gridPosition = new Vector2Int(
            Mathf.RoundToInt(localPosition.x / nodeSize),
            Mathf.RoundToInt(localPosition.z / nodeSize)
        );

        node = GetNode(gridPosition);
        return node != null;
    }

    public bool HasNode(Vector2Int gridPosition)
    {
        return nodeDictionary.ContainsKey(gridPosition);
    }

    public bool IsInsideBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 &&
               gridPosition.x < width &&
               gridPosition.y >= 0 &&
               gridPosition.y < height;
    }

    public List<GridNodeBehaviour> GetNeighborNodes(Vector2Int gridPosition)
    {
        List<GridNodeBehaviour> neighbors = new List<GridNodeBehaviour>();
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighborPosition = gridPosition + direction;

            if (!IsInsideBounds(neighborPosition))
            {
                continue;
            }

            GridNodeBehaviour neighbor = GetNode(neighborPosition);

            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public bool SetNodeWalkable(Vector2Int gridPosition, bool value)
    {
        GridNodeBehaviour node = GetNode(gridPosition);

        if (node == null)
        {
            return false;
        }

        node.SetWalkable(value);
        RefreshMapVisual();

        return true;
    }

    public bool SetNodeWalkable(int x, int y, bool value)
    {
        return SetNodeWalkable(new Vector2Int(x, y), value);
    }
    [ContextMenu("Refresh Map Visual")]
    public void RefreshMapVisual()
    {
        if (nodeDictionary.Count == 0)
        {
            RebuildNodeDictionary();
        }

        foreach (KeyValuePair<Vector2Int, GridNodeBehaviour> nodeEntry in nodeDictionary)
        {
            GridNodeBehaviour node = nodeEntry.Value;

            if (node == null)
            {
                continue;
            }

            ClearNodeTileVisual(node.transform);

            GameObject tilePrefab = GetTileVisualPrefab(nodeEntry.Key);

            if (tilePrefab == null)
            {
                Debug.LogWarning($"Map visual refresh warning: no tile prefab matched node {nodeEntry.Key}.", this);
                continue;
            }

            GameObject tileVisual = Instantiate(tilePrefab, node.transform);
            tileVisual.name = "TileVisualInstance";
            tileVisual.transform.localPosition = Vector3.zero;
            tileVisual.transform.localRotation = Quaternion.identity;
            tileVisual.transform.localScale = Vector3.one;
        }
    }

    private GameObject GetTileVisualPrefab(Vector2Int gridPosition)
    {
        GridNodeBehaviour currentNode = GetNode(gridPosition);

        if (currentNode == null || !currentNode.IsWalkable)
        {
            return noDirectionsTilePrefab;
        }

        bool up = IsDirectionWalkable(gridPosition + Vector2Int.up);
        bool down = IsDirectionWalkable(gridPosition + Vector2Int.down);
        bool left = IsDirectionWalkable(gridPosition + Vector2Int.left);
        bool right = IsDirectionWalkable(gridPosition + Vector2Int.right);

        int directionMask = 0;

        if (up)
        {
            directionMask |= 1;
        }

        if (down)
        {
            directionMask |= 2;
        }

        if (left)
        {
            directionMask |= 4;
        }

        if (right)
        {
            directionMask |= 8;
        }

        switch (directionMask)
        {
            case 0:
                return noDirectionsTilePrefab;
            case 1:
            case 2:
            case 4:
            case 8:
                return noDirectionsTilePrefab;
            case 1 | 2:
                return upDownTilePrefab;
            case 4 | 8:
                return leftRightTilePrefab;
            case 1 | 4:
                return upLeftTilePrefab;
            case 1 | 8:
                return upRightTilePrefab;
            case 2 | 4:
                return downLeftTilePrefab;
            case 2 | 8:
                return downRightTilePrefab;
            case 2 | 4 | 8:
                return downLeftRightTilePrefab;
            case 1 | 4 | 8:
                return upLeftRightTilePrefab;
            case 1 | 2 | 8:
                return upDownRightTilePrefab;
            case 1 | 2 | 4:
                return upDownLeftTilePrefab;
            case 1 | 2 | 4 | 8:
                return allDirectionsTilePrefab;
            default:
                return noDirectionsTilePrefab;
        }
    }

    private bool IsDirectionWalkable(Vector2Int gridPosition)
    {
        GridNodeBehaviour node = GetNode(gridPosition);
        return node != null && node.IsWalkable;
    }

    private Transform GetGridSpaceTransform()
    {
        Transform parent = GetGeneratedNodesParent();
        return parent != null ? parent : transform;
    }

    private Vector3 GetGridOriginLocalPosition(Transform gridSpace)
    {
        foreach (KeyValuePair<Vector2Int, GridNodeBehaviour> nodeEntry in nodeDictionary)
        {
            GridNodeBehaviour node = nodeEntry.Value;

            if (node == null)
            {
                continue;
            }

            Vector3 nodeLocalPosition = gridSpace.InverseTransformPoint(node.WorldPosition);
            return nodeLocalPosition - new Vector3(
                nodeEntry.Key.x * nodeSize,
                0f,
                nodeEntry.Key.y * nodeSize
            );
        }

        return Vector3.zero;
    }

    private void ClearNodeTileVisual(Transform nodeTransform)
    {
        for (int i = nodeTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = nodeTransform.GetChild(i);

            if (!IsGeneratedTileVisualChild(child))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private bool IsGeneratedTileVisualChild(Transform child)
    {
        if (child.name.StartsWith(TileVisualInstanceName))
        {
            return true;
        }

        return IsLegacyTileVisualName(child.name);
    }

    private bool IsLegacyTileVisualName(string childName)
    {
        return MatchesPrefabName(childName, allDirectionsTilePrefab) ||
               MatchesPrefabName(childName, noDirectionsTilePrefab) ||
               MatchesPrefabName(childName, upDownTilePrefab) ||
               MatchesPrefabName(childName, leftRightTilePrefab) ||
               MatchesPrefabName(childName, upLeftTilePrefab) ||
               MatchesPrefabName(childName, upRightTilePrefab) ||
               MatchesPrefabName(childName, downLeftTilePrefab) ||
               MatchesPrefabName(childName, downRightTilePrefab) ||
               MatchesPrefabName(childName, downLeftRightTilePrefab) ||
               MatchesPrefabName(childName, upLeftRightTilePrefab) ||
               MatchesPrefabName(childName, upDownRightTilePrefab) ||
               MatchesPrefabName(childName, upDownLeftTilePrefab);
    }

    private bool MatchesPrefabName(string childName, GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        return childName == prefab.name || childName == $"{prefab.name}(Clone)";
    }

    private Transform GetGeneratedNodesParent()
    {
        if (generatedNodesParent != null && generatedNodesParent != transform)
        {
            return generatedNodesParent;
        }

        return transform.Find(GeneratedNodesParentName);
    }

    private Transform GetOrCreateGeneratedNodesParent()
    {
        Transform parent = GetGeneratedNodesParent();

        if (parent != null)
        {
            return parent;
        }

        GameObject parentObject = new GameObject(GeneratedNodesParentName);
        Transform parentTransform = parentObject.transform;
        parentTransform.SetParent(transform);
        parentTransform.localPosition = Vector3.zero;
        parentTransform.localRotation = Quaternion.identity;
        parentTransform.localScale = Vector3.one;

        return parentTransform;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MapGeneratorBehaviour))]
public class MapGeneratorBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapGeneratorBehaviour mapGenerator = (MapGeneratorBehaviour)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Map Authoring Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Map"))
        {
            mapGenerator.GenerateMap();
            EditorUtility.SetDirty(mapGenerator);
        }

        if (GUILayout.Button("Refresh Map Visual"))
        {
            mapGenerator.RefreshMapVisual();
            EditorUtility.SetDirty(mapGenerator);
        }
    }
}
#endif
