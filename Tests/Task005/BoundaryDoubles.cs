using System;
using System.Collections.Generic;
namespace UnityEngine {
 public class Object { public static T Instantiate<T>(T original,Vector3 p,Quaternion q) where T:class { Instantiated++; return original; } public static int Instantiated; public static void Destroy(object x){} }
 public class GameObject:Object { public WindVortexBehaviour Vortex; public bool TryGetComponent(out WindVortexBehaviour b){b=Vortex;return b!=null;} }
 public class Transform { public Vector3 position; }
 public struct Quaternion { public static Quaternion identity=>default; }
 public struct Vector3 {public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); public float sqrMagnitude=>x*x+y*y+z*z;}
 public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); }
 public static class Random { public static int Range(int a,int b)=>a; }
 public static class Debug { public static void LogWarning(string s,object o=null){} public static void LogException(Exception e,object o=null){} }
}
public class MonsterManager { public void ForceCleanupAllMonsters(){} public bool isActiveAndEnabled=true, IsBattleActive=true; public BattleCombatBinding CombatBinding; public List<MonsterBehaviour> Alive=new List<MonsterBehaviour>(); public IReadOnlyList<MonsterBehaviour> GetAliveMonsters()=>Alive; }
public class MonsterBehaviour {
 public BattleCombatBinding CombatBinding; public object RuntimeIdentity=new object(); public int CurrentHealth=100; public bool IsGameplayTargetable=true; public UnityEngine.Transform transform=new UnityEngine.Transform(); public UnityEngine.Transform HitAnchor=>transform;
 public Action OnHealthChanged; public int Transactions,Reactions,Applications; public bool Locked; public float Slow=1;
 public void TakeDamage(int n){CurrentHealth-=n;OnHealthChanged?.Invoke();}
 public void BeginTowerOwnedHitTransaction(){Transactions++;} public void EndTowerOwnedHitTransaction(){Transactions--;}
 public void ResolveElementalHitReactions(TowerInstance s,TowerDamageSourceIdentity d,ElementalOpportunityDiagnosticContext c){Reactions++;}
 public BuffApplyOutcome ApplyBuffWithOutcome(BuffApplyRequest r,bool defer){Applications++;return new BuffApplyOutcome{Result=BuffApplyResult.Applied};}
 public bool SetMoveSpeedMultiplier(float v){Slow=v;return true;} public void ClearMoveSpeedMultiplier(){Slow=1;} public void SetMovementLock(bool v){Locked=v;}
}
public class TowerInstance { public TowerUpgradeDefinition Upgrade; public bool TryGetElementalUpgrade(out TowerUpgradeDefinition u){u=Upgrade;return u!=null;} }
public class TowerUpgradeDefinition {public EffectDefinition ElementalApplyEffect;public int ElementalStackContribution=1;}
public class MonsterBuffInstance{}
public class MonsterBuffRuntime {public int Finalized; public void FinalizePendingOverload(PendingBuffOverload p){Finalized++;}}
public class PendingBuffOverload {public MonsterBuffRuntime Runtime; public bool IsPending=true;}
public struct BuffApplyOutcome {public PendingBuffOverload PendingOverload; public BuffApplyResult Result;}
public enum BuffApplyResult {Applied,Refreshed,Stacked,Invalid}
public enum ElementType {None,Fire}
public class BuffDefinition {public ElementType ElementType;}
public class BuffApplyRequest {public BuffApplyRequest(BuffDefinition b,TowerInstance t,TowerUpgradeDefinition u,bool h,UnityEngine.Vector3 p,int n=0){}}
public enum EffectActionType {DealDamage,ApplyBuff,SetMoveSpeedMultiplier,ClearMoveSpeedMultiplier,SetMovementLock,ExecuteMultiTargetEffect,SpawnWindVortex}
public enum EffectDamageMode {None,TowerScaled,FixedBuff}
public class EffectAction {public EffectActionType ActionType;public EffectDamageMode DamageMode=EffectDamageMode.FixedBuff; public int FixedDamage=10; public float DamageScale=1,MoveSpeedMultiplier=.5f;public BuffDefinition BuffDefinition;public bool IsMovementLocked,ExcludeTriggerContextTarget;public EffectDefinition MultiTargetEffectDefinition;public int TargetCount=1;public UnityEngine.GameObject WindVortexPrefab;}
public class EffectDefinition {public float Radius;public UnityEngine.GameObject ExecutionVfxPrefab;public List<EffectAction> Actions=new List<EffectAction>();public bool IsValid()=>true;}
public struct TowerDamageSourceIdentity {public static TowerDamageSourceIdentity BehaviourEffect(EffectDefinition e,int n)=>default;}
public struct TowerOwnedDamageResolution {public int FinalDamage;public TowerInstance SourceTower;public TowerDamageSourceIdentity DamageSourceIdentity;}
public static class TowerRuntimeStatResolver {public static int Published;public static bool TryResolveTowerOwnedDamage(TowerInstance t,TowerDamageSourceIdentity id,float scale,out TowerOwnedDamageResolution d){d=new TowerOwnedDamageResolution{SourceTower=t,FinalDamage=10};return t!=null;} public static void PublishTowerOwnedDamageApplication(TowerOwnedDamageResolution r,int n){Published+=n;}public static void PublishTowerOwnedTargetDamage(TowerOwnedDamageResolution r,MonsterBehaviour t,int n,bool kill){} }
public class WindVortexBehaviour {public bool IsValid()=>true;public BattleCombatBinding Binding;public bool IsInitialized; public void Initialize(BattleCombatBinding b,TowerInstance t,TowerUpgradeDefinition u){Binding=b;IsInitialized=b.IsUsable;}}

partial class LifecycleHarness {
 public BattleCombatBinding combatBinding;
 public MonsterManager monsterManager;
 public bool isReleasing=false,isLifecycleOperationInProgress,releaseRequested;
 public Action BeforeCleanup;
 public SubmissionStub Submission=new SubmissionStub();
 public void CloseBattleAuthorityAndGates() { RevokeCombatAuthority(); }
 private void CaptureBeforeCleanup(){BeforeCleanup?.Invoke();}
 private void RunCleanupSafely(Action a){a();}
 private void ReleasePreparedBattleRuntimeCore(){CaptureBeforeCleanup();}
}
class SubmissionStub {public bool Closed;public void CloseBattleGate(){Closed=true;}public void StopTrackedTowerCombat(){}}
