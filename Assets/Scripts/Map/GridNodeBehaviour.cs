using UnityEngine;

public enum GridNodeType
{
    Normal,
    Spawn,
    Target
}

[ExecuteAlways]
public class GridNodeBehaviour : MonoBehaviour
{
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private bool baseWalkable = true;
    [SerializeField] private GridNodeType nodeType = GridNodeType.Normal;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform tileVisualRoot;
    [SerializeField] private Transform featureVisualRoot;

    private bool runtimeOccupied;
    private MapGeneratorBehaviour mapOwner;
    private Vector2Int observedPosition;
    private GridNodeType observedType;
    private bool observedWalkable;
    private bool hasObservedAuthoring;

    internal MapGeneratorBehaviour MapOwner => mapOwner;
    internal ulong AuthoringRevision { get; private set; }

    internal void BindMapOwner(MapGeneratorBehaviour owner)
    {
        mapOwner = owner;
        RememberAuthoring();
    }

    internal void ReleaseMapOwner(MapGeneratorBehaviour owner)
    {
        if (mapOwner == owner) mapOwner = null;
    }

    // Call after an explicit runtime hierarchy edit, including inactive nodes.
    public void RefreshMapOwnership()
    {
        MapGeneratorBehaviour next = GetComponentInParent<MapGeneratorBehaviour>(true);
        if (next != null && !next.OwnsNodeHierarchy(this)) next = null;
        MapGeneratorBehaviour previous = mapOwner;
        if (previous == next) return;
        mapOwner = next;
        previous?.NotifyNodeStructureChanged();
        next?.NotifyNodeStructureChanged();
    }

    private void OnEnable() => RefreshMapOwnership();
    private void OnTransformParentChanged() => RefreshMapOwnership();
    private void OnDestroy()
    {
        MapGeneratorBehaviour previous = mapOwner;
        mapOwner = null;
        previous?.NotifyNodeStructureChanged();
    }

    private void OnValidate()
    {
        if (!hasObservedAuthoring || observedPosition != gridPosition || observedType != nodeType)
            mapOwner?.NotifyNodeStructureChanged();
        if (!hasObservedAuthoring || observedWalkable != baseWalkable)
            mapOwner?.NotifyNodeWalkabilityChanged();
        RememberAuthoring();
    }

    private void RememberAuthoring()
    {
        if (!hasObservedAuthoring || observedPosition != gridPosition ||
            observedType != nodeType || observedWalkable != baseWalkable)
            AuthoringRevision++;
        observedPosition = gridPosition;
        observedType = nodeType;
        observedWalkable = baseWalkable;
        hasObservedAuthoring = true;
    }

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
        bool structureChanged = this.gridPosition != gridPosition || this.nodeType != nodeType;
        bool walkabilityChanged = this.baseWalkable != baseWalkable || runtimeOccupied;
        RefreshMapOwnership();
        this.gridPosition = gridPosition;
        this.baseWalkable = baseWalkable;
        this.nodeType = nodeType;
        runtimeOccupied = false;
        RememberAuthoring();
        if (structureChanged) mapOwner?.NotifyNodeStructureChanged();
        if (walkabilityChanged) mapOwner?.NotifyNodeWalkabilityChanged();
    }

    public void SetBaseWalkable(bool value)
    {
        if (baseWalkable == value) return;
        baseWalkable = value;
        RememberAuthoring();
        mapOwner?.NotifyNodeWalkabilityChanged();
    }

    public void SetRuntimeOccupied(bool value)
    {
        if (runtimeOccupied == value) return;
        runtimeOccupied = value;
        mapOwner?.NotifyNodeWalkabilityChanged();
    }

    public void ResetRuntimeState()
    {
        runtimeOccupied = false;
        mapOwner?.NotifyNodeWalkabilityChanged();
    }

    public void SetGridPosition(Vector2Int value)
    {
        if (gridPosition == value) return;
        gridPosition = value;
        RememberAuthoring();
        mapOwner?.NotifyNodeStructureChanged();
    }

    public void SetNodeType(GridNodeType value)
    {
        if (nodeType == value) return;
        nodeType = value;
        RememberAuthoring();
        mapOwner?.NotifyNodeStructureChanged();
    }
}
