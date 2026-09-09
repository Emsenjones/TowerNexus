using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
class QueryTests
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static object Call(object owner, string name) => owner.GetType().GetMethod(name, BindingFlags.NonPublic|BindingFlags.Instance).Invoke(owner,null);
    static TowerPlacementPreview Preview(params GridNodeBehaviour[] nodes) => new TowerPlacementPreview {
        TowerAnchorSet = new TowerAnchorSet { OccupiedAnchors = nodes.Select(n=>new Transform {position=n.WorldPosition}).ToArray() }
    };
    static string PathText(IReadOnlyList<GridNodeBehaviour> nodes) => string.Join(";",nodes.Select(n=>n.GridPosition.ToString()));
    static void Main(string[] args)
    {
        foreach(string file in args)
        {
            string[] lines=File.ReadAllLines(file);string[] size=lines[0].Split(' ');
            int width=int.Parse(size[0]),height=int.Parse(size[1]);
            var nodes=new List<GridNodeBehaviour>();
            foreach(string line in lines.Skip(1))
            {
                int[] values=line.Split(' ').Select(int.Parse).ToArray();
                var node=new GridNodeBehaviour();node.Initialize(new Vector2Int(values[0],values[1]),values[2]!=0,(GridNodeType)values[3]);
                node.transform.position=new Vector3(values[0],0,values[1]);nodes.Add(node);
            }
            var map=new MapGeneratorBehaviour();map.Configure(width,height,nodes);
            var astar=new AStarPathfindingService();astar.BindActiveMap(map);
            var validator=new TowerPlacementValidator();validator.Initialize(map,astar);
            var spawn=map.GetSpawnNode();var target=map.GetTargetNode();
            Check(spawn!=null && target!=null,"authored endpoints");
            var ordered=nodes.OrderBy(n=>n.GridPosition.x).ThenBy(n=>n.GridPosition.y).ToArray();
            for(int i=0;i<ordered.Length;i++)
            {
                var footprint=i+1<ordered.Length ? new[]{ordered[i],ordered[i+1]} : new[]{ordered[i]};
                var preview=Preview(footprint);
                bool valid=validator.CanPlaceTower(preview);
                bool final=validator.TryCreateTopologyPlan(preview,out var plan,out _,out _,out _);
                var route=astar.FindPath(spawn,target,footprint);
                Console.WriteLine("CASE|"+width+"|"+i+"|"+valid+"|"+final+"|"+PathText(route));
                Check(valid==final,"preview and final agree");
                if (route.Count > 1)
                {
                    bool connected = astar.TryBuildRouteConnectivityMap(route, footprint, out var connectivity);
                    Check(connected, "route connectivity preparation succeeds");
                    foreach (var origin in ordered)
                    {
                        if (!connectivity.TryGetConnection(origin, out int distance, out var join)) continue;
                        var connector = connectivity.BuildPathToJoin(origin);
                        Check(join != target && connector.Count == distance + 1 && connector[0] == origin &&
                            connector[connector.Count - 1] == join, "connector preserves origin and joins a non-Target route grid");
                        Console.WriteLine("CASE|connector|" + width + "|" + i + "|" + origin.GridPosition + "|" + PathText(connector));
                    }
                }
            }
            // Same fixture/pointer sequence, cold first query and 99 identical repeats.
            var candidate=ordered.First(n=>n.IsWalkable && n!=spawn && n!=target);
            var stationary=Preview(candidate);validator.Initialize(map,astar);
            int scans=Transform.HierarchyScans, searches=astar.SearchCount;
            GC.Collect();long bytes=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();
            for(int i=0;i<100;i++)validator.CanPlaceTower(stationary);
            timer.Stop();bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
            Console.WriteLine($"BENCH {width}x{height}, 100 stationary queries: scans={Transform.HierarchyScans-scans}, searches={astar.SearchCount-searches}, managed_ms={timer.Elapsed.TotalMilliseconds:F3}, managed_bytes={bytes}");
#if !BASELINE
            Check(Transform.HierarchyScans==scans,"hot preview performs no hierarchy scans");
            Check(astar.SearchCount-searches==1,"stationary preview searches once");
            Check(validator.PreviewCacheHitCount>=99,"stationary cache hits");
            int count=astar.SearchCount;
            validator.TryCreateTopologyPlan(stationary,out _,out _,out _,out _);
            Check(astar.SearchCount==count+1,"final submission searches afresh despite cache");
            RunInvalidation(map,astar,validator,candidate,spawn,stationary);
#endif
        }
    }
#if !BASELINE
    static void RunInvalidation(MapGeneratorBehaviour map,AStarPathfindingService astar,TowerPlacementValidator validator,
        GridNodeBehaviour candidate,GridNodeBehaviour spawn,TowerPlacementPreview preview)
    {
        ulong structure=map.StructureRevision, walk=map.WalkabilityRevision;
        int builds=map.NodeIndexBuildCount;
        for(int i=0;i<100;i++)map.GetNode(candidate.GridPosition);
        Check(map.StructureRevision==structure && map.WalkabilityRevision==walk && map.NodeIndexBuildCount==builds,"reads do not invalidate or rebuild");
        candidate.SetRuntimeOccupied(true);Check(map.WalkabilityRevision>walk,"occupancy invalidates");
        Check(!validator.CanPlaceTower(preview),"occupied footprint rejected");
        candidate.ResetRuntimeState();Check(validator.CanPlaceTower(preview)==astar.HasValidPath(map.GetSpawnNode(),map.GetTargetNode(),new[]{candidate}),"reset invalidates");
        int queries=validator.PreviewTopologyQueryCount;astar.BindActiveMap(map);validator.CanPlaceTower(preview);
        Check(validator.PreviewTopologyQueryCount==queries+1,"same-map rebind invalidates");
        preview.TowerAnchorSet.Valid=false;Check(!validator.CanPlaceTower(preview),"invalid anchor cannot hit old result");preview.TowerAnchorSet.Valid=true;
        preview.TowerAnchorSet.OccupiedAnchors[0].position=new Vector3(-10,0,-10);Check(!validator.CanPlaceTower(preview),"out of bounds cannot hit old result");
        preview.TowerAnchorSet.OccupiedAnchors[0].position=candidate.WorldPosition;
        Vector2Int position=candidate.GridPosition;candidate.SetGridPosition(spawn.GridPosition);
        Check(!map.TryEnsureNodeIndex() && map.GetNode(position)==null && map.GetSpawnNode()==null,"duplicate rejects entire index");
        candidate.SetGridPosition(position);Check(map.TryEnsureNodeIndex(),"duplicate repair recovers");
        bool originalWalkable=candidate.BaseWalkable;
        candidate.SetBaseWalkable(!originalWalkable);walk=map.WalkabilityRevision;
        Call(map,"CheckEditorTopology");Check(map.WalkabilityRevision==walk,"editor reconciliation does not double-invalidate notified authoring changes");
        candidate.SetBaseWalkable(originalWalkable);Call(map,"CheckEditorTopology");
        GridNodeType role=spawn.NodeType;spawn.SetNodeType(GridNodeType.Normal);
        Check(map.GetSpawnNode()==null && map.TryEnsureNodeIndex(),"role changes invalidate endpoint without requiring full Stage readiness");
        spawn.SetNodeType(role);Check(map.GetSpawnNode()==spawn,"role repair updates endpoint");
        int before=map.NodeIndexBuildCount;structure=map.StructureRevision;walk=map.WalkabilityRevision;
        new Transform().SetParent(candidate.transform);Call(map,"CheckEditorTopology");map.GetNode(position);
        Check(map.NodeIndexBuildCount==before && map.StructureRevision==structure && map.WalkabilityRevision==walk,"visual-only hierarchy change keeps topology");
        var second=new MapGeneratorBehaviour();var only=new GridNodeBehaviour();only.Initialize(new Vector2Int(0,0),true);second.Configure(1,1,new[]{only});
        Check(second.TryEnsureNodeIndex() && second.GetSpawnNode()==null,"all-Normal scaffold queryable");
        ulong oldRevision=map.StructureRevision,newRevision=second.StructureRevision;
        candidate.transform.SetParent(second.NodesRoot);candidate.RefreshMapOwnership();
        Check(map.StructureRevision>oldRevision && second.StructureRevision>newRevision,"transfer invalidates both maps");
        Check(!map.TryEnsureNodeIndex() && !second.TryEnsureNodeIndex(),"malformed transfer fails closed");
        candidate.transform.SetParent(map.NodesRoot);candidate.RefreshMapOwnership();Check(map.TryEnsureNodeIndex() && second.TryEnsureNodeIndex(),"transfer repair recovers");
        Call(map,"OnDisable");Check(!map.TryEnsureNodeIndex() && candidate.MapOwner==null,"release detaches owners and closes index");
        Call(map,"OnEnable");Check(map.TryEnsureNodeIndex(),"reenable rebuilds");
        Console.WriteLine("PASS index/revision/preview/rebind/invalid-footprint/repair/authoring-transfer/release boundaries");
    }
#endif
}
