using UnityEngine;

public class GridNodeBehaviour : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private bool isWalkable = false;

    public Vector2Int GridPosition => gridPosition;
    public Vector3 WorldPosition => transform.position;
    public bool IsWalkable => isWalkable;

    public void Initialize(Vector2Int gridPosition, bool isWalkable)
    {
        this.gridPosition = gridPosition;
        this.isWalkable = isWalkable;
    }

    public void SetWalkable(bool value)
    {
        isWalkable = value;
    }

    public void SetGridPosition(Vector2Int value)
    {
        gridPosition = value;
    }
}
