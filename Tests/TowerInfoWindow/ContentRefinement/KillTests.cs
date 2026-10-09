using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class Object {public static int Destructions;public static void Destroy(object x,float delay=0){Destructions++;}}
 public class MonoBehaviour:Object {public Transform transform=new Transform();  public bool isActiveAndEnabled=true;public object gameObject=new object();public int GetInstanceID()=>1; }
 public class Transform {public Vector3 position;}
 public struct Vector3 {}
 public static class Time {public static float time;}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);}
 public static class Debug {public static int Errors; public static void LogException(Exception e,object c=null){Errors++;}}
}
public class TowerDefinition {}
public class GridNodeBehaviour {}
public class TowerUpgradeDefinition {public int GetInstanceID()=>1;}
public class TowerUpgradeState {public void Reset(){}}
public enum BuffRuntimePhase {Stacking,Protection}
public enum BuffApplyResult {Applied,Invalid,BlockedByProtectionPhase,BlockedBySourceApplyCooldown,Refreshed,Stacked}
public enum BuffEventType {Removed,Overload,PeriodicTick,TowerHitReceived}
public class EffectDefinition {}
public class BuffDefinition {public float ActiveDuration=10,PeriodicTickInterval=1,SourceApplyCooldown,OverloadProtectionDuration,TowerHitReactionCooldown;public bool UsesStacks;public int MaximumStacks=4;public bool IsValid()=>true;public int GetInstanceID()=>1;public EffectDefinition GetEffectDefinition(BuffEventType e)=>new EffectDefinition();}
public static class EffectTargetResolver {public static Vector3 GetMonsterHitPosition(MonsterBehaviour m)=>default;}
public static class EffectExecutor {public static EffectTriggerContext Last;public static bool Execute(EffectDefinition e,EffectTriggerContext c){Last=c;c.TargetMonster.Damage(10,c.KillSource);return true;}}
public class ElementalApplicationTransaction {public void FinalizePendingOverloads(){}}
public enum TowerDamageSourceIdentity {Direct}
public struct TowerOwnedDamageResolution {public TowerInstance SourceTower;public int FinalDamage;public TowerDamageSourceIdentity DamageSourceIdentity;}
public static class TowerRuntimeStatResolver {public static int Applications;public static void PublishTowerOwnedDamageApplication(TowerOwnedDamageResolution r,int n){Applications+=n;}public static void PublishTowerOwnedTargetDamage(TowerOwnedDamageResolution r,MonsterBehaviour m,int n,bool killed){}}
public static class ElementalApplication {public static ElementalApplicationTransaction TryApplyFromTowerAttack(BattleCombatBinding b,TowerInstance t,MonsterBehaviour m,Vector3 p,ElementalOpportunityDiagnosticContext d,TowerKillSource? s=null)=>null;}
public class BuffRemovalPermission {public BuffRemovalPermission(MonsterBehaviour m,BattleCombatBinding b,object id){}public void Close(){}}
public partial class TowerInstance:MonoBehaviour {
 private const int DefaultLevel=1; private TowerDefinition towerDefinition;private int currentLevel;
 private TowerUpgradeState upgradeState=new TowerUpgradeState();private List<GridNodeBehaviour> occupiedNodes=new List<GridNodeBehaviour>();
}
internal class TowerPlacementSubmission { internal HashSet<TowerInstance> Members=new HashSet<TowerInstance>();internal bool OwnsDeployedTower(TowerInstance tower)=>Members.Contains(tower); }
public class MonsterManager:MonoBehaviour {
 public BattleCombatBinding CombatBinding;public bool IsBattleActive=true;
 public IReadOnlyList<MonsterBehaviour> GetAliveMonsters()=>new MonsterBehaviour[0];
}
public enum MonsterPlacementRouteResolutionReason {Killed,Leaked}
public enum BuffRemovalReason {None,RuntimeReset,MonsterKilled,MonsterLeaked}
public class BuffRuntimeObservation {public static BuffRuntimeObservation CreateLifecycleEvent(MonsterBuffInstance i,BuffEventType e,BuffRemovalReason r,bool h)=>new BuffRuntimeObservation();}
public class ElementalHitReactionObservation {}
public partial class MonsterBuffRuntime {
 private void QueueObservation(BuffRuntimeObservation o){}
 internal bool Execute(MonsterBuffInstance instance,BuffEventType e,TowerKillSource? source=null)=>ExecuteLifecycleEffect(instance,e,hasSourceContextOverride:source.HasValue,sourceTowerOverride:source.HasValue?source.Value.Tower:null,killSourceOverride:source);
 public int Depth;public Action ClearCallback;
 public event Action OnStateChanged;public event Action<BuffRuntimeObservation> OnRuntimeObserved;
 public event Action<ElementalHitReactionObservation> OnElementalHitReactionObserved;
 public void BeginExternalMutation(){Depth++;}public void EndExternalMutation(){Depth--;}
 public void Clear(BuffRemovalReason reason){ClearCallback?.Invoke();}
}
public class Feedback {public void PlayHitFeedback(){} public void StopFeedback(){}public bool TryInitialize(out string r){r="";return true;}}
public partial class MonsterBehaviour:MonoBehaviour {
 internal object RuntimeIdentity=new object();public BattleCombatBinding CombatBinding;
 private int currentHealth=10,maxHealth=10,towerOwnedHitTransactionDepth;
 private bool isResolved,isDead,isCleaningUp,requiresExactTargetApproach,hasActivePlacementConnector;
 private int laneIdentity,activePlacementRevisionId;private object activePlacementJoinGrid;
 private TowerKillSource lethalSource;private object lethalTargetIdentity;
 private List<int> currentPath=new List<int>();private Feedback hitFeedback=new Feedback();
 internal MonsterBuffRuntime buffRuntime=new MonsterBuffRuntime();
 public event Action<MonsterBehaviour,bool> OnResolved;public event Action<MonsterBehaviour> OnDied,OnTargetReached;
 internal Action Health,Placement,Reaction;
 internal void ResolveElementalHitReactions(TowerInstance t,TowerDamageSourceIdentity d,ElementalOpportunityDiagnosticContext c,TowerKillSource? source=null){Reaction?.Invoke();}internal int Progress;
 public int CurrentHealth=>currentHealth;public Transform HitAnchor=>transform;
 public bool IsGameplayTargetable=>!isResolved&&!isCleaningUp&&currentHealth>0;
 internal int HealthValue=>currentHealth;
 private void NotifyHealthChanged(){Health?.Invoke();}
 private void ShowDamageNumber(int n){}private void PlayDeathAnimation(){}private float GetDeathDelay()=>0;
 private void PublishPlacementResolutionBeforeJoin(MonsterPlacementRouteResolutionReason r){Placement?.Invoke();}
 private bool TryValidateAuthoredConfiguration(out string r){r="";return true;}
 private void ClearMovementControls(){}private void ClearPlacementRouteState(){}private void StopMovement(){}
 private void EnsureBuffRuntime(){if(buffRuntime==null)buffRuntime=new MonsterBuffRuntime();}
 private void CacheBuffVisualController(){}private void CacheAnimator(){}private void RefreshEffectiveMoveSpeed(){}private void CacheHitFeedback(){}
 private void HandleBuffStateChanged(){}private void HandleBuffRuntimeObserved(BuffRuntimeObservation r){}private void HandleElementalHitReactionObserved(ElementalHitReactionObservation r){}
 internal void Damage(int n,TowerKillSource s=default){TakeDamage(n,s);}
 internal void Arrive(){TryResolve(true);}
 internal void Cleanup(){isCleaningUp=isResolved=true;lethalSource=default;lethalTargetIdentity=null;}
 internal void Reset(BattleCombatBinding b){TryInitializeRuntime(out _);CombatBinding=b;}
}
class KillTests {
 static int checks;
 static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
 class Fixture {
  internal TowerInstance A=new TowerInstance(),B=new TowerInstance();internal MonsterBehaviour M=new MonsterBehaviour();
  internal BattleCombatBinding Battle;internal TowerPlacementSubmission Members=new TowerPlacementSubmission();
  internal Fixture(){var manager=new MonsterManager();Battle=new BattleCombatBinding(manager,_=>throw new Exception("impure read"));manager.CombatBinding=Battle;Battle.Open();foreach(var t in new[]{A,B}){t.Initialize(new TowerDefinition(),null);Members.Members.Add(t);t.BindKillOwnership(Battle,Members);}M.CombatBinding=Battle;M.OnResolved+=(m,r)=>m.Progress++;}
  internal TowerKillSource Source(TowerInstance t)=>new TowerKillSource(Battle,t);
 }
 static void Main(){
  var f=new Fixture();f.M.Damage(4,f.Source(f.A));Check(f.A.KillCount==0,"nonlethal");
  f.M.OnResolved+=(m,r)=>Check(f.B.KillCount==1,"credit before progress/terminal observer");
  f.M.Damage(6,f.Source(f.B));Check(f.B.KillCount==1&&f.A.KillCount==0&&f.M.Progress==1,"last blow B");f.M.Damage(10,f.Source(f.A));f.M.Die();Check(f.B.KillCount==1&&f.M.Progress==1,"exactly once");
  f=new Fixture();f.M.Damage(10);Check(f.A.KillCount==0&&f.M.Progress==1,"unattributed death still progresses");
  f=new Fixture();var scope=f.M.BeginTowerOwnedHitTransaction();f.M.Damage(10,f.Source(f.A));f.M.Arrive();f.M.EndTowerOwnedHitTransaction(scope);Check(f.A.KillCount==0&&f.M.Progress==1,"arrival discards lethal credit");
  f=new Fixture();scope=f.M.BeginTowerOwnedHitTransaction();f.M.Damage(10,f.Source(f.A));f.M.Cleanup();f.M.EndTowerOwnedHitTransaction(scope);Check(f.A.KillCount==0&&f.M.buffRuntime.Depth==0,"cleanup discards record and balances mutation");
  f=new Fixture();scope=f.M.BeginTowerOwnedHitTransaction();var inner=f.M.BeginTowerOwnedHitTransaction();f.M.Damage(10,f.Source(f.B));f.M.EndTowerOwnedHitTransaction(inner);f.M.Damage(10,f.Source(f.A));Check(f.B.KillCount==0,"nested death deferred");f.M.EndTowerOwnedHitTransaction(scope);f.M.EndTowerOwnedHitTransaction(scope);Check(f.B.KillCount==1&&f.A.KillCount==0&&f.M.buffRuntime.Depth==0,"inner source wins, both depths and duplicate End");
  f=new Fixture();var old=f.Source(f.A);f.A.Initialize(new TowerDefinition(),null);f.A.BindKillOwnership(f.Battle,f.Members);f.M.Damage(10,old);Check(f.A.KillCount==0,"same Tower object reinitialized and redeployed cannot claim old source");
  f=new Fixture();f.Members.Members.Remove(f.A);f.M.Damage(10,f.Source(f.A));Check(f.A.KillCount==0,"removed source");
  f=new Fixture();var foreign=new Fixture();f.M.Damage(10,foreign.Source(foreign.A));Check(foreign.A.KillCount==0,"foreign Battle");
  f=new Fixture();old=f.Source(f.A);f.Battle.Close();f.M.Damage(10,old);Check(f.A.KillCount==0,"outgoing Battle pure check");
  f=new Fixture();var current=f;f.M.Health=()=>current.M.Cleanup();f.M.Damage(10,f.Source(f.A));Check(f.A.KillCount==0,"health callback cleanup");
  f=new Fixture();current=f;var buffs=f.M.buffRuntime;scope=f.M.BeginTowerOwnedHitTransaction();f.M.Health=()=>{current.M.Health=null;current.M.Reset(current.Battle);};f.M.Damage(10,f.Source(f.A));f.M.EndTowerOwnedHitTransaction(scope);Check(f.A.KillCount==0&&f.M.HealthValue==10&&f.M.IsGameplayTargetable&&buffs.Depth==0&&f.M.buffRuntime.Depth==0,"callback reset cannot resolve new Monster or unbalance new Buff runtime");
  f=new Fixture();f.M.Health=()=>throw new Exception("health");try{f.M.Damage(10,f.Source(f.A));}catch(Exception){}Check(f.A.KillCount==1&&f.M.Progress==1,"committed health exception still resolves");
  f=new Fixture();f.M.Placement=()=>throw new Exception("path");f.M.buffRuntime.ClearCallback=()=>throw new Exception("clear");f.M.OnResolved+=(m,r)=>throw new Exception("resolved");f.M.OnDied+=m=>throw new Exception("death");scope=f.M.BeginTowerOwnedHitTransaction();f.M.Damage(10,f.Source(f.A));f.M.EndTowerOwnedHitTransaction(scope);Check(f.A.KillCount==1&&f.M.Progress==1&&f.M.buffRuntime.Depth==0,"death exceptions preserve progress and transaction balance");
  f=new Fixture();current=f;buffs=f.M.buffRuntime;scope=f.M.BeginTowerOwnedHitTransaction();f.M.OnResolved+=(m,r)=>m.Reset(current.Battle);f.M.Damage(10,f.Source(f.A));f.M.EndTowerOwnedHitTransaction(scope);Check(f.A.KillCount==1&&f.M.HealthValue==10&&f.M.IsGameplayTargetable&&buffs.Depth==0&&f.M.buffRuntime.Depth==0,"old death callback does not destroy new Monster lifecycle");
  f=new Fixture();var context=new EffectTriggerContext(f.Battle,f.A,null,f.M,false,default,false);var request=new BuffApplyRequest(null,f.A,null,false,default,killSource:context.KillSource);old=request.KillSource;f.A.Initialize(new TowerDefinition(),null);f.A.BindKillOwnership(f.Battle,f.Members);var delayed=new EffectTriggerContext(f.Battle,f.A,null,f.M,false,default,false,killSource:old);f.M.Damage(10,delayed.KillSource);Check(f.A.KillCount==0,"Buff and delayed Effect preserve original provenance");
  f=new Fixture();typeof(TowerInstance).GetField("killCount",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(f.A,int.MaxValue);f.M.Damage(10,f.Source(f.A));Check(f.A.KillCount==int.MaxValue,"saturating counter");f.A.Initialize(new TowerDefinition(),null);Check(f.A.KillCount==0,"new initialization resets");
  f=new Fixture();var savedA=new BuffApplyRequest(new BuffDefinition(),f.A,null,false,default);
  var buff=new MonsterBuffInstance(savedA,f.M,1);var runtime=f.M.buffRuntime;
  runtime.Execute(buff,BuffEventType.TowerHitReceived,f.Source(f.B));
  Check(f.B.KillCount==1&&f.A.KillCount==0&&buff.SourceTower==f.A,"explicit reaction execution credits B without overwriting shared source A");
  f.M.Reset(f.Battle);buff=new MonsterBuffInstance(savedA,f.M,2);runtime=f.M.buffRuntime;
  runtime.Execute(buff,BuffEventType.PeriodicTick);Check(f.A.KillCount==1&&f.B.KillCount==1,"periodic lifecycle still credits saved A");
  f.M.Reset(f.Battle);buff=new MonsterBuffInstance(savedA,f.M,3);buff.TryReapply(new BuffApplyRequest(savedA.BuffDefinition,f.B,null,false,default),out _,out _,out _);
  f.M.buffRuntime.Execute(buff,BuffEventType.PeriodicTick);Check(f.B.KillCount==2,"successful reapplication captures B lifecycle source");
  f.M.Reset(f.Battle);buff=new MonsterBuffInstance(savedA,f.M,4);var pending=new PendingBuffOverload(f.M.buffRuntime,buff,f.A,null,default,true,savedA.KillSource);
  buff.TryReapply(new BuffApplyRequest(savedA.BuffDefinition,f.B,null,false,default),out _,out _,out _);
  f.M.buffRuntime.Execute(buff,BuffEventType.Overload,pending.KillSource);Check(f.A.KillCount==2&&f.B.KillCount==2,"delayed Overload retains captured A despite reapplication B");
  f.M.Reset(f.Battle);buff=new MonsterBuffInstance(savedA,f.M,5);f.A.Initialize(new TowerDefinition(),null);f.A.BindKillOwnership(f.Battle,f.Members);
  f.M.buffRuntime.Execute(buff,BuffEventType.PeriodicTick);Check(f.A.KillCount==0,"actual saved Buff provenance rejects reused Tower object");
  f=new Fixture();buff=new MonsterBuffInstance(new BuffApplyRequest(new BuffDefinition(),null,null,false,default),f.M,1);f.M.buffRuntime.Execute(buff,BuffEventType.PeriodicTick);
  Check(f.M.Progress==1&&f.A.KillCount==0,"source-less FixedBuff remains lethal with no Tower credit");
  f=new Fixture();var resolved=new TowerOwnedDamageResolution{SourceTower=f.A,FinalDamage=10};old=f.Source(f.A);f.A.Initialize(new TowerDefinition(),null);f.A.BindKillOwnership(f.Battle,f.Members);
  TowerOwnedHitTransaction.ApplyDamage(f.Battle,f.M,resolved,default,default,false,killSource:old);Check(f.A.KillCount==0&&f.M.Progress==1,"production released-hit path cannot recapture reused source identity");
  f=new Fixture();current=f;resolved=new TowerOwnedDamageResolution{SourceTower=f.A,FinalDamage=1};f.M.Reaction=()=>current.M.Damage(9,current.Source(current.B));
  TowerOwnedHitTransaction.ApplyDamage(f.Battle,f.M,resolved,default,default,false,killSource:f.Source(f.A));Check(f.A.KillCount==0&&f.B.KillCount==1&&f.M.buffRuntime.Depth==0,"production outer hit credits nested lethal reaction once");
  f=new Fixture();resolved=new TowerOwnedDamageResolution{SourceTower=f.A,FinalDamage=10};int evidence=TowerRuntimeStatResolver.Applications;f.M.Health=()=>throw new Exception("health");
  try{TowerOwnedHitTransaction.ApplyDamage(f.Battle,f.M,resolved,default,default,false,killSource:f.Source(f.A));}catch(Exception){}
  Check(f.A.KillCount==1&&f.M.buffRuntime.Depth==0&&TowerRuntimeStatResolver.Applications==evidence+1,"actual hit exception balances scopes and preserves committed damage evidence");
  f=new Fixture();current=f;buffs=f.M.buffRuntime;resolved=new TowerOwnedDamageResolution{SourceTower=f.A,FinalDamage=10};evidence=TowerRuntimeStatResolver.Applications;
  f.M.Health=()=>{current.M.Health=null;current.M.Reset(current.Battle);throw new Exception("reset then throw");};
  try{TowerOwnedHitTransaction.ApplyDamage(f.Battle,f.M,resolved,default,default,false,killSource:f.Source(f.A));}catch(Exception){}
  Check(f.A.KillCount==0&&f.M.HealthValue==10&&buffs.Depth==0&&f.M.buffRuntime.Depth==0&&TowerRuntimeStatResolver.Applications==evidence+1,"reset-and-throw retains old committed damage evidence without new-life death");
  f=new Fixture();f.M.Damage(10,f.Source(f.A));f.A.BindKillOwnership(f.Battle,f.Members);Check(f.A.KillCount==1,"same Battle ownership rebind preserves count");f.Battle.Close();Check(f.A.KillCount==1,"Battle stop retains cumulative count");
  Console.WriteLine("PASS exact production health/death/transaction/counter/provenance: "+checks+" assertions (native and Buff boundaries doubled)");
 }
}
