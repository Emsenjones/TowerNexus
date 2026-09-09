using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

internal sealed class TowerPlacementTopologyPlan
{
    private readonly ReadOnlyCollection<GridNodeBehaviour> footprint;
    private readonly ReadOnlyCollection<GridNodeBehaviour> authoritativeRoute;

    internal TowerPlacementTopologyPlan(
        IReadOnlyList<GridNodeBehaviour> footprint,
        IReadOnlyList<GridNodeBehaviour> authoritativeRoute)
    {
        this.footprint = CopyNodes(footprint);
        this.authoritativeRoute = CopyNodes(authoritativeRoute);
    }

    internal IReadOnlyList<GridNodeBehaviour> Footprint => footprint;
    internal IReadOnlyList<GridNodeBehaviour> AuthoritativeRoute =>
        authoritativeRoute;

    private static ReadOnlyCollection<GridNodeBehaviour> CopyNodes(
        IReadOnlyList<GridNodeBehaviour> source)
    {
        GridNodeBehaviour[] copy = source != null
            ? new GridNodeBehaviour[source.Count]
            : Array.Empty<GridNodeBehaviour>();

        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = source[i];
        }

        return Array.AsReadOnly(copy);
    }
}

internal sealed class TowerPlacementPreviewQueryCache
{
    private MapGeneratorBehaviour map;
    private AStarPathfindingService pathfinding;
    private ulong structureRevision, walkabilityRevision, bindingRevision;
    private readonly List<GridNodeBehaviour> footprint = new List<GridNodeBehaviour>();
    private bool hasResult, result;

    internal void Clear()
    {
        hasResult = false;
        map = null;
        pathfinding = null;
        footprint.Clear();
    }

    internal bool TryGet(MapGeneratorBehaviour currentMap, AStarPathfindingService service,
        IReadOnlyList<GridNodeBehaviour> nodes, out bool canPlace)
    {
        canPlace = false;
        if (!hasResult || map != currentMap || pathfinding != service ||
            structureRevision != currentMap.StructureRevision ||
            walkabilityRevision != currentMap.WalkabilityRevision ||
            bindingRevision != service.BindingRevision || footprint.Count != nodes.Count) return false;
        // Resolved footprints are unique. Compare full identities, independent of anchor order.
        for (int i = 0; i < nodes.Count; i++)
            if (!footprint.Contains(nodes[i])) return false;
        canPlace = result;
        return true;
    }

    internal void Store(MapGeneratorBehaviour currentMap, AStarPathfindingService service,
        IReadOnlyList<GridNodeBehaviour> nodes, bool canPlace)
    {
        map = currentMap;
        pathfinding = service;
        structureRevision = map.StructureRevision;
        walkabilityRevision = map.WalkabilityRevision;
        bindingRevision = service.BindingRevision;
        footprint.Clear();
        for (int i = 0; i < nodes.Count; i++) footprint.Add(nodes[i]);
        result = canPlace;
        hasResult = true;
    }
}

public class TowerPlacementValidator : MonoBehaviour
{
    [SerializeField] private AStarPathfindingService pathfindingService;

    private MapGeneratorBehaviour mapGenerator;
    private readonly TowerPlacementPreviewQueryCache previewCache = new TowerPlacementPreviewQueryCache();
#if UNITY_EDITOR
    public int PreviewTopologyQueryCount { get; private set; }
    public int PreviewCacheHitCount { get; private set; }
#endif
    public void InvalidatePreviewCache() => previewCache.Clear();

    public void Initialize(
        MapGeneratorBehaviour mapGenerator,
        AStarPathfindingService pathfindingService)
    {
        InvalidatePreviewCache();
        this.mapGenerator = mapGenerator;
        this.pathfindingService = pathfindingService;
    }

    public bool IsConfiguredFor(
        MapGeneratorBehaviour activeMap,
        AStarPathfindingService activePathfindingService)
    {
        return activeMap != null &&
               mapGenerator == activeMap &&
               pathfindingService == activePathfindingService;
    }

    public bool TryGetOccupiedNodes(TowerPlacementPreview preview, out List<GridNodeBehaviour> occupiedNodes)
    {
        occupiedNodes = new List<GridNodeBehaviour>();

        if (preview == null || mapGenerator == null)
        {
            return false;
        }

        TowerAnchorSet anchorSet = preview.TowerAnchorSet;

        if (anchorSet == null || !anchorSet.IsValid())
        {
            return false;
        }

        IReadOnlyList<Transform> occupiedAnchors = anchorSet.OccupiedAnchors;
        HashSet<GridNodeBehaviour> uniqueNodes = new HashSet<GridNodeBehaviour>();

        for (int i = 0; i < occupiedAnchors.Count; i++)
        {
            Transform occupiedAnchor = occupiedAnchors[i];

            if (occupiedAnchor == null)
            {
                occupiedNodes.Clear();
                return false;
            }

            if (!mapGenerator.TryGetNodeByWorldPosition(occupiedAnchor.position, out GridNodeBehaviour node))
            {
                occupiedNodes.Clear();
                return false;
            }

            if (uniqueNodes.Add(node))
            {
                occupiedNodes.Add(node);
            }
        }

        return occupiedNodes.Count > 0;
    }

    public bool CanPlaceTower(TowerPlacementPreview preview)
    {
        if (!TryResolveQueryEndpoints(out GridNodeBehaviour spawn, out GridNodeBehaviour target,
                out _) || !TryGetOccupiedNodes(preview, out List<GridNodeBehaviour> nodes))
        {
            previewCache.Clear();
            return false;
        }
        if (previewCache.TryGet(mapGenerator, pathfindingService, nodes, out bool cachedResult))
        {
#if UNITY_EDITOR
            PreviewCacheHitCount++;
#endif
            return cachedResult;
        }
#if UNITY_EDITOR
        PreviewTopologyQueryCount++;
#endif
        bool result = TryEvaluateTopology(nodes, spawn, target, out _, out _, out _);
        previewCache.Store(mapGenerator, pathfindingService, nodes, result);
        return result;
    }

    private bool TryResolveQueryEndpoints(out GridNodeBehaviour spawn,
        out GridNodeBehaviour target, out string failureReason)
    {
        spawn = target = null;
        if (mapGenerator == null || pathfindingService == null ||
            pathfindingService.ActiveMap != mapGenerator)
        {
            failureReason = "Map and A* service must be bound to the same Active Map.";
            return false;
        }
        if (!mapGenerator.TryEnsureNodeIndex())
        {
            failureReason = "The Active Map node index is unavailable: " + mapGenerator.NodeIndexFailureReason;
            return false;
        }
        spawn = mapGenerator.GetSpawnNode();
        target = mapGenerator.GetTargetNode();
        if (spawn == null || target == null)
        {
            failureReason = "The Active Map does not provide unambiguous Spawn and Target Grid Nodes.";
            return false;
        }
        failureReason = string.Empty;
        return true;
    }

    private bool TryEvaluateTopology(IReadOnlyList<GridNodeBehaviour> nodes,
        GridNodeBehaviour spawn, GridNodeBehaviour target,
        out List<GridNodeBehaviour> route, out bool? routeExists, out string failureReason)
    {
        route = null;
        routeExists = null;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null || !nodes[i].IsWalkable)
            {
                failureReason = "the candidate Tower footprint contains an unavailable Grid Node.";
                return false;
            }
        }
        // Candidate blockers remain read-only; the same A* order is used by final submission.
        route = pathfindingService.FindPath(spawn, target, nodes);
        routeExists = route != null && route.Count > 0;
        failureReason = routeExists.Value ? string.Empty :
            "the candidate Tower footprint blocks the Spawn-to-Target route.";
        return routeExists.Value;
    }

    internal bool TryCreateTopologyPlan(
        TowerPlacementPreview preview,
        out TowerPlacementTopologyPlan topologyPlan,
        out IReadOnlyList<GridNodeBehaviour> diagnosticFootprint,
        out bool? routeExists,
        out string failureReason,
        bool captureDiagnostics = true)
    {
        topologyPlan = null;
        diagnosticFootprint = null;
        routeExists = null;
        if (!TryResolveQueryEndpoints(out GridNodeBehaviour spawn, out GridNodeBehaviour target,
                out failureReason)) return false;
        if (!TryGetOccupiedNodes(preview, out List<GridNodeBehaviour> nodes))
        {
            failureReason = "the candidate Tower footprint could not be resolved on the Active Map.";
            return false;
        }
        if (!TryEvaluateTopology(nodes, spawn, target, out List<GridNodeBehaviour> route,
                out routeExists, out failureReason))
        {
            diagnosticFootprint = CopyDiagnosticFootprint(nodes, captureDiagnostics);
            return false;
        }
        // A final plan is never retained in, or recovered from, the preview cache.
        topologyPlan = new TowerPlacementTopologyPlan(nodes, route);
        diagnosticFootprint = captureDiagnostics ? topologyPlan.Footprint : null;
        return true;
    }

    private static IReadOnlyList<GridNodeBehaviour> CopyDiagnosticFootprint(
        IReadOnlyList<GridNodeBehaviour> footprint,
        bool captureDiagnostics)
    {
        if (!captureDiagnostics || footprint == null)
        {
            return null;
        }

        GridNodeBehaviour[] copy = new GridNodeBehaviour[footprint.Count];

        for (int i = 0; i < footprint.Count; i++)
        {
            copy[i] = footprint[i];
        }

        return Array.AsReadOnly(copy);
    }
}
