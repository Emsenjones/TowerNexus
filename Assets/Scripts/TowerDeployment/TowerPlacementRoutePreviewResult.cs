using System;
using System.Collections.Generic;

internal enum TowerPlacementRoutePreviewOutcome
{
    RouteAvailable,
    RouteBlocked,
    CandidateUnavailable,
    TechnicalFailure
}

internal sealed class TowerPlacementRoutePreviewResult
{
    private readonly TowerPlacementValidator validator;

    internal TowerPlacementRoutePreviewResult(TowerPlacementValidator validator,
        TowerPlacementRoutePreviewOutcome outcome, IReadOnlyList<GridNodeBehaviour> footprint,
        MonsterMainRouteSnapshot mainRoute, string failureReason)
    {
        this.validator = validator;
        Map = validator.ActiveMap;
        Pathfinding = validator.ActivePathfinding;
        ValidatorBindingRevision = validator.BindingRevision;
        StructureRevision = Map != null ? Map.StructureRevision : 0;
        WalkabilityRevision = Map != null ? Map.WalkabilityRevision : 0;
        PathBindingRevision = Pathfinding != null ? Pathfinding.BindingRevision : 0;
        Outcome = outcome;
        MainRoute = mainRoute;
        FailureReason = failureReason;
        GridNodeBehaviour[] copy = new GridNodeBehaviour[footprint != null ? footprint.Count : 0];
        for (int i = 0; i < copy.Length; i++) copy[i] = footprint[i];
        Footprint = Array.AsReadOnly(copy);
    }

    internal TowerPlacementRoutePreviewOutcome Outcome { get; }
    internal bool CanPlace => Outcome == TowerPlacementRoutePreviewOutcome.RouteAvailable;
    internal MonsterMainRouteSnapshot MainRoute { get; }
    internal string FailureReason { get; }
    internal IReadOnlyList<GridNodeBehaviour> Footprint { get; }
    internal MapGeneratorBehaviour Map { get; }
    internal AStarPathfindingService Pathfinding { get; }
    internal ulong ValidatorBindingRevision { get; }
    internal ulong StructureRevision { get; }
    internal ulong WalkabilityRevision { get; }
    internal ulong PathBindingRevision { get; }
    internal bool CanCache => Outcome != TowerPlacementRoutePreviewOutcome.TechnicalFailure &&
                              Footprint.Count > 0;

    // Blocked and unavailable results have their own provenance, even without a route.
    // Frame/drag authority belongs to final Candidate and interaction, not preview caching.
    internal bool IsCurrentFor(TowerPlacementValidator owner, IReadOnlyList<GridNodeBehaviour> nodes)
    {
        if (!CanCache || owner == null || owner != validator || Map == null || Pathfinding == null ||
            owner.ActiveMap != Map || owner.ActivePathfinding != Pathfinding ||
            Pathfinding.ActiveMap != Map || !Map.TryEnsureNodeIndex() ||
            ValidatorBindingRevision != owner.BindingRevision ||
            StructureRevision != Map.StructureRevision || WalkabilityRevision != Map.WalkabilityRevision ||
            PathBindingRevision != Pathfinding.BindingRevision || nodes == null || nodes.Count != Footprint.Count)
            return false;

        // Stored footprints are unique. Require every stored identity, so a duplicate
        // input cannot accidentally substitute for a missing cell.
        for (int i = 0; i < Footprint.Count; i++)
        {
            bool found = false;
            for (int j = 0; j < nodes.Count; j++)
                if (Footprint[i] == nodes[j]) { found = true; break; }
            if (!found) return false;
        }
        return MainRoute == null || MainRoute.IsCurrentFor(Map, Pathfinding);
    }
}
