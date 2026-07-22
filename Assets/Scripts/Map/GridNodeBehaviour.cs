using UnityEngine;

public enum GridNodeType
{
    Normal,
    Spawn,
    Target
}

public class GridNodeBehaviour : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private bool baseWalkable = true;
    [SerializeField] private GridNodeType nodeType = GridNodeType.Normal;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform tileVisualRoot;
    [SerializeField] private Transform featureVisualRoot;

    private bool runtimeOccupied;

    public Vector2Int GridPosition => gridPosition;
    public Vector3 WorldPosition => transform.position;
    public bool BaseWalkable => baseWalkable;
    public bool RuntimeOccupied => runtimeOccupied;
    public bool IsWalkable => baseWalkable && !runtimeOccupied;
    public GridNodeType NodeType => nodeType;
    public Transform VisualRoot => visualRoot;
    public Transform TileVisualRoot => tileVisualRoot;
    public Transform FeatureVisualRoot => featureVisualRoot;

    public void Initialize(
        Vector2Int gridPosition,
        bool baseWalkable,
        GridNodeType nodeType = GridNodeType.Normal)
    {
        this.gridPosition = gridPosition;
        this.baseWalkable = baseWalkable;
        this.nodeType = nodeType;
        runtimeOccupied = false;
    }

    public void SetBaseWalkable(bool value)
    {
        baseWalkable = value;
    }

    public void SetRuntimeOccupied(bool value)
    {
        runtimeOccupied = value;
    }

    public void ResetRuntimeState()
    {
        runtimeOccupied = false;
    }

    public void SetGridPosition(Vector2Int value)
    {
        gridPosition = value;
    }

    public void SetNodeType(GridNodeType value)
    {
        nodeType = value;
    }
}
