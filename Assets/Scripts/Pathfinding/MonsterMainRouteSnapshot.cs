using System;
using System.Collections.Generic;

// Read-only ordered node identities, valid only for their originating topology.
internal sealed class MonsterMainRouteSnapshot
{
    internal MonsterMainRouteSnapshot(MapGeneratorBehaviour map,
        AStarPathfindingService pathfinding, IReadOnlyList<GridNodeBehaviour> route)
    {
        Map = map;
        Pathfinding = pathfinding;
        StructureRevision = map.StructureRevision;
        WalkabilityRevision = map.WalkabilityRevision;
        PathBindingRevision = pathfinding.BindingRevision;
        GridNodeBehaviour[] copy = new GridNodeBehaviour[route.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = route[i];
        Route = Array.AsReadOnly(copy);
    }

    internal MapGeneratorBehaviour Map { get; }
    internal AStarPathfindingService Pathfinding { get; }
    internal ulong StructureRevision { get; }
    internal ulong WalkabilityRevision { get; }
    internal ulong PathBindingRevision { get; }
    internal IReadOnlyList<GridNodeBehaviour> Route { get; }

    internal bool IsCurrentFor(MapGeneratorBehaviour map, AStarPathfindingService pathfinding)
    {
        return Map != null && Pathfinding != null && map == Map && pathfinding == Pathfinding &&
               pathfinding.ActiveMap == Map && Map.TryEnsureNodeIndex() &&
               StructureRevision == Map.StructureRevision &&
               WalkabilityRevision == Map.WalkabilityRevision &&
               PathBindingRevision == pathfinding.BindingRevision;
    }
}
