using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class MapValidationResult
{
    private readonly List<string> errors = new List<string>();
    private readonly List<string> warnings = new List<string>();

    public bool IsValid => errors.Count == 0;
    public IReadOnlyList<string> Errors => errors;
    public IReadOnlyList<string> Warnings => warnings;

    internal void AddError(string message)
    {
        errors.Add(message);
    }

    internal void AddWarning(string message)
    {
        warnings.Add(message);
    }
}

[ExecuteAlways]
public class MapGeneratorBehaviour : MonoBehaviour
{
    private sealed class VisualRefreshItem
    {
        public GridNodeBehaviour Node;
        public GameObject TilePrefab;
        public GameObject FeaturePrefab;
    }

    private static readonly Vector2Int[] OrthogonalDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    [SerializeField] private int width = 10;
    [SerializeField] private int lengh = 10;
    [SerializeField] private float nodeSize = 1f;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform nodesRoot;
    [SerializeField] private MapVisualTheme mapVisualTheme;
    [SerializeField] private int mapVisualSeed;
    [SerializeField] private MapCameraBoundary cameraBoundary;
    [SerializeField] private Transform cameraDefaultPose;

    private readonly MapRuntimeNodeIndex runtimeIndex = new MapRuntimeNodeIndex();
    private GridNodeBehaviour[] indexedHierarchy = System.Array.Empty<GridNodeBehaviour>();
    private bool indexReleased;
    private Transform observedNodesRoot;
    private int observedWidth, observedHeight;
    private float observedNodeSize;

    public ulong StructureRevision => runtimeIndex.StructureRevision;
    public ulong WalkabilityRevision => runtimeIndex.WalkabilityRevision;
    public string NodeIndexFailureReason => runtimeIndex.FailureReason;
#if UNITY_EDITOR
    public int NodeIndexBuildCount => runtimeIndex.BuildCount;
    public int HierarchyQueryCount { get; private set; }
    private NodeAuthoringSnapshot[] authoringSnapshot = System.Array.Empty<NodeAuthoringSnapshot>();

    private readonly struct NodeAuthoringSnapshot
    {
        internal NodeAuthoringSnapshot(GridNodeBehaviour node)
        {
            Node = node;
            Parent = node != null ? node.transform.parent : null;
            Position = node != null ? node.GridPosition : default;
            Type = node != null ? node.NodeType : default;
            Walkable = node != null && node.BaseWalkable;
            NotificationRevision = node != null ? node.AuthoringRevision : 0;
        }
        internal GridNodeBehaviour Node { get; }
        internal Transform Parent { get; }
        internal Vector2Int Position { get; }
        internal GridNodeType Type { get; }
        internal bool Walkable { get; }
        internal ulong NotificationRevision { get; }
    }
#endif

    public int Width => width;
    public int Lengh => lengh;
    public float NodeSize => nodeSize;
    public Transform NodesRoot => nodesRoot;
    public MapVisualTheme VisualTheme => mapVisualTheme;
    public MapCameraBoundary CameraBoundary => cameraBoundary;
    public Transform CameraDefaultPose => cameraDefaultPose;

    private void OnEnable()
    {
        indexReleased = false;
        NotifyNodeStructureChanged();
        RebuildNodeDictionary();
    }

    private void OnDisable()
    {
        indexReleased = true;
        ReleaseNodeOwners();
        runtimeIndex.Release();
    }

    private void OnValidate()
    {
        if (observedNodesRoot != nodesRoot || observedWidth != width ||
            observedHeight != lengh || observedNodeSize != nodeSize)
            NotifyNodeStructureChanged();
    }

    // Explicit structural edit boundary for each affected Map. Runtime node transfers
    // also call node.RefreshMapOwnership(); removals detach before deferred Destroy.
    public void NotifyNodeStructureChanged() => runtimeIndex.InvalidateStructure();
    internal void NotifyNodeWalkabilityChanged() => runtimeIndex.InvalidateWalkability();

    internal bool OwnsNodeHierarchy(GridNodeBehaviour node) =>
        node != null && IsNodesRootSafelyOwned() && node.transform != nodesRoot &&
        node.transform.IsChildOf(nodesRoot) &&
        node.GetComponentInParent<MapGeneratorBehaviour>(true) == this &&
        !HasGridNodeAncestorInsideNodesRoot(node);

    public bool TryEnsureNodeIndex()
    {
        if (indexReleased) return false;
        if (runtimeIndex.IsDirty) RebuildNodeDictionary();
        return runtimeIndex.IsValid;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterEditorTopologyChecks()
    {
        UnityEditor.EditorApplication.hierarchyChanged -= CheckEditedMapTopologies;
        UnityEditor.EditorApplication.hierarchyChanged += CheckEditedMapTopologies;
        UnityEditor.Undo.undoRedoPerformed -= CheckEditedMapTopologies;
        UnityEditor.Undo.undoRedoPerformed += CheckEditedMapTopologies;
    }

    private static void CheckEditedMapTopologies()
    {
        // Runtime hierarchy churn (projectiles/VFX) is not a topology-edit signal.
        // Runtime structure changes use the explicit node/Map mutation boundary.
        if (Application.isPlaying) return;
        foreach (MapGeneratorBehaviour map in Resources.FindObjectsOfTypeAll<MapGeneratorBehaviour>())
            if (map != null && !map.indexReleased) map.CheckEditorTopology();
    }

    private void CheckEditorTopology()
    {
        GridNodeBehaviour[] nodes = GetHierarchyNodes();
        bool changed = observedNodesRoot != nodesRoot || observedWidth != width ||
            observedHeight != lengh || observedNodeSize != nodeSize ||
            nodes.Length != authoringSnapshot.Length;
        bool walkabilityChanged = false;
        for (int i = 0; !changed && i < nodes.Length; i++)
        {
            GridNodeBehaviour node = nodes[i];
            NodeAuthoringSnapshot previous = authoringSnapshot[i];
            changed = node == null || previous.Node != node ||
                previous.Parent != node.transform.parent || previous.Position != node.GridPosition ||
                previous.Type != node.NodeType;
            walkabilityChanged |= node != null && previous.Walkable != node.BaseWalkable &&
                previous.NotificationRevision == node.AuthoringRevision;
        }
        if (changed)
        {
            // Reconcile inactive-node ownership at the edit boundary, never per lookup.
            foreach (GridNodeBehaviour node in indexedHierarchy)
                if (node != null) node.RefreshMapOwnership();
            foreach (GridNodeBehaviour node in nodes)
                if (node != null) node.RefreshMapOwnership();
            if (!runtimeIndex.IsDirty) NotifyNodeStructureChanged();
        }
        else if (walkabilityChanged)
        {
            NotifyNodeWalkabilityChanged();
        }
        CaptureAuthoringSnapshot(nodes);
    }

    private void CaptureAuthoringSnapshot(GridNodeBehaviour[] nodes)
    {
        authoringSnapshot = new NodeAuthoringSnapshot[nodes.Length];
        for (int i = 0; i < nodes.Length; i++) authoringSnapshot[i] = new NodeAuthoringSnapshot(nodes[i]);
        RememberMapConfiguration();
    }
#endif

    private void RememberMapConfiguration()
    {
        observedNodesRoot = nodesRoot;
        observedWidth = width;
        observedHeight = lengh;
        observedNodeSize = nodeSize;
    }

    [Button("Generate Map")]
    private void GenerateMapFromInspector()
    {
        if (!TryEnsureCameraAuthoringFromInspector())
        {
            return;
        }

        GenerateMap();
    }

    public bool GenerateMap()
    {
        MapValidationResult preflight = ValidateGeneratePreflight();

        if (!preflight.IsValid)
        {
            LogValidationResult("Map generation preflight failed", preflight);
            return false;
        }

        ClearMap();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < lengh; y++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                GameObject nodeObject = Instantiate(nodePrefab, nodesRoot, false);
                nodeObject.name = $"Node_{x}_{y}";
                nodeObject.transform.localPosition = new Vector3(x * nodeSize, 0f, y * nodeSize);
                nodeObject.transform.localRotation = Quaternion.identity;

                GridNodeBehaviour node = nodeObject.GetComponent<GridNodeBehaviour>();
                node.Initialize(gridPosition, true, GridNodeType.Normal);
            }
        }

        ApplyGridCentering();
        RebuildNodeDictionary();
        return RefreshMapVisual();
    }

    [Button("Clear Map")]
    public void ClearMap()
    {
        if (!IsNodesRootSafelyOwned())
        {
            Debug.LogError(
                "Clear Map refused: NodesRoot must be a child of this Map Generator and must not be a Grid Node.",
                this);
            return;
        }

        if (nodesRoot != null)
        {
            GridNodeBehaviour[] nodes = GetHierarchyNodes();

            for (int i = nodes.Length - 1; i >= 0; i--)
            {
                GridNodeBehaviour node = nodes[i];

                if (node == null || HasGridNodeAncestorInsideNodesRoot(node))
                {
                    continue;
                }

                node.ReleaseMapOwner(this);
                // Remove from the hierarchy now; Destroy is deferred in Play Mode.
                node.transform.SetParent(null, true);
                DestroyOwnedObject(node.gameObject);
            }
        }

        ReleaseNodeOwners();
        runtimeIndex.Release();
    }

    public GridNodeBehaviour GetNode(Vector2Int gridPosition)
    {
        return TryEnsureNodeIndex() ? runtimeIndex.GetNode(gridPosition) : null;
    }

    public GridNodeBehaviour GetNode(int x, int y)
    {
        return GetNode(new Vector2Int(x, y));
    }

    public GridNodeBehaviour GetSpawnNode() =>
        TryEnsureNodeIndex() ? runtimeIndex.Spawn : null;

    public GridNodeBehaviour GetTargetNode() =>
        TryEnsureNodeIndex() ? runtimeIndex.Target : null;

    public bool TryGetNodeByWorldPosition(Vector3 worldPosition, out GridNodeBehaviour node)
    {
        node = null;
        if (!TryEnsureNodeIndex()) return false;

        if (nodesRoot == null || nodeSize <= 0f || !IsFinite(worldPosition))
        {
            return false;
        }

        Vector3 localPosition = nodesRoot.InverseTransformPoint(worldPosition);

        if (!IsFinite(localPosition) ||
            !TryResolvePhysicalGridCoordinate(
                localPosition.x,
                nodeSize,
                width,
                out int gridX) ||
            !TryResolvePhysicalGridCoordinate(
                localPosition.z,
                nodeSize,
                lengh,
                out int gridY))
        {
            return false;
        }

        Vector2Int gridPosition = new Vector2Int(gridX, gridY);

        node = runtimeIndex.GetNode(gridPosition);
        return node != null;
    }

    private static bool TryResolvePhysicalGridCoordinate(
        float localCoordinate,
        float spacing,
        int nodeCount,
        out int gridCoordinate)
    {
        gridCoordinate = 0;

        if (float.IsNaN(localCoordinate) ||
            float.IsInfinity(localCoordinate) ||
            spacing <= 0f ||
            nodeCount <= 0)
        {
            return false;
        }

        float normalized = localCoordinate / spacing + 0.5f;

        if (normalized < 0f || normalized > nodeCount)
        {
            return false;
        }

        if (normalized == nodeCount)
        {
            gridCoordinate = nodeCount - 1;
            return true;
        }

        gridCoordinate = Mathf.FloorToInt(normalized);
        return gridCoordinate >= 0 && gridCoordinate < nodeCount;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) &&
               !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) &&
               !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) &&
               !float.IsInfinity(value.z);
    }

    public bool HasNode(Vector2Int gridPosition)
    {
        return TryEnsureNodeIndex() && runtimeIndex.GetNode(gridPosition) != null;
    }

    public bool IsInsideBounds(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 &&
               gridPosition.x < width &&
               gridPosition.y >= 0 &&
               gridPosition.y < lengh;
    }

    public List<GridNodeBehaviour> GetNeighborNodes(Vector2Int gridPosition)
    {
        List<GridNodeBehaviour> neighbors = new List<GridNodeBehaviour>();
        if (!TryEnsureNodeIndex()) return neighbors;

        for (int i = 0; i < OrthogonalDirections.Length; i++)
        {
            Vector2Int neighborPosition = gridPosition + OrthogonalDirections[i];

            GridNodeBehaviour neighbor = runtimeIndex.GetNode(neighborPosition);
            if (IsInsideBounds(neighborPosition) && neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    [Button("Refresh Map Visual")]
    private void RefreshMapVisualFromInspector()
    {
        RefreshMapVisual();
    }

    public bool RefreshMapVisual()
    {
        if (!TryBuildVisualRefreshPlan(true, out List<VisualRefreshItem> refreshItems, out MapValidationResult preflight))
        {
            LogValidationResult("Authoring visual refresh preflight failed", preflight);
            return false;
        }

        for (int i = 0; i < refreshItems.Count; i++)
        {
            VisualRefreshItem item = refreshItems[i];
            ClearOwnedChildren(item.Node.TileVisualRoot);
            ClearOwnedChildren(item.Node.FeatureVisualRoot);
            CreateVisual(item.TilePrefab, item.Node.TileVisualRoot, "TileVisualInstance");

            if (item.FeaturePrefab != null)
            {
                string featureName = item.Node.BaseWalkable
                    ? item.Node.NodeType == GridNodeType.Spawn
                        ? "SpawnVisualInstance"
                        : "TargetVisualInstance"
                    : "ObstacleVisualInstance";

                CreateVisual(item.FeaturePrefab, item.Node.FeatureVisualRoot, featureName);
            }
        }

        return true;
    }

    public bool RefreshRuntimeTileVisuals()
    {
        if (!TryBuildVisualRefreshPlan(false, out List<VisualRefreshItem> refreshItems, out MapValidationResult preflight))
        {
            LogValidationResult("Runtime Tile visual refresh preflight failed", preflight);
            return false;
        }

        for (int i = 0; i < refreshItems.Count; i++)
        {
            VisualRefreshItem item = refreshItems[i];
            ClearOwnedChildren(item.Node.TileVisualRoot);
            CreateVisual(item.TilePrefab, item.Node.TileVisualRoot, "TileVisualInstance");
        }

        return true;
    }

    public MapValidationResult ValidateMap()
    {
        MapValidationResult result = new MapValidationResult();
        ValidateBasicConfiguration(result, true);
        ValidateCameraAuthoring(result);
        ValidateTheme(result, true);

        if (nodesRoot == null)
        {
            return result;
        }

        GridNodeBehaviour[] nodes = GetHierarchyNodes();
        int expectedNodeCount = width > 0 && lengh > 0 ? width * lengh : 0;

        if (nodes.Length != expectedNodeCount)
        {
            result.AddError($"Expected {expectedNodeCount} Grid Nodes but found {nodes.Length} under NodesRoot.");
        }

        Dictionary<Vector2Int, GridNodeBehaviour> uniqueNodes =
            new Dictionary<Vector2Int, GridNodeBehaviour>();
        List<GridNodeBehaviour> spawnNodes = new List<GridNodeBehaviour>();
        List<GridNodeBehaviour> targetNodes = new List<GridNodeBehaviour>();

        for (int i = 0; i < nodes.Length; i++)
        {
            GridNodeBehaviour node = nodes[i];

            if (node == null)
            {
                result.AddError("NodesRoot contains a missing Grid Node reference.");
                continue;
            }

            string nodeLabel = GetNodeLabel(node);
            ValidateVisualRootOwnership(node, nodesRoot, result, nodeLabel);

            if (node.transform.parent != nodesRoot)
            {
                result.AddError($"{nodeLabel} must be a direct child of NodesRoot.");
            }

            if (!IsInsideBounds(node.GridPosition))
            {
                result.AddError($"{nodeLabel} has out-of-bounds coordinate {node.GridPosition}.");
            }

            if (uniqueNodes.ContainsKey(node.GridPosition))
            {
                result.AddError($"Duplicate Grid Node coordinate {node.GridPosition} includes {nodeLabel}.");
            }
            else
            {
                uniqueNodes.Add(node.GridPosition, node);
            }

            Vector3 expectedLocalPosition = new Vector3(
                node.GridPosition.x * nodeSize,
                0f,
                node.GridPosition.y * nodeSize);

            if (node.transform.parent == nodesRoot &&
                (node.transform.localPosition - expectedLocalPosition).sqrMagnitude > 0.000001f)
            {
                result.AddError($"{nodeLabel} local position must be {expectedLocalPosition}.");
            }

            if (node.NodeType == GridNodeType.Spawn)
            {
                spawnNodes.Add(node);
            }
            else if (node.NodeType == GridNodeType.Target)
            {
                targetNodes.Add(node);
            }

            if (node.NodeType != GridNodeType.Normal && !node.BaseWalkable)
            {
                result.AddError($"{nodeLabel} is {node.NodeType} and must be Base Walkable.");
            }
        }

        if (width > 0 && lengh > 0)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < lengh; y++)
                {
                    Vector2Int coordinate = new Vector2Int(x, y);

                    if (!uniqueNodes.ContainsKey(coordinate))
                    {
                        result.AddError($"Missing Grid Node at coordinate {coordinate}.");
                    }
                }
            }
        }

        if (spawnNodes.Count != 1)
        {
            result.AddError($"Map must contain exactly one Spawn node; found {spawnNodes.Count}.");
        }

        if (targetNodes.Count != 1)
        {
            result.AddError($"Map must contain exactly one Target node; found {targetNodes.Count}.");
        }

        if (spawnNodes.Count == 1 && targetNodes.Count == 1 &&
            spawnNodes[0].BaseWalkable && targetNodes[0].BaseWalkable &&
            !HasBaseWalkableRoute(spawnNodes[0], targetNodes[0], uniqueNodes))
        {
            result.AddError("Map has no Base-Walkable route from Spawn to Target.");
        }

        return result;
    }

    [Button("Validate Map")]
    private void ValidateMapFromInspector()
    {
        LogValidationResult("Map validation", ValidateMap());
    }

    private MapValidationResult ValidateGeneratePreflight()
    {
        MapValidationResult result = new MapValidationResult();
        ValidateBasicConfiguration(result, true);
        ValidateCameraAuthoring(result);
        ValidateTheme(result, true);

        if (nodePrefab != null)
        {
            if (!nodePrefab.TryGetComponent(out GridNodeBehaviour gridNodePrefab))
            {
                result.AddError("Grid Node prefab must contain GridNodeBehaviour on its root GameObject.");
            }
            else
            {
                ValidateVisualRootOwnership(gridNodePrefab, null, result, "Grid Node prefab");
            }

            if (nodesRoot != null &&
                (nodePrefab.transform == nodesRoot || nodePrefab.transform.IsChildOf(nodesRoot)))
            {
                result.AddError("Grid Node prefab reference must not point to an existing node inside NodesRoot.");
            }
        }

        return result;
    }

    private bool TryEnsureCameraAuthoringFromInspector()
    {
        MapCameraBoundary[] adapters =
            GetComponentsInChildren<MapCameraBoundary>(true);
        List<Transform> namedBoundaryObjects =
            GetNamedDescendants(MapCameraBoundary.RequiredGameObjectName);

        if (cameraBoundary == null)
        {
            if (adapters.Length == 0 && namedBoundaryObjects.Count == 0)
            {
                GameObject boundaryObject =
                    new GameObject(MapCameraBoundary.RequiredGameObjectName);
                boundaryObject.transform.SetParent(transform, false);

                int boundaryLayer =
                    LayerMask.NameToLayer(MapCameraBoundary.RequiredLayerName);

                if (boundaryLayer >= 0)
                {
                    boundaryObject.layer = boundaryLayer;
                }

                cameraBoundary =
                    boundaryObject.AddComponent<MapCameraBoundary>();
                BoxCollider collider =
                    boundaryObject.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.center = new Vector3(0f, 10f, -6f);
                collider.size = Vector3.one;
                cameraBoundary.InitializeAuthoring(collider);
            }
            else if (adapters.Length == 1 &&
                     namedBoundaryObjects.Count == 1 &&
                     adapters[0].transform == namedBoundaryObjects[0] &&
                     adapters[0].transform.parent == transform)
            {
                cameraBoundary = adapters[0];
            }
            else
            {
                Debug.LogError(
                    "Generate Map cannot create or adopt Camera authoring: " +
                    "the Map contains an invalid or ambiguous MapCameraBoundary " +
                    "candidate. Run Validate Map for details.",
                    this);
                return false;
            }
        }

        if (!TryEnsureCameraDefaultPoseFromInspector())
        {
            return false;
        }

        MapValidationResult validation = new MapValidationResult();
        ValidateCameraAuthoring(validation);

        if (!validation.IsValid)
        {
            LogValidationResult(
                "Generate Map Camera authoring preflight failed",
                validation);
            return false;
        }

        return true;
    }

    private bool TryEnsureCameraDefaultPoseFromInspector()
    {
        List<Transform> namedPoseObjects =
            GetNamedDescendants("CameraDefaultPose");

        if (cameraDefaultPose == null)
        {
            if (namedPoseObjects.Count == 0)
            {
                GameObject poseObject = new GameObject("CameraDefaultPose");
                cameraDefaultPose = poseObject.transform;
                cameraDefaultPose.SetParent(transform, false);
                cameraDefaultPose.localPosition =
                    new Vector3(0f, 10f, -6f);
                cameraDefaultPose.localRotation =
                    Quaternion.Euler(60f, 0f, 0f);
            }
            else if (namedPoseObjects.Count == 1 &&
                     namedPoseObjects[0].parent == transform)
            {
                cameraDefaultPose = namedPoseObjects[0];
            }
            else
            {
                Debug.LogError(
                    "Generate Map cannot create or adopt CameraDefaultPose: " +
                    "the Map contains an invalid or ambiguous named candidate.",
                    this);
                return false;
            }
        }

        return true;
    }

    private void ValidateCameraAuthoring(MapValidationResult result)
    {
        MapCameraBoundary[] adapters =
            GetComponentsInChildren<MapCameraBoundary>(true);
        List<Transform> namedBoundaryObjects =
            GetNamedDescendants(MapCameraBoundary.RequiredGameObjectName);

        if (adapters.Length != 1)
        {
            result.AddError(
                $"Map must contain exactly one MapCameraBoundary adapter; " +
                $"found {adapters.Length}.");
        }

        if (namedBoundaryObjects.Count != 1)
        {
            result.AddError(
                $"Map must contain exactly one child named " +
                $"'{MapCameraBoundary.RequiredGameObjectName}'; found " +
                $"{namedBoundaryObjects.Count}.");
        }

        if (cameraBoundary == null)
        {
            result.AddError("MapCameraBoundary reference is not assigned.");
        }
        else
        {
            if (cameraBoundary.transform.parent != transform)
            {
                result.AddError(
                    "Referenced MapCameraBoundary must be a direct child of the " +
                    "Map root.");
            }

            if (cameraBoundary.gameObject.name !=
                MapCameraBoundary.RequiredGameObjectName)
            {
                result.AddError(
                    $"Referenced MapCameraBoundary GameObject must be named " +
                    $"'{MapCameraBoundary.RequiredGameObjectName}'.");
            }

            if (adapters.Length == 1 && adapters[0] != cameraBoundary)
            {
                result.AddError(
                    "MapCameraBoundary reference does not match the sole adapter " +
                    "candidate.");
            }

            if (!HasActiveSelfPathToMapRoot(cameraBoundary.transform))
            {
                result.AddError(
                    "MapCameraBoundary and its Map-owned ancestor path must be active.");
            }

            cameraBoundary.ValidateLocalStructure(result);
        }

        List<Transform> namedPoseObjects =
            GetNamedDescendants("CameraDefaultPose");

        if (namedPoseObjects.Count != 1)
        {
            result.AddError(
                $"Map must contain exactly one child named 'CameraDefaultPose'; " +
                $"found {namedPoseObjects.Count}.");
        }

        if (cameraDefaultPose == null)
        {
            result.AddError("CameraDefaultPose reference is not assigned.");
        }
        else
        {
            if (cameraDefaultPose.parent != transform)
            {
                result.AddError(
                    "Referenced CameraDefaultPose must be a direct child of the " +
                    "Map root.");
            }

            if (cameraDefaultPose.name != "CameraDefaultPose")
            {
                result.AddError(
                    "Referenced CameraDefaultPose must be named " +
                    "'CameraDefaultPose'.");
            }

            if (namedPoseObjects.Count == 1 &&
                namedPoseObjects[0] != cameraDefaultPose)
            {
                result.AddError(
                    "CameraDefaultPose reference does not match the sole named " +
                    "candidate.");
            }

            if (!HasActiveSelfPathToMapRoot(cameraDefaultPose))
            {
                result.AddError(
                    "CameraDefaultPose and its Map-owned ancestor path must be active.");
            }

            if (cameraBoundary != null &&
                cameraBoundary.BoundaryCollider != null &&
                !cameraBoundary.ContainsPoint(cameraDefaultPose.position))
            {
                result.AddError(
                    "CameraDefaultPose position must lie inside the authored " +
                    "MapCameraBoundary BoxCollider.");
            }
        }
    }

    private List<Transform> GetNamedDescendants(string requiredName)
    {
        Transform[] descendants = GetComponentsInChildren<Transform>(true);
        List<Transform> matches = new List<Transform>();

        for (int i = 0; i < descendants.Length; i++)
        {
            Transform candidate = descendants[i];

            if (candidate != transform && candidate.name == requiredName)
            {
                matches.Add(candidate);
            }
        }

        return matches;
    }

    private bool HasActiveSelfPathToMapRoot(Transform candidate)
    {
        Transform current = candidate;

        while (current != null && current != transform)
        {
            if (!current.gameObject.activeSelf)
            {
                return false;
            }

            current = current.parent;
        }

        return current == transform && transform.gameObject.activeSelf;
    }

    private bool TryBuildVisualRefreshPlan(
        bool useAuthoredState,
        out List<VisualRefreshItem> refreshItems,
        out MapValidationResult result)
    {
        refreshItems = new List<VisualRefreshItem>();
        result = new MapValidationResult();
        ValidateBasicConfiguration(result, false);
        ValidateTheme(result, useAuthoredState);

        if (nodesRoot == null || mapVisualTheme == null)
        {
            return false;
        }

        GridNodeBehaviour[] nodes = GetHierarchyNodes();
        Dictionary<Vector2Int, GridNodeBehaviour> uniqueNodes =
            new Dictionary<Vector2Int, GridNodeBehaviour>();

        for (int i = 0; i < nodes.Length; i++)
        {
            GridNodeBehaviour node = nodes[i];

            if (node == null)
            {
                result.AddError("NodesRoot contains a missing Grid Node reference.");
                continue;
            }

            ValidateVisualRootOwnership(node, nodesRoot, result, GetNodeLabel(node));

            if (uniqueNodes.ContainsKey(node.GridPosition))
            {
                result.AddError($"Visual refresh cannot resolve duplicate coordinate {node.GridPosition}.");
            }
            else
            {
                uniqueNodes.Add(node.GridPosition, node);
            }

            if (useAuthoredState && node.NodeType != GridNodeType.Normal && !node.BaseWalkable)
            {
                result.AddError($"{GetNodeLabel(node)} is {node.NodeType} and must be Base Walkable before refresh.");
            }
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            GridNodeBehaviour node = nodes[i];

            if (node == null)
            {
                continue;
            }

            MapTileDirectionMask tileMask =
                ResolveDirectionMask(node, uniqueNodes, useAuthoredState);

            if (!mapVisualTheme.TryGetTilePrefab(tileMask, out GameObject tilePrefab))
            {
                result.AddError($"{GetNodeLabel(node)} requires Tile mask {tileMask}, but no valid prefab resolves it.");
            }

            GameObject featurePrefab = null;

            if (useAuthoredState)
            {
                if (!node.BaseWalkable)
                {
                    featurePrefab = mapVisualTheme.GetDeterministicObstaclePrefab(mapVisualSeed, node.GridPosition);

                    if (featurePrefab == null)
                    {
                        result.AddError($"{GetNodeLabel(node)} requires an Obstacle prefab, but none resolves.");
                    }
                }
                else if (node.NodeType == GridNodeType.Spawn)
                {
                    featurePrefab = mapVisualTheme.SpawnPrefab;
                }
                else if (node.NodeType == GridNodeType.Target)
                {
                    featurePrefab = mapVisualTheme.TargetPrefab;
                }
            }

            refreshItems.Add(new VisualRefreshItem
            {
                Node = node,
                TilePrefab = tilePrefab,
                FeaturePrefab = featurePrefab
            });
        }

        return result.IsValid;
    }

    private void ValidateBasicConfiguration(MapValidationResult result, bool requireNodePrefab)
    {
        if (width <= 0)
        {
            result.AddError("Width must be greater than zero.");
        }

        if (lengh <= 0)
        {
            result.AddError("Height must be greater than zero.");
        }

        if (nodeSize <= 0f)
        {
            result.AddError("Node Size must be greater than zero.");
        }

        if (requireNodePrefab && nodePrefab == null)
        {
            result.AddError("Grid Node prefab is not assigned.");
        }

        if (nodesRoot == null)
        {
            result.AddError("NodesRoot is not assigned.");
        }
        else if (!IsNodesRootSafelyOwned())
        {
            result.AddError("NodesRoot must be a child of this Map Generator and must not be a Grid Node.");
        }

        if (mapVisualTheme == null)
        {
            result.AddError("MapVisualTheme is not assigned.");
        }
    }

    private void ValidateTheme(MapValidationResult result, bool includeFeatures)
    {
        if (mapVisualTheme == null)
        {
            return;
        }

        IReadOnlyList<MapTileVisualEntry> entries = mapVisualTheme.TileVisualEntries;
        Dictionary<MapTileDirectionMask, int> maskCounts =
            new Dictionary<MapTileDirectionMask, int>();

        if (entries == null)
        {
            result.AddError("MapVisualTheme Tile entries are null.");
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                MapTileVisualEntry entry = entries[i];

                if (entry == null)
                {
                    result.AddError($"MapVisualTheme Tile entry {i} is null.");
                    continue;
                }

                MapTileDirectionMask mask = entry.DirectionMask;

                if (!MapVisualTheme.UsesOnlyDirectionBits(mask))
                {
                    result.AddError($"MapVisualTheme Tile entry {i} uses invalid direction bits: {(int)mask}.");
                }

                if (!MapVisualTheme.IsSupportedTileMask(mask))
                {
                    result.AddError($"MapVisualTheme Tile entry {i} uses unsupported mask {mask}.");
                }

                if (entry.Prefab == null)
                {
                    result.AddError($"MapVisualTheme Tile entry {i} ({mask}) has no prefab.");
                }

                maskCounts.TryGetValue(mask, out int count);
                maskCounts[mask] = count + 1;
            }
        }

        IReadOnlyList<MapTileDirectionMask> requiredMasks = MapVisualTheme.RequiredTileMasks;

        for (int i = 0; i < requiredMasks.Count; i++)
        {
            MapTileDirectionMask requiredMask = requiredMasks[i];
            maskCounts.TryGetValue(requiredMask, out int count);

            if (count == 0)
            {
                result.AddError($"MapVisualTheme is missing required Tile mask {requiredMask}.");
            }
            else if (count > 1)
            {
                result.AddError($"MapVisualTheme contains {count} entries for Tile mask {requiredMask}; exactly one is required.");
            }
        }

        if (entries != null && entries.Count != requiredMasks.Count)
        {
            result.AddError($"MapVisualTheme must contain exactly {requiredMasks.Count} Tile entries; found {entries.Count}.");
        }

        if (!includeFeatures)
        {
            return;
        }

        IReadOnlyList<GameObject> obstacles = mapVisualTheme.ObstaclePrefabs;

        if (obstacles == null || obstacles.Count == 0)
        {
            result.AddError("MapVisualTheme Obstacle list must not be empty.");
        }
        else
        {
            HashSet<GameObject> uniqueObstacles = new HashSet<GameObject>();

            for (int i = 0; i < obstacles.Count; i++)
            {
                GameObject obstacle = obstacles[i];

                if (obstacle == null)
                {
                    result.AddError($"MapVisualTheme Obstacle entry {i} is null.");
                }
                else if (!uniqueObstacles.Add(obstacle))
                {
                    result.AddError($"MapVisualTheme contains duplicate Obstacle prefab {obstacle.name}.");
                }
            }
        }

        if (mapVisualTheme.SpawnPrefab == null)
        {
            result.AddError("MapVisualTheme Spawn prefab is not assigned.");
        }

        if (mapVisualTheme.TargetPrefab == null)
        {
            result.AddError("MapVisualTheme Target prefab is not assigned.");
        }
    }

    private void ValidateVisualRootOwnership(
        GridNodeBehaviour node,
        Transform expectedNodesRoot,
        MapValidationResult result,
        string nodeLabel)
    {
        if (node == null)
        {
            result.AddError($"{nodeLabel} is missing.");
            return;
        }

        Transform visualRoot = node.VisualRoot;
        Transform tileRoot = node.TileVisualRoot;
        Transform featureRoot = node.FeatureVisualRoot;

        if (visualRoot == null)
        {
            result.AddError($"{nodeLabel} has no VisualRoot.");
        }

        if (tileRoot == null)
        {
            result.AddError($"{nodeLabel} has no TileVisualRoot.");
        }

        if (featureRoot == null)
        {
            result.AddError($"{nodeLabel} has no FeatureVisualRoot.");
        }

        if (visualRoot == null || tileRoot == null || featureRoot == null)
        {
            return;
        }

        if (!IsStrictDescendant(visualRoot, node.transform) ||
            visualRoot.GetComponentInParent<GridNodeBehaviour>(true) != node)
        {
            result.AddError($"{nodeLabel} VisualRoot must belong exclusively to that Grid Node hierarchy.");
        }

        if (!IsStrictDescendant(tileRoot, visualRoot) ||
            tileRoot.GetComponentInParent<GridNodeBehaviour>(true) != node)
        {
            result.AddError($"{nodeLabel} TileVisualRoot must be a descendant of its VisualRoot.");
        }

        if (!IsStrictDescendant(featureRoot, visualRoot) ||
            featureRoot.GetComponentInParent<GridNodeBehaviour>(true) != node)
        {
            result.AddError($"{nodeLabel} FeatureVisualRoot must be a descendant of its VisualRoot.");
        }

        if (tileRoot == featureRoot)
        {
            result.AddError($"{nodeLabel} TileVisualRoot and FeatureVisualRoot must be different objects.");
        }
        else if (tileRoot.IsChildOf(featureRoot) || featureRoot.IsChildOf(tileRoot))
        {
            result.AddError($"{nodeLabel} TileVisualRoot and FeatureVisualRoot must not contain one another.");
        }

        if (expectedNodesRoot != null)
        {
            if (!IsStrictDescendant(node.transform, expectedNodesRoot) ||
                !visualRoot.IsChildOf(expectedNodesRoot) ||
                !tileRoot.IsChildOf(expectedNodesRoot) ||
                !featureRoot.IsChildOf(expectedNodesRoot))
            {
                result.AddError($"{nodeLabel} root references must remain inside NodesRoot.");
            }
        }
    }

    private void ReleaseNodeOwners()
    {
        foreach (GridNodeBehaviour node in indexedHierarchy)
            if (node != null) node.ReleaseMapOwner(this);
        indexedHierarchy = System.Array.Empty<GridNodeBehaviour>();
    }

    private void RebuildNodeDictionary()
    {
        GridNodeBehaviour[] nodes = GetHierarchyNodes();
        ReleaseNodeOwners();
        indexedHierarchy = nodes;
        foreach (GridNodeBehaviour node in nodes)
            if (node != null && OwnsNodeHierarchy(node)) node.BindMapOwner(this);
        runtimeIndex.Rebuild(nodes, width, lengh, OwnsNodeHierarchy);
        RememberMapConfiguration();
#if UNITY_EDITOR
        CaptureAuthoringSnapshot(nodes);
#endif
    }

    private GridNodeBehaviour[] GetHierarchyNodes()
    {
#if UNITY_EDITOR
        HierarchyQueryCount++;
#endif
        return nodesRoot != null
            ? nodesRoot.GetComponentsInChildren<GridNodeBehaviour>(true)
            : new GridNodeBehaviour[0];
    }

    private MapTileDirectionMask ResolveDirectionMask(
        GridNodeBehaviour node,
        IReadOnlyDictionary<Vector2Int, GridNodeBehaviour> nodes,
        bool useAuthoredState)
    {
        if (node == null || !IsWalkableForVisual(node, useAuthoredState))
        {
            return MapTileDirectionMask.None;
        }

        MapTileDirectionMask mask = MapTileDirectionMask.None;

        if (IsDirectionWalkable(node.GridPosition + Vector2Int.up, nodes, useAuthoredState))
        {
            mask |= MapTileDirectionMask.Up;
        }

        if (IsDirectionWalkable(node.GridPosition + Vector2Int.down, nodes, useAuthoredState))
        {
            mask |= MapTileDirectionMask.Down;
        }

        if (IsDirectionWalkable(node.GridPosition + Vector2Int.left, nodes, useAuthoredState))
        {
            mask |= MapTileDirectionMask.Left;
        }

        if (IsDirectionWalkable(node.GridPosition + Vector2Int.right, nodes, useAuthoredState))
        {
            mask |= MapTileDirectionMask.Right;
        }

        return mask;
    }

    private bool IsDirectionWalkable(
        Vector2Int gridPosition,
        IReadOnlyDictionary<Vector2Int, GridNodeBehaviour> nodes,
        bool useAuthoredState)
    {
        return nodes.TryGetValue(gridPosition, out GridNodeBehaviour node) &&
               node != null &&
               IsWalkableForVisual(node, useAuthoredState);
    }

    private static bool IsWalkableForVisual(GridNodeBehaviour node, bool useAuthoredState)
    {
        return useAuthoredState ? node.BaseWalkable : node.IsWalkable;
    }

    private static bool HasBaseWalkableRoute(
        GridNodeBehaviour start,
        GridNodeBehaviour target,
        IReadOnlyDictionary<Vector2Int, GridNodeBehaviour> nodes)
    {
        Queue<GridNodeBehaviour> open = new Queue<GridNodeBehaviour>();
        HashSet<GridNodeBehaviour> visited = new HashSet<GridNodeBehaviour>();
        open.Enqueue(start);
        visited.Add(start);

        while (open.Count > 0)
        {
            GridNodeBehaviour current = open.Dequeue();

            if (current == target)
            {
                return true;
            }

            for (int i = 0; i < OrthogonalDirections.Length; i++)
            {
                Vector2Int neighborPosition = current.GridPosition + OrthogonalDirections[i];

                if (!nodes.TryGetValue(neighborPosition, out GridNodeBehaviour neighbor) ||
                    neighbor == null ||
                    !neighbor.BaseWalkable ||
                    !visited.Add(neighbor))
                {
                    continue;
                }

                open.Enqueue(neighbor);
            }
        }

        return false;
    }

    private bool HasGridNodeAncestorInsideNodesRoot(GridNodeBehaviour node)
    {
        Transform parent = node.transform.parent;

        while (parent != null && parent != nodesRoot)
        {
            if (parent.TryGetComponent(out GridNodeBehaviour _))
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    private bool IsNodesRootSafelyOwned()
    {
        return nodesRoot != null &&
               IsStrictDescendant(nodesRoot, transform) &&
               nodesRoot.GetComponent<GridNodeBehaviour>() == null;
    }

    private void ApplyGridCentering()
    {
        Vector3 gridCenterInNodesRoot = new Vector3(
            (width - 1) * nodeSize * 0.5f,
            0f,
            (lengh - 1) * nodeSize * 0.5f);
        Vector3 gridCenterInWorld =
            nodesRoot.TransformPoint(gridCenterInNodesRoot);

        nodesRoot.position += transform.position - gridCenterInWorld;
    }

    private static bool IsStrictDescendant(Transform child, Transform parent)
    {
        return child != null && parent != null && child != parent && child.IsChildOf(parent);
    }

    private static string GetNodeLabel(GridNodeBehaviour node)
    {
        return node != null ? $"Grid Node '{node.name}'" : "Grid Node";
    }

    private static void CreateVisual(GameObject prefab, Transform parent, string instanceName)
    {
        GameObject instance = Instantiate(prefab, parent, false);
        instance.name = instanceName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
    }

    private static void ClearOwnedChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            DestroyOwnedObject(root.GetChild(i).gameObject);
        }
    }

    private static void DestroyOwnedObject(GameObject ownedObject)
    {
        if (ownedObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            ownedObject.SetActive(false);
            Destroy(ownedObject);
        }
        else
        {
            DestroyImmediate(ownedObject);
        }
    }

    private void LogValidationResult(string operation, MapValidationResult result)
    {
        for (int i = 0; i < result.Errors.Count; i++)
        {
            Debug.LogError($"{operation}: {result.Errors[i]}", this);
        }

        for (int i = 0; i < result.Warnings.Count; i++)
        {
            Debug.LogWarning($"{operation}: {result.Warnings[i]}", this);
        }

        if (result.IsValid)
        {
            Debug.Log(
                $"{operation} succeeded with {result.Warnings.Count} warning(s).",
                this);
        }
    }
}
