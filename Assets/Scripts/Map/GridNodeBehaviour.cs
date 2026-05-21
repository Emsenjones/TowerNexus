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
    [SerializeField] private bool isWalkable = false;
    [SerializeField] private GridNodeType nodeType = GridNodeType.Normal;

    public Vector2Int GridPosition => gridPosition;
    public Vector3 WorldPosition => transform.position;
    public bool IsWalkable => isWalkable;
    public GridNodeType NodeType => nodeType;

    public void Initialize(Vector2Int gridPosition, bool isWalkable)
    {
        this.gridPosition = gridPosition;
        this.isWalkable = isWalkable;
        nodeType = GridNodeType.Normal;
    }

    public void SetWalkable(bool value)
    {
        isWalkable = value;
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
