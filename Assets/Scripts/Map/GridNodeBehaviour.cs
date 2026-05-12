using UnityEngine;

public class GridNodeBehaviour : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private bool isWalkable = true;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public Vector2Int GridPosition => gridPosition;
    public Vector3 WorldPosition => transform.position;
    public bool IsWalkable => isWalkable;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    public void Initialize(Vector2Int gridPosition, bool isWalkable, SpriteRenderer spriteRenderer = null)
    {
        this.gridPosition = gridPosition;
        this.isWalkable = isWalkable;
        this.spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
    }

    public void SetWalkable(bool value)
    {
        isWalkable = value;
    }

    public void SetGridPosition(Vector2Int value)
    {
        gridPosition = value;
    }

    public void SetSpriteRenderer(SpriteRenderer value)
    {
        spriteRenderer = value;
    }

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
