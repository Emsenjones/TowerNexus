using System;
using System.Reflection;
using UnityEngine;
class SessionTests
{
    static int count;
    static void Check(bool result,string message){if(!result)throw new Exception(message);}
    class Fixture
    {
        internal MapGeneratorBehaviour Map=new MapGeneratorBehaviour();
        internal AStarPathfindingService Paths=new AStarPathfindingService();
        internal TowerPlacementValidator Validator=new TowerPlacementValidator();
        internal MonsterDashedPathPresenter Presenter=new MonsterDashedPathPresenter();
        internal LineRenderer Line=new LineRenderer{widthMultiplier=.1f};
        internal MonsterDashedPathSession Session;
        internal GridNodeBehaviour[] Formal,Candidate;
        internal int Reports;
        internal object Drag;
        internal Fixture()
        {
            Line.transform.parent=Presenter.transform;
            typeof(MonsterDashedPathPresenter).GetField("lineRenderer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(Presenter,Line);
            Formal=new[]{Node(0,0,GridNodeType.Spawn),Node(1,0,GridNodeType.Normal),Node(1,1,GridNodeType.Target)};
            Candidate=new[]{Formal[0],Node(0,1,GridNodeType.Normal),Formal[2]};
            Paths.ActiveMap=Map;Paths.FormalNodes=Formal;Validator.ActiveMap=Map;Validator.ActivePathfinding=Paths;
            Session=new MonsterDashedPathSession(Map,Paths,Validator,Presenter,_=>Reports++);
            Session.Tick();
        }
        GridNodeBehaviour Node(int x,int y,GridNodeType type)=>new GridNodeBehaviour{MapOwner=Map,GridPosition=new Vector2Int(x,y),WorldPosition=new Vector3(x,0,y),NodeType=type};
        internal TowerPlacementRoutePreviewResult Result(TowerPlacementRoutePreviewOutcome outcome)=>new TowerPlacementRoutePreviewResult(Validator,outcome,new[]{Formal[1]},outcome==TowerPlacementRoutePreviewOutcome.RouteAvailable?new MonsterMainRouteSnapshot(Map,Paths,Candidate):null,"technical query failed");
        internal void Begin(){Drag=Session.BeginDrag();}
        internal void Preview(TowerPlacementRoutePreviewOutcome outcome)=>Session.Preview(Drag,Result(outcome),false);
        internal bool Red=>Line.colorGradient.colorKeys[0].color.g==0;
        internal float Alpha=>Line.colorGradient.alphaKeys[0].alpha;
    }
    static void Run(string name,Action<Fixture> test){test(new Fixture());count++;Console.WriteLine("PASS "+name);}
    static void Main()
    {
        Run("Battle baseline before monsters / drag preserves Normal appearance",f=>{Check(f.Line.enabled&&f.Alpha==.5f&&f.Paths.SearchCount==1,"baseline");f.Begin();Check(f.Alpha==.5f&&!f.Red,"begin");});
        Run("valid blocked blocked valid",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteAvailable);var p=f.Line.Positions[1];int writes=f.Line.Writes;f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);Check(f.Red&&f.Line.Writes==writes&&f.Line.Positions[1].Equals(p),"blocked geometry");f.Preview(TowerPlacementRoutePreviewOutcome.RouteAvailable);Check(!f.Red&&f.Alpha==.5f,"recovery");});
        Run("immediately blocked uses formal seed",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);Check(f.Red&&f.Line.Positions[1].x==1,"seed");});
        Run("fallback becomes retained baseline",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteAvailable);f.Session.Preview(f.Drag,null,true);f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);Check(f.Red&&f.Line.Positions[1].x==1,"fallback retained");});
        Run("unavailable uses white formal",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteAvailable);f.Preview(TowerPlacementRoutePreviewOutcome.CandidateUnavailable);Check(!f.Red&&f.Line.Positions[1].x==1,"unavailable");});
        Run("valid technical blocked restores retained geometry",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteAvailable);var p=f.Line.Positions[1];f.Preview(TowerPlacementRoutePreviewOutcome.TechnicalFailure);Check(!f.Line.enabled,"technical hiding");f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);Check(f.Line.enabled&&f.Red&&f.Line.Positions[1].Equals(p),"technical blocked recovery");});
        Run("technical fallback restores formal",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.TechnicalFailure);f.Session.Preview(f.Drag,null,true);Check(f.Line.enabled&&!f.Red&&f.Line.Positions[1].x==1,"technical fallback");});
        Run("technical diagnostics deduplicated",f=>{f.Begin();for(int i=0;i<100;i++)f.Preview(TowerPlacementRoutePreviewOutcome.TechnicalFailure);Check(f.Reports==1,"log spam");});
        Run("topology change discards hidden retained candidate",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteAvailable);f.Preview(TowerPlacementRoutePreviewOutcome.TechnicalFailure);f.Map.WalkabilityRevision++;f.Session.Tick();f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);Check(f.Red&&f.Line.Positions[1].x==1&&f.Paths.SearchCount==2,"stale retained");});
        Run("cancel restores Normal / stale drag ignored",f=>{f.Begin();var old=f.Drag;f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);f.Session.EndDrag(old);Check(f.Alpha==.5f&&!f.Red,"cancel");f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteAvailable);f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);f.Session.EndDrag(old);Check(f.Red&&f.Line.Positions[1].z==1,"old cancel adopted new drag");});
        Run("revocation before callbacks / repeated close",f=>{f.Begin();var route=new MonsterMainRouteSnapshot(f.Map,f.Paths,f.Candidate);f.Session.Close();f.Session.Preview(f.Drag,f.Result(TowerPlacementRoutePreviewOutcome.RouteAvailable),false);f.Session.AcceptCommittedRoute(route);f.Session.EndDrag(f.Drag);f.Session.Tick();f.Session.Close();Check(!f.Line.enabled&&!f.Session.IsActive,"restored outgoing display");});
        Run("commit captures post revision / no second search",f=>{f.Begin();f.Map.WalkabilityRevision++;var route=f.Session.CaptureCommittedRoute(new TowerPlacementTopologyPlan{AuthoritativeRoute=f.Candidate});Check(route.WalkabilityRevision==f.Map.WalkabilityRevision,"revision");f.Session.AcceptCommittedRoute(route);f.Session.EndDrag(f.Drag);Check(f.Alpha==.5f&&f.Line.Positions[1].z==1&&f.Paths.SearchCount==1,"commit");});
        Run("late stale commit cannot overwrite newer topology",f=>{var route=f.Session.CaptureCommittedRoute(new TowerPlacementTopologyPlan{AuthoritativeRoute=f.Candidate});f.Map.WalkabilityRevision++;f.Session.Tick();int writes=f.Line.Writes;f.Session.AcceptCommittedRoute(route);Check(f.Line.Writes==writes&&f.Line.Positions[1].x==1,"stale commit");});
        Run("validator binding invalidates retained candidate",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteAvailable);f.Validator.BindingRevision++;f.Session.Tick();Check(f.Line.Positions[1].x==1,"validator invalidation");});
        Run("path revision rebuilds baseline",f=>{f.Paths.BindingRevision++;f.Session.Tick();Check(f.Paths.SearchCount==2,"path invalidation");});
        Run("foreign Map cannot be adopted",f=>{f.Paths.ActiveMap=new MapGeneratorBehaviour();f.Session.Tick();Check(!f.Line.enabled&&f.Session.CaptureCommittedRoute(new TowerPlacementTopologyPlan{AuthoritativeRoute=f.Candidate})==null,"foreign binding");});
        Run("spatial change refreshes blocked / idle tick no query",f=>{f.Begin();f.Preview(TowerPlacementRoutePreviewOutcome.RouteBlocked);int queries=f.Paths.SearchCount,writes=f.Line.Writes;f.Session.Tick();Check(f.Line.Writes==writes&&f.Paths.SearchCount==queries,"stationary");f.Map.NodesRoot.position=new Vector3(10,0,0);foreach(var n in f.Formal)n.WorldPosition=n.WorldPosition+new Vector3(10,0,0);f.Session.Tick();Check(f.Red&&f.Line.Positions[0].x==10&&f.Paths.SearchCount==queries,"space refresh");});
        Run("formal failure hides / restoration rebuilds",f=>{f.Map.StructureRevision++;f.Paths.FailQuery=true;f.Session.Tick();f.Session.Tick();Check(!f.Line.enabled&&f.Reports==1,"formal failure");f.Paths.FailQuery=false;f.Session.Tick();Check(f.Line.enabled&&f.Alpha==.5f,"formal recovery");});
        Console.WriteLine("PASS "+count+" production Session/Presenter cases; query/material/transform doubles, no native interaction.");
    }
}
