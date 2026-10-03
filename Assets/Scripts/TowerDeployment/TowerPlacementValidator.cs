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
    private TowerPlacementRoutePreviewResult result;

    internal void Clear() => result = null;

    internal bool TryGet(TowerPlacementValidator validator,
        IReadOnlyList<GridNodeBehaviour> nodes, out TowerPlacementRoutePreviewResult preview)
    {
        preview = null;
        if (result == null || !result.IsCurrentFor(validator, nodes)) return false;
        preview = result;
        return true;
    }

    internal void Store(TowerPlacementRoutePreviewResult preview)
    {
        result = preview.CanCache ? preview : null;
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
    internal MapGeneratorBehaviour ActiveMap => mapGenerator;
    internal AStarPathfindingService ActivePathfinding => pathfindingService;
    internal ulong BindingRevision { get; private set; }
    internal ulong PathBindingRevision => pathfindingService != null ? pathfindingService.BindingRevision : 0;
    public void InvalidatePreviewCache() => previewCache.Clear();

    public void Initialize(
        MapGeneratorBehaviour mapGenerator,
        AStarPathfindingService pathfindingService)
    {
        BindingRevision++;
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
        return QueryRoutePreview(preview).CanPlace;
    }

    internal TowerPlacementRoutePreviewResult QueryRoutePreview(TowerPlacementPreview preview)
    {
        if (!TryResolveQueryEndpoints(out GridNodeBehaviour spawn, out GridNodeBehaviour target,
                out string failureReason))
            return UncachedPreview(TowerPlacementRoutePreviewOutcome.TechnicalFailure, failureReason);
        if (!spawn.IsWalkable || !target.IsWalkable)
            return UncachedPreview(TowerPlacementRoutePreviewOutcome.TechnicalFailure,
                "The committed Spawn and Target nodes must be walkable.");
        if (!TryGetOccupiedNodes(preview, out List<GridNodeBehaviour> nodes))
            return UncachedPreview(TowerPlacementRoutePreviewOutcome.CandidateUnavailable,
                "The complete candidate footprint cannot be resolved on the Active Map.");
        if (previewCache.TryGet(this, nodes, out TowerPlacementRoutePreviewResult cachedResult))
        {
#if UNITY_EDITOR
            PreviewCacheHitCount++;
#endif
            return cachedResult;
        }
#if UNITY_EDITOR
        PreviewTopologyQueryCount++;
#endif
        ulong validatorBinding = BindingRevision, pathBinding = pathfindingService.BindingRevision,
            structure = mapGenerator.StructureRevision, walkability = mapGenerator.WalkabilityRevision;
        MapGeneratorBehaviour map = mapGenerator;
        AStarPathfindingService paths = pathfindingService;
        bool canPlace = TryEvaluateTopology(nodes, spawn, target, out List<GridNodeBehaviour> route,
            out bool? routeExists, out failureReason);
        var outcome = canPlace ? TowerPlacementRoutePreviewOutcome.RouteAvailable :
            routeExists == false ? TowerPlacementRoutePreviewOutcome.RouteBlocked :
            TowerPlacementRoutePreviewOutcome.CandidateUnavailable;
        // Empty candidate search only proves blocking when the committed topology
        // still has a formal route. Never turn a broken runtime Map into a red preview.
        if (outcome == TowerPlacementRoutePreviewOutcome.RouteBlocked &&
            !paths.TryQueryFormalMainRoute(out _, out string formalFailure))
            return UncachedPreview(TowerPlacementRoutePreviewOutcome.TechnicalFailure, formalFailure);
        if (mapGenerator != map || pathfindingService != paths || BindingRevision != validatorBinding ||
            paths.ActiveMap != map || paths.BindingRevision != pathBinding || !map.TryEnsureNodeIndex() ||
            map.StructureRevision != structure || map.WalkabilityRevision != walkability)
            return UncachedPreview(TowerPlacementRoutePreviewOutcome.TechnicalFailure,
                "The candidate query binding or topology changed during evaluation.");

        MonsterMainRouteSnapshot mainRoute = canPlace ? new MonsterMainRouteSnapshot(map, paths, route) : null;
        var result = new TowerPlacementRoutePreviewResult(this, outcome, nodes, mainRoute, failureReason);
        previewCache.Store(result);
        return result;
    }

    private TowerPlacementRoutePreviewResult UncachedPreview(TowerPlacementRoutePreviewOutcome outcome,
        string failureReason)
    {
        previewCache.Clear();
        return new TowerPlacementRoutePreviewResult(this, outcome, null, null, failureReason);
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
        TowerPlacementCandidate candidate,
        out TowerPlacementTopologyPlan topologyPlan,
        out IReadOnlyList<GridNodeBehaviour> diagnosticFootprint,
        out bool? routeExists,
        out string failureReason,
        bool captureDiagnostics = true)
    {
        topologyPlan = null; diagnosticFootprint = null; routeExists = null;
        if (!TryResolveQueryEndpoints(out GridNodeBehaviour spawn, out GridNodeBehaviour target,
                out failureReason)) return false;
        if (candidate == null || !candidate.IsCurrent(this) || candidate.Footprint.Count == 0)
        { failureReason = "The deployment candidate expired or has no footprint."; return false; }
        var nodes = candidate.Footprint;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null || mapGenerator.GetNode(nodes[i].GridPosition) != nodes[i])
            { failureReason = "The footprint contains a foreign Map node."; return false; }
        }
        if (!TryEvaluateTopology(nodes, spawn, target, out List<GridNodeBehaviour> route,
                out routeExists, out failureReason))
        {
            diagnosticFootprint = CopyDiagnosticFootprint(nodes, captureDiagnostics);
            return false;
        }
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
