using UnityEngine;

[ExecuteAlways]
public class MapGeneratorBehaviour : MonoBehaviour
{
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float nodeSize = 1f;
    [SerializeField] private GridNodeBehaviour nodePrefab;
    [SerializeField] private Transform generatedNodesParent;

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

                node.transform.position = new Vector3(x * nodeSize, y * nodeSize, 0f);
                node.name = $"Node_{x}_{y}";
                node.Initialize(gridPosition, true);
            }
        }
    }

    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
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

    private Transform GetGeneratedNodesParent()
    {
        return generatedNodesParent != null ? generatedNodesParent : transform;
    }
}
