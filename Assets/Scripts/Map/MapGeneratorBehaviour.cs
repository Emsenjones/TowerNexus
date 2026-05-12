using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class MapGeneratorBehaviour : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float nodeSize = 1f;
    [SerializeField] private GridNodeBehaviour nodePrefab;
    [SerializeField] private Transform generatedNodesParent;
    [SerializeField] private Sprite walkableSprite;
    [SerializeField] private Sprite unwalkableSprite;

    private readonly Dictionary<Vector2Int, GridNodeBehaviour> nodeDictionary = new Dictionary<Vector2Int, GridNodeBehaviour>();

    public int Width => width;
    public int Height => height;
    public IReadOnlyDictionary<Vector2Int, GridNodeBehaviour> NodeDictionary => nodeDictionary;

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (nodePrefab == null)
        {
            Debug.LogWarning("Map generation failed: node prefab is not assigned.", this);
            return;
        }

        ClearMap();

        Transform parent = GetGeneratedNodesParent();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                GridNodeBehaviour node = Instantiate(nodePrefab, parent);

                node.transform.position = new Vector3(x * nodeSize, 0f, y * nodeSize);
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

    public GridNodeBehaviour GetNode(Vector2Int gridPosition)
    {
        return nodeDictionary.TryGetValue(gridPosition, out GridNodeBehaviour node) ? node : null;
    }

    public GridNodeBehaviour GetNode(int x, int y)
    {
        return GetNode(new Vector2Int(x, y));
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

    public void RefreshMapVisual()
    {
        if (walkableSprite == null)
        {
            Debug.LogWarning("Map visual refresh warning: walkable sprite is not assigned.", this);
        }

        if (unwalkableSprite == null)
        {
            Debug.LogWarning("Map visual refresh warning: unwalkable sprite is not assigned.", this);
        }

        foreach (KeyValuePair<Vector2Int, GridNodeBehaviour> nodeEntry in nodeDictionary)
        {
            GridNodeBehaviour node = nodeEntry.Value;

            if (node == null || node.SpriteRenderer == null)
            {
                continue;
            }

            node.SpriteRenderer.sprite = node.IsWalkable ? walkableSprite : unwalkableSprite;
        }
    }

    private Transform GetGeneratedNodesParent()
    {
        return generatedNodesParent != null ? generatedNodesParent : transform;
    }
}
