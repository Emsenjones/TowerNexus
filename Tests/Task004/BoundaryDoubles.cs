// Native/runtime boundaries only. Submission, collection, Pending, candidate,
// validator, Upgrade, Tower instance and immutable observations compile from production.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object
    {
        public string name="fixture";
        public static void Destroy(Object o) { if(o is GameObject g) { g.DestroyCount++; g.OnDestroy?.Invoke(); } }
    }
    public class Sprite {}
    public class SerializeField : Attribute {}
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x,y; public Vector2Int(int a,int b) {x=a;y=b;}
        public bool Equals(Vector2Int b)=>x==b.x&&y==b.y;
        public override bool Equals(object o)=>o is Vector2Int b&&Equals(b);
        public override int GetHashCode()=>x*397^y;
        public override string ToString()=>x+","+y;
    }
    public struct Vector3
    {
        public float x,y,z; public Vector3(float a,float b,float c) {x=a;y=b;z=c;}
        public static Vector3 one=>new Vector3(1,1,1);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public float sqrMagnitude=>x*x+y*y+z*z;
    }
    public struct Quaternion { public float angle; public static float Angle(Quaternion a,Quaternion b)=>Math.Abs(a.angle-b.angle); }
    public class Transform { public Vector3 position,localScale=Vector3.one;public Quaternion rotation; }
    public class GameObject : Object
    {
        public Action OnDisable,OnDestroy;public int DestroyCount;
        public void SetActive(bool value) {if(!value)OnDisable?.Invoke();}
    }
    public class MonoBehaviour : Object
    {
        public readonly Dictionary<Type,object> Components=new Dictionary<Type,object>();
        public Transform transform=new Transform();public GameObject gameObject=new GameObject();
        public bool isActiveAndEnabled=true;
        public int GetInstanceID()=>GetHashCode();
        public bool TryGetComponent<T>(out T value) where T:class
        {value=Components.TryGetValue(typeof(T),out var found)?found as T:null;return value!=null;}
    }
    public static class Debug
    {
        public static void LogException(Exception e,Object context=null){}
        public static void LogWarning(string s,Object context=null){}
        public static void LogError(string s,Object context=null){}
        public static void Log(string s,Object context=null){}
    }
    public static class Time {public static int frameCount;}
    public static class Mathf
    {public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(params int[] v)=>Math.Min(v[0],Math.Min(v[1],v[2]));}
}
public enum TowerFamily {Archer,Magic}
public enum TowerBehaviourPackageType {None,Example}
public enum TowerUpgradeLayer {Basic,Behaviour,Elemental}
public enum DraftResultType {TowerDraft,TowerUpgradeDraft}
public class TowerUpgradeDefinition : UnityEngine.Object
{
    public string DisplayName,Description; public UnityEngine.Sprite Icon;
    public TowerFamily TowerFamily;public TowerUpgradeLayer UpgradeLayer;
    public TowerBehaviourPackageType BehaviourPackageType;public int RequiredTowerLevel=1;
    public bool Valid=true;public bool IsValid()=>Valid;
}
public class TowerDefinition : UnityEngine.Object
{
    public string DisplayName="Tower",Description;public UnityEngine.Sprite Icon;
    public TowerFamily TowerFamily;public bool Valid=true;
    private readonly TowerLevelConfig[] levels={new TowerLevelConfig{Level=1},new TowerLevelConfig{Level=2},new TowerLevelConfig{Level=3}};
    public TowerLevelConfig GetLevelConfig(int n)=>n>=1&&n<=3?levels[n-1]:null;
    public int GetMaxConfiguredLevel()=>3;public bool IsValid()=>Valid;
}
public class TowerLevelConfig {public int Level;public bool IsValid()=>Level>0;}
public class GridNodeBehaviour : UnityEngine.MonoBehaviour
{
    public UnityEngine.Vector2Int GridPosition;public bool IsWalkable=true;public MapGeneratorBehaviour Map;
    public void SetRuntimeOccupied(bool v) {IsWalkable=!v;if(Map!=null)Map.WalkabilityRevision++;}
}
public class MapGeneratorBehaviour : UnityEngine.MonoBehaviour
{
    public ulong StructureRevision,WalkabilityRevision;public string NodeIndexFailureReason="";
    public readonly Dictionary<UnityEngine.Vector2Int,GridNodeBehaviour> Nodes=new Dictionary<UnityEngine.Vector2Int,GridNodeBehaviour>();
    public GridNodeBehaviour Spawn=new GridNodeBehaviour(),Target=new GridNodeBehaviour();
    public bool TryEnsureNodeIndex()=>true;
    public GridNodeBehaviour GetNode(UnityEngine.Vector2Int p)=>Nodes.TryGetValue(p,out var node)?node:null;
    public bool TryGetNodeByWorldPosition(UnityEngine.Vector3 p,out GridNodeBehaviour node)
    {node=GetNode(new UnityEngine.Vector2Int((int)Math.Round(p.x),(int)Math.Round(p.z)));return node!=null;}
    public GridNodeBehaviour GetSpawnNode()=>Spawn;public GridNodeBehaviour GetTargetNode()=>Target;
    public bool RefreshRuntimeTileVisuals()=>true;
}
public class AStarPathfindingService
{
    public MapGeneratorBehaviour ActiveMap;public ulong BindingRevision;public bool Blocked;public int Searches;
    public List<GridNodeBehaviour> FindPath(GridNodeBehaviour from,GridNodeBehaviour to,IReadOnlyList<GridNodeBehaviour> nodes)
    {Searches++;return Blocked?null:new List<GridNodeBehaviour>{from,to};}
}
public class TowerAnchorSet
{
    public UnityEngine.Transform CenterAnchor=new UnityEngine.Transform();
    public IReadOnlyList<UnityEngine.Transform> OccupiedAnchors=new List<UnityEngine.Transform>();
    public bool IsValid()=>CenterAnchor!=null&&OccupiedAnchors!=null&&OccupiedAnchors.Count>0;
}
public class TowerPlacementPreview : UnityEngine.MonoBehaviour
{public TowerDefinition TowerDefinition;public TowerAnchorSet TowerAnchorSet;}
public class TowerBehaviour : UnityEngine.MonoBehaviour
{
    public TowerInstance TowerInstance;public Visual VisualController=new Visual();public bool VisualReady=true;
    public bool TryPrepareLevelVisualRefresh(TowerLevelConfig config,out string reason) {reason="visual readiness";return VisualReady;}
    public bool RefreshTowerVisual()=>VisualReady;
}
public class Visual
{
    public object CurrentTowerModelInstance=new object(); public Action OnPlay;
    public void PlayUpgradeAppliedFeedback(){OnPlay?.Invoke();}
    public void PlayTowerSpawnRefreshFeedback(){OnPlay?.Invoke();}
}
public enum RequiredUpgradeRefreshResult {Applied,NotRequired,TechnicalFailure}
internal struct PreparedTowerCombatUpgradeRevision {}
internal struct PreparedTowerCombatLevelRevision {}
public class TowerCombatBehaviour : UnityEngine.MonoBehaviour
{
    public bool Ready=true,IsPreparedForBattleActivation=true;
    public int Baselines,Refreshes,LevelBaselines,Activations,Stops;public Action OnRefresh,OnPrepare,OnStop;
    public RequiredUpgradeRefreshResult Result=RequiredUpgradeRefreshResult.Applied;
    internal bool TryPrepareUpgradeRevision(TowerInstance t,TowerUpgradeDefinition u,out PreparedTowerCombatUpgradeRevision r,out string reason)
    {OnPrepare?.Invoke();r=default;reason="injected readiness";return Ready;}
    internal void CommitPreparedUpgradeBaseline(PreparedTowerCombatUpgradeRevision r){Baselines++;}
    internal RequiredUpgradeRefreshResult RefreshCommittedUpgrade(TowerUpgradeDefinition u,PreparedTowerCombatUpgradeRevision r,out string reason)
    {Refreshes++;OnRefresh?.Invoke();reason="injected refresh";return Result;}
    internal bool TryPrepareLevelDamageRevision(TowerLevelConfig c,out PreparedTowerCombatLevelRevision r,out string reason)
    {OnPrepare?.Invoke();r=default;reason="level readiness";return Ready;}
    internal void ApplyPreparedLevelDamageRevision(PreparedTowerCombatLevelRevision r){LevelBaselines++;}
    public void ActivatePreparedBattleRuntime(){Activations++;}
    public void StopBattle(){Stops++;OnStop?.Invoke();}
    public string CapturePlacementOwnershipFingerprint()=>"fixture";
}
internal class MonsterRouteRevisionEntry
{
    public UnityEngine.MonoBehaviour Monster=new UnityEngine.MonoBehaviour();public int RevisionId;public string Mode,RelocationReason;
    public UnityEngine.Vector3 CapturedWorldPosition;public GridNodeBehaviour PhysicalCurrentGrid,JoinGrid,RecoveryGrid;
    public bool HasComparableRelocationDistance,RequiresExactTargetApproach;public float RelocationDistance;
}
internal class MonsterRouteRevisionBatch
{
    public string CombatOwnershipFingerprintBefore,CombatOwnershipFingerprintAfter;
    public int LivingMonsterCount,AlreadyOnNewRouteCount,ReachableRouteRejoinCount,ForcedRelocationCount;
    public List<MonsterRouteRevisionEntry> Entries=new List<MonsterRouteRevisionEntry>();
}
public class MonsterManager
{
    public bool Ready=true;public int Applied;public Action OnPrepare,OnRoute;
    internal bool TryPrepareTopologyRevision(TowerPlacementTopologyPlan p,out MonsterRouteRevisionBatch batch,out string reason)
    {OnPrepare?.Invoke();batch=new MonsterRouteRevisionBatch();reason="monster readiness";return Ready;}
    internal void ApplyPreparedMovementRevisionBatch(MonsterRouteRevisionBatch b){Applied++;}
    internal void PublishCommittedPlacementRouteLifecycle(MonsterRouteRevisionBatch b){OnRoute?.Invoke();}
}
public class TowerDeployController
{
    public Action OnPrepare; public bool Ready=true;public TowerBehaviour Last;
    public Action<TowerBehaviour> Mutate;
    internal bool TryPrepareTower(TowerPlacementCandidate c,TowerPlacementTopologyPlan plan,out TowerBehaviour tower,out string reason)
    {
        tower=Last=null;reason="deployer readiness";
        if(!Ready)return false;
        var t=new TowerInstance();t.Initialize(c.Definition,new List<GridNodeBehaviour>(plan.Footprint));
        var b=new TowerBehaviour{TowerInstance=t};b.transform.position=c.Position;b.transform.rotation=c.Rotation;b.transform.localScale=c.LocalScale;
        var anchors=new List<UnityEngine.Transform>();foreach(var n in c.Footprint)
            anchors.Add(new UnityEngine.Transform{position=new UnityEngine.Vector3(n.GridPosition.x,0,n.GridPosition.y)});
        b.Components[typeof(TowerAnchorSet)]=new TowerAnchorSet{OccupiedAnchors=anchors,CenterAnchor=new UnityEngine.Transform{position=c.Position}};
        var combat=new TowerCombatBehaviour();b.Components[typeof(TowerCombatBehaviour)]=combat;
        t.Components[typeof(TowerCombatBehaviour)]=combat;t.Components[typeof(TowerBehaviour)]=b;
        tower=Last=b;Mutate?.Invoke(b);OnPrepare?.Invoke();return true;
    }
}
public partial class DraftSystem : UnityEngine.MonoBehaviour
{
    internal PendingDraftCollection PendingOwner {get;}=new PendingDraftCollection();
    internal bool IsPendingMutationBusy {get;set;}
    public IReadOnlyList<PendingDraftEntry> PendingDrafts=>PendingOwner.Held;
}
public class BattleRuntimeCoordinator : UnityEngine.MonoBehaviour
{
    public bool IsBattleActive=true;public int FailureCount;public Action OnFailure;
    internal readonly TowerPlacementSubmission Submission=new TowerPlacementSubmission();
    internal void FailCommittedUpgrade(TowerUpgradeSystem owner,string reason)
    {owner.FlushCommittedInvestment();FailureCount++;IsBattleActive=false;Submission.CloseBattleGate();OnFailure?.Invoke();}
}
