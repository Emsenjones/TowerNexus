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

public class TowerPlacementValidator : MonoBehaviour
{
    [SerializeField] private AStarPathfindingService pathfindingService;

    private MapGeneratorBehaviour mapGenerator;

    public void Initialize(
        MapGeneratorBehaviour mapGenerator,
        AStarPathfindingService pathfindingService)
    {
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
        return TryCreateTopologyPlan(
            preview,
            out _,
            out _,
            out _,
            out _,
            captureDiagnostics: false);
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

        if (!TryGetOccupiedNodes(preview, out List<GridNodeBehaviour> occupiedNodes))
        {
            failureReason =
                "the candidate Tower footprint could not be resolved on the Active Map.";
            return false;
        }

        for (int i = 0; i < occupiedNodes.Count; i++)
        {
            GridNodeBehaviour node = occupiedNodes[i];

            if (node == null || !node.IsWalkable)
            {
                diagnosticFootprint = CopyDiagnosticFootprint(
                    occupiedNodes,
                    captureDiagnostics);
                failureReason =
                    "the candidate Tower footprint contains an unavailable Grid Node.";
                return false;
            }
        }

        if (pathfindingService == null || mapGenerator == null)
        {
            diagnosticFootprint = CopyDiagnosticFootprint(
                occupiedNodes,
                captureDiagnostics);
            failureReason =
                "the Active Map or A* pathfinding service is not assigned.";
            return false;
        }

        if (pathfindingService.ActiveMap != mapGenerator)
        {
            diagnosticFootprint = CopyDiagnosticFootprint(
                occupiedNodes,
                captureDiagnostics);
            failureReason =
                "the A* pathfinding service is not bound to the same Active Map.";
            return false;
        }

        GridNodeBehaviour spawnNode = mapGenerator.GetSpawnNode();
        GridNodeBehaviour targetNode = mapGenerator.GetTargetNode();

        if (spawnNode == null || targetNode == null)
        {
            diagnosticFootprint = CopyDiagnosticFootprint(
                occupiedNodes,
                captureDiagnostics);
            failureReason =
                "the Active Map does not provide both Spawn and Target Grid Nodes.";
            return false;
        }

        List<GridNodeBehaviour> authoritativeRoute = pathfindingService.FindPath(
            spawnNode,
            targetNode,
            occupiedNodes);
        routeExists =
            authoritativeRoute != null && authoritativeRoute.Count > 0;

        if (!routeExists.Value)
        {
            diagnosticFootprint = CopyDiagnosticFootprint(
                occupiedNodes,
                captureDiagnostics);
            failureReason =
                "the candidate Tower footprint blocks the Spawn-to-Target route.";
            return false;
        }

        topologyPlan = new TowerPlacementTopologyPlan(
            occupiedNodes,
            authoritativeRoute);
        diagnosticFootprint = captureDiagnostics
            ? topologyPlan.Footprint
            : null;
        failureReason = string.Empty;
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
