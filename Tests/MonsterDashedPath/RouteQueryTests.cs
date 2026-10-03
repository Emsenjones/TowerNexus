using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Real A*, Map index and Validator; Unity hierarchy/authoring APIs are fixture doubles.
internal static class MonsterPathRouteQueryTests
{
    private static int cases;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception("Monster path query: " + message);
    }
    private static void Case(string name, Action test)
    {
        test(); cases++; Console.WriteLine("PASS route-query: " + name);
    }
    private static string Text(IReadOnlyList<GridNodeBehaviour> route) =>
        string.Join(";", route.Select(n => n.GridPosition.ToString()));
    private static TowerPlacementPreview Preview(params GridNodeBehaviour[] nodes) =>
        new TowerPlacementPreview { TowerAnchorSet = new TowerAnchorSet {
            OccupiedAnchors = nodes.Select(n => new Transform { position = n.WorldPosition }).ToArray() } };
    private sealed class Fixture
    {
        internal readonly MapGeneratorBehaviour Map = new MapGeneratorBehaviour();
        internal readonly AStarPathfindingService Paths = new AStarPathfindingService();
        internal readonly TowerPlacementValidator Validator = new TowerPlacementValidator();
        internal readonly List<GridNodeBehaviour> Nodes = new List<GridNodeBehaviour>();
        internal Fixture()
        {
            for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++)
            {
                var node = new GridNodeBehaviour();
                node.Initialize(new Vector2Int(x, y), true,
                    x == 0 && y == 0 ? GridNodeType.Spawn : x == 2 && y == 2 ? GridNodeType.Target : GridNodeType.Normal);
                node.transform.position = new Vector3(x, 0, y); Nodes.Add(node);
            }
            Map.Configure(3, 3, Nodes); Paths.BindActiveMap(Map); Validator.Initialize(Map, Paths);
        }
        internal GridNodeBehaviour Node(int x, int y) => Map.GetNode(x, y);
        internal GridNodeBehaviour[] Blockers => new[] { Node(0, 1), Node(1, 0) };
    }
    private static void ExpectReadOnly(IReadOnlyList<GridNodeBehaviour> nodes)
    {
        bool rejected = false;
        try { ((IList<GridNodeBehaviour>)nodes)[0] = null; }
        catch (NotSupportedException) { rejected = true; }
        Check(rejected, "published collection refuses mutation");
    }
    private static void Call(object owner, string method) =>
        owner.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(owner, null);

    internal static void Run()
    {
        Case("formal route before Monsters and stable equal-length choice", () => {
            var f = new Fixture();
            Check(f.Paths.TryQueryFormalMainRoute(out var route, out var reason) && reason == "", "formal query succeeds");
            Check(Text(route.Route) == "0,0;0,1;0,2;1,2;2,2", "existing orthogonal tie ordering preserved");
            Check(route.Route[0] == f.Map.GetSpawnNode() && route.Route.Last() == f.Map.GetTargetNode(), "endpoints included");
            ExpectReadOnly(route.Route);
            Check(f.Paths.TryQueryFormalMainRoute(out var again, out _) && Text(route.Route) == Text(again.Route), "stable repeated result");
        });
        Case("available full footprint, cache reorder and unchanged snapshots", () => {
            var f = new Fixture(); var nodes = new[] { f.Node(0, 1), f.Node(1, 1) };
            ulong structure = f.Map.StructureRevision, walkability = f.Map.WalkabilityRevision;
            var originalWalkability = f.Nodes.Select(n => n.IsWalkable).ToArray();
            int searches = f.Paths.SearchCount;
            var result = f.Validator.QueryRoutePreview(Preview(nodes));
            Check(result.Outcome == TowerPlacementRoutePreviewOutcome.RouteAvailable && f.Paths.SearchCount == searches + 1,
                "one candidate search supplies route and validity");
            Check(Text(result.MainRoute.Route) == Text(f.Paths.FindPath(f.Map.GetSpawnNode(), f.Map.GetTargetNode(), nodes)), "all blockers used");
            searches = f.Paths.SearchCount;
            for (int i = 0; i < 100; i++)
                Check(ReferenceEquals(result, f.Validator.QueryRoutePreview(Preview(nodes.Reverse().ToArray()))), "full reordered footprint cache reuse");
            Check(f.Paths.SearchCount == searches, "stationary rich queries search zero additional times");
            Check(result.IsCurrentFor(f.Validator, nodes) && !result.IsCurrentFor(f.Validator, new[] { nodes[0], nodes[0] }), "exact unique identities required");
            string saved = Text(result.MainRoute.Route); ExpectReadOnly(result.MainRoute.Route); ExpectReadOnly(result.Footprint);
            var raw = f.Paths.FindPath(f.Map.GetSpawnNode(), f.Map.GetTargetNode());
            var snapshot = new MonsterMainRouteSnapshot(f.Map, f.Paths, raw); string rawText = Text(snapshot.Route); raw.Clear();
            Check(Text(snapshot.Route) == rawText, "snapshot copies search buffer");
            f.Validator.QueryRoutePreview(Preview(f.Node(2, 0)));
            Check(Text(result.MainRoute.Route) == saved, "later query does not mutate retained route result");
            Check(f.Map.StructureRevision == structure && f.Map.WalkabilityRevision == walkability &&
                originalWalkability.SequenceEqual(f.Nodes.Select(n => n.IsWalkable)), "queries do not mutate topology or occupancy");
        });
        Case("complete multi-cell block and negative cache", () => {
            var f = new Fixture();
            Check(f.Validator.QueryRoutePreview(Preview(f.Node(0, 1))).CanPlace, "single cell does not block");
            int searches = f.Paths.SearchCount;
            var result = f.Validator.QueryRoutePreview(Preview(f.Blockers));
            Check(result.Outcome == TowerPlacementRoutePreviewOutcome.RouteBlocked && result.MainRoute == null, "complete footprint blocks");
            Check(f.Paths.SearchCount == searches + 2, "cold block confirms candidate and committed routes");
            searches = f.Paths.SearchCount;
            Check(ReferenceEquals(result, f.Validator.QueryRoutePreview(Preview(f.Blockers.Reverse().ToArray()))) &&
                f.Paths.SearchCount == searches, "blocked cache hits retain outcome without searches");
        });
        Case("occupied and base-unwalkable candidates use unavailable cache", () => {
            var f = new Fixture(); var node = f.Node(1, 1); node.SetRuntimeOccupied(true);
            int searches = f.Paths.SearchCount;
            var result = f.Validator.QueryRoutePreview(Preview(node));
            Check(result.Outcome == TowerPlacementRoutePreviewOutcome.CandidateUnavailable && result.MainRoute == null, "occupied is not route blocking");
            Check(ReferenceEquals(result, f.Validator.QueryRoutePreview(Preview(node))) && f.Paths.SearchCount == searches,
                "unavailable resolved footprint cache is readable and needs no A*");
            node.ResetRuntimeState(); node.SetBaseWalkable(false);
            Check(!result.IsCurrentFor(f.Validator, new[] { node }), "occupied outcome invalidates on mutation");
            Check(f.Validator.QueryRoutePreview(Preview(node)).Outcome == TowerPlacementRoutePreviewOutcome.CandidateUnavailable,
                "base-unwalkable is not route blocking");
        });
        Case("partial out-of-Map and malformed anchors clear old cache", () => {
            var f = new Fixture(); var node = f.Node(1, 1); var preview = Preview(node);
            var old = f.Validator.QueryRoutePreview(preview);
            preview.TowerAnchorSet.OccupiedAnchors = new[] { new Transform { position = node.WorldPosition }, new Transform { position = new Vector3(-5, 0, -5) } };
            var invalid = f.Validator.QueryRoutePreview(preview);
            Check(invalid.Outcome == TowerPlacementRoutePreviewOutcome.CandidateUnavailable && !invalid.CanCache && invalid.Footprint.Count == 0,
                "no partial footprint cached");
            Check(!ReferenceEquals(old, f.Validator.QueryRoutePreview(Preview(node))), "out-of-Map cleared old cache");
            foreach (var anchors in new[] { Array.Empty<Transform>(), new Transform[] { null } })
            {
                old = f.Validator.QueryRoutePreview(Preview(node)); preview.TowerAnchorSet.OccupiedAnchors = anchors;
                Check(f.Validator.QueryRoutePreview(preview).Outcome == TowerPlacementRoutePreviewOutcome.CandidateUnavailable, "malformed anchors unavailable");
                Check(!ReferenceEquals(old, f.Validator.QueryRoutePreview(Preview(node))), "malformed anchors clear cache");
            }
        });
        Case("technical errors are uncached and diagnosable", () => {
            var f = new Fixture(); var node = f.Node(1, 1);
            var old = f.Validator.QueryRoutePreview(Preview(node)); f.Paths.ClearActiveMap();
            var failure = f.Validator.QueryRoutePreview(Preview(node));
            Check(failure.Outcome == TowerPlacementRoutePreviewOutcome.TechnicalFailure && !failure.CanCache && failure.FailureReason.Length > 0,
                "lost binding is technical");
            Check(!f.Paths.TryQueryFormalMainRoute(out _, out var reason) && reason.Length > 0, "unbound formal query fails");
            f.Paths.BindActiveMap(f.Map); Check(!ReferenceEquals(old, f.Validator.QueryRoutePreview(Preview(node))), "binding failure clears cache");
            f.Map.GetSpawnNode().SetNodeType(GridNodeType.Normal);
            Check(!f.Paths.TryQueryFormalMainRoute(out _, out reason) && reason.Length > 0, "missing formal endpoint fails");
            Check(f.Validator.QueryRoutePreview(Preview(node)).Outcome == TowerPlacementRoutePreviewOutcome.TechnicalFailure, "missing endpoint is technical");
            f.Node(0, 0).SetNodeType(GridNodeType.Spawn); f.Node(0, 0).SetRuntimeOccupied(true);
            Check(f.Validator.QueryRoutePreview(Preview(node)).Outcome == TowerPlacementRoutePreviewOutcome.TechnicalFailure, "unwalkable formal endpoint is technical");
            f.Node(0, 0).ResetRuntimeState(); var validPreview = Preview(f.Node(2, 0));
            f.Node(1, 1).SetGridPosition(new Vector2Int(0, 0));
            Check(f.Validator.QueryRoutePreview(validPreview).Outcome == TowerPlacementRoutePreviewOutcome.TechnicalFailure,
                "invalid node index is technical");
        });
        Case("committed graph without a route cannot report candidate blocking", () => {
            var f = new Fixture(); foreach (var node in f.Blockers) node.SetRuntimeOccupied(true);
            Check(!f.Paths.TryQueryFormalMainRoute(out _, out var reason) && reason.Length > 0, "committed formal route unavailable");
            var result = f.Validator.QueryRoutePreview(Preview(f.Node(1, 1)));
            Check(result.Outcome == TowerPlacementRoutePreviewOutcome.TechnicalFailure && !result.CanCache, "formal graph failure is not candidate blocking");
            int searches = f.Paths.SearchCount; f.Validator.QueryRoutePreview(Preview(f.Node(1, 1)));
            Check(f.Paths.SearchCount > searches, "technical failure is not cached");
        });
        foreach (var outcome in new[] { TowerPlacementRoutePreviewOutcome.RouteAvailable,
            TowerPlacementRoutePreviewOutcome.RouteBlocked, TowerPlacementRoutePreviewOutcome.CandidateUnavailable })
        foreach (string mutation in new[] { "walkability", "structure", "path-rebind", "validator-rebind", "map-replace", "service-replace", "release" })
            Case(outcome + " invalidates on " + mutation, () => {
                var f = new Fixture(); var nodes = outcome == TowerPlacementRoutePreviewOutcome.RouteBlocked ? f.Blockers : new[] { f.Node(1, 1) };
                if (outcome == TowerPlacementRoutePreviewOutcome.CandidateUnavailable) nodes[0].SetRuntimeOccupied(true);
                var result = f.Validator.QueryRoutePreview(Preview(nodes));
                Check(result.Outcome == outcome && result.IsCurrentFor(f.Validator, nodes), "held result initially current");
                switch (mutation)
                {
                    case "walkability": f.Node(2, 0).SetRuntimeOccupied(true); break;
                    case "structure": f.Map.NotifyNodeStructureChanged(); break;
                    case "path-rebind": f.Paths.BindActiveMap(f.Map); break;
                    case "validator-rebind": f.Validator.Initialize(f.Map, f.Paths); break;
                    case "map-replace": var next = new Fixture(); f.Paths.BindActiveMap(next.Map); f.Validator.Initialize(next.Map, f.Paths); break;
                    case "service-replace": var service = new AStarPathfindingService(); service.BindActiveMap(f.Map); f.Validator.Initialize(f.Map, service); break;
                    case "release": Call(f.Map, "OnDisable"); break;
                }
                Check(!result.IsCurrentFor(f.Validator, nodes), "old outcome rejects changed source independently of route presence");
                if (result.MainRoute != null && mutation != "validator-rebind")
                    Check(!result.MainRoute.IsCurrentFor(f.Validator.ActiveMap, f.Validator.ActivePathfinding), "old route snapshot rejects changed source");
            });
        Case("new footprint cannot match previous outcome", () => {
            var f = new Fixture(); var old = f.Validator.QueryRoutePreview(Preview(f.Node(1, 1)));
            Check(!old.IsCurrentFor(f.Validator, new[] { f.Node(2, 0) }), "footprint identity freshness");
            Check(!ReferenceEquals(old, f.Validator.QueryRoutePreview(Preview(f.Node(2, 0)))), "new footprint recomputes");
        });
        Console.WriteLine("PASS " + cases + " Monster dashed-path route-query cases");
    }
}
