using System;
using System.Collections.Generic;
class BindingTests {
 static int count;
 static void Check(bool b,string message){if(!b)throw new Exception(message);count++;}
 static BattleCombatBinding Bind(MonsterManager m){var b=new BattleCombatBinding(m,_=>{});m.CombatBinding=b;b.Open();return b;}
 static MonsterBehaviour Target(MonsterManager m){var t=new MonsterBehaviour{CombatBinding=m.CombatBinding};m.Alive.Add(t);return t;}
 static EffectTriggerContext Context(BattleCombatBinding b,MonsterBehaviour t=null,BuffRemovalPermission p=null,bool dead=false)=>new EffectTriggerContext(b,null,null,t,true,default,false,allowsLifecycleOwnerTarget:dead,removalPermission:p);
 static EffectDefinition Effect(float radius,params EffectAction[] a){var e=new EffectDefinition{Radius=radius};e.Actions.AddRange(a);return e;}
 static EffectAction Damage()=>new EffectAction{ActionType=EffectActionType.DealDamage};
 static void Main(){
 var m=new MonsterManager();var b=Bind(m);var t=Target(m);var other=new MonsterManager();var b2=Bind(other);var foreign=Target(other);
 Check(EffectExecutor.Execute(Effect(10,Damage()),Context(b))&&t.CurrentHealth==90&&foreign.CurrentHealth==100,"bound radius only");
 Check(!EffectExecutor.Execute(Effect(0,Damage()),Context(b,foreign)),"foreign direct rejected");
 var child=Effect(0,Damage());var nested=Effect(10,new EffectAction{ActionType=EffectActionType.ExecuteMultiTargetEffect,MultiTargetEffectDefinition=child});
 Check(EffectExecutor.Execute(nested,Context(b))&&t.CurrentHealth==80,"nested preserves binding");
 var vortex=new WindVortexBehaviour();var spawn=Effect(0,new EffectAction{ActionType=EffectActionType.SpawnWindVortex,WindVortexPrefab=new UnityEngine.GameObject{Vortex=vortex}});
 Check(EffectExecutor.Execute(spawn,Context(b,t))&&vortex.Binding==b,"vortex inherits binding");
 b.Close();b.Open();Check(!b.IsUsable,"closed cannot reopen");var fresh=Bind(m);var newTarget=Target(m);
 Check(!EffectExecutor.Execute(Effect(10,Damage()),Context(b))&&newTarget.CurrentHealth==100,"old context cannot hit retry");
 Check(!EffectExecutor.Execute(Effect(0,Damage()),Context(fresh,t)),"new binding cannot target old monster");
 var permission=new BuffRemovalPermission(t,b,t.RuntimeIdentity);t.Slow=.5f;t.Locked=true;
 var cleanup=Effect(0,Damage(),new EffectAction{ActionType=EffectActionType.ClearMoveSpeedMultiplier},new EffectAction{ActionType=EffectActionType.SetMovementLock,IsMovementLocked=false},new EffectAction{ActionType=EffectActionType.SetMovementLock,IsMovementLocked=true},new EffectAction{ActionType=EffectActionType.ApplyBuff,BuffDefinition=new BuffDefinition()});
 Check(EffectExecutor.Execute(cleanup,Context(b,t,permission))&&t.Slow==1&&!t.Locked&&t.CurrentHealth==80&&t.Applications==0,"cleanup only release actions");
 Check(!EffectExecutor.Execute(Effect(10,Damage()),Context(b,t,permission)),"cleanup never radius");
 permission.Close();t.Locked=true;Check(!EffectExecutor.Execute(cleanup,Context(b,t,permission))&&t.Locked,"expired cleanup token rejected");
 var stalePermission=new BuffRemovalPermission(t,b,t.RuntimeIdentity);t.RuntimeIdentity=new object();Check(!EffectExecutor.Execute(cleanup,Context(b,t,stalePermission)),"runtime reuse rejects old cleanup");
 var stop=new MonsterManager();var stopBinding=Bind(stop);var victim=Target(stop);victim.OnHealthChanged=()=>stopBinding.Close();
 var source=new TowerInstance();var resolution=new TowerOwnedDamageResolution{SourceTower=source,FinalDamage=10};
 Check(TowerOwnedHitTransaction.ApplyDamage(stopBinding,victim,resolution,default,default,true)&&victim.CurrentHealth==90&&victim.Reactions==0&&victim.Transactions==0&&TowerRuntimeStatResolver.Published==1,"Stop from direct damage keeps evidence and closes followups");
 var actionsManager=new MonsterManager();var ab=Bind(actionsManager);var av=Target(actionsManager);av.OnHealthChanged=()=>ab.Close();
 Check(EffectExecutor.Execute(Effect(0,Damage(),Damage()),Context(ab,av))&&av.CurrentHealth==90,"Stop blocks sibling action");
 var fault=new MonsterManager();int failures=0;var fb=new BattleCombatBinding(fault,_=>failures++);fault.CombatBinding=fb;fb.Open();fault.isActiveAndEnabled=false;
 Check(!fb.IsUsable&&!fb.IsUsable&&failures==1,"broken active dependency reports once");

 var thrownManager=new MonsterManager();var tb=Bind(thrownManager);var tv=Target(thrownManager);
 tv.OnHealthChanged=()=>{tb.Close();throw new Exception("native callback");};int publishedBefore=TowerRuntimeStatResolver.Published;
 try{TowerOwnedHitTransaction.ApplyDamage(tb,tv,resolution,default,default,true);}catch(Exception){}
 Check(tv.CurrentHealth==90&&tv.Transactions==0&&TowerRuntimeStatResolver.Published==publishedBefore+1,"throwing callback preserves committed evidence and finally");
 var deadManager=new MonsterManager();var db=Bind(deadManager);var dead=Target(deadManager);var neighbour=Target(deadManager);dead.CurrentHealth=0;dead.IsGameplayTargetable=false;
 Check(EffectExecutor.Execute(Effect(10,Damage()),Context(db,dead,dead:true))&&neighbour.CurrentHealth==90,"active Battle dead-owner area effect survives");
 db.Close();Check(!EffectExecutor.Execute(Effect(10,Damage()),Context(db,dead,dead:true))&&neighbour.CurrentHealth==90,"dead-owner flag does not grant closed gameplay");
 var multiManager=new MonsterManager();var mb=Bind(multiManager);var first=Target(multiManager);var second=Target(multiManager);first.OnHealthChanged=()=>mb.Close();
 EffectExecutor.Execute(Effect(10,Damage()),Context(mb));Check(first.CurrentHealth==90&&second.CurrentHealth==100,"closure between targets blocks remaining damage");
 int created=UnityEngine.Object.Instantiated;Check(!EffectExecutor.Execute(spawn,Context(b,t))&&UnityEngine.Object.Instantiated==created,"closed context does not instantiate vortex");
 var noBindingTargets=new List<MonsterBehaviour>{t};Check(!EffectExecutor.ExecuteWithResolvedTargets(Effect(10,Damage()),Context(null),noBindingTargets)&&noBindingTargets.Count==0,"invalid context clears resolved output");
 var liveManager=new MonsterManager();var lb=Bind(liveManager);var lt=Target(liveManager);
 Check(EffectExecutor.Execute(Effect(0,Damage()),Context(lb,lt))&&lt.CurrentHealth==90,"source-less FixedBuff preserves damage");
 var scaled=Damage();scaled.DamageMode=EffectDamageMode.TowerScaled;Check(!EffectExecutor.Execute(Effect(0,scaled),Context(lb,lt))&&lt.CurrentHealth==90,"TowerScaled still requires source");
 b.Close();Check(fresh.IsUsable,"late old closure cannot close new binding");

 var cm=new MonsterManager();var cb=Bind(cm);var ct=Target(cm);var h=new LifecycleHarness{combatBinding=cb,monsterManager=cm};
 h.BeforeCleanup=()=>{EffectExecutor.Execute(Effect(0,Damage()),Context(cb,ct));Check(h.Submission.Closed,"Submission closed before evidence callback");};
 h.StopBattle();Check(ct.CurrentHealth==100&&!cb.IsUsable,"production Stop revokes before capture");
 var dm=new MonsterManager();var deferred=Bind(dm);var dh=new LifecycleHarness{combatBinding=deferred,isLifecycleOperationInProgress=true};
 dh.ReleasePreparedBattleRuntime();Check(dh.releaseRequested&&!deferred.IsUsable&&dh.Submission.Closed,"deferred Release revokes immediately");
 var rm=new MonsterManager();var rb=Bind(rm);var rh=new LifecycleHarness{combatBinding=rb};bool revokedAtRelease=false;
 rh.BeforeCleanup=()=>revokedAtRelease=!rb.IsUsable;rh.ReleasePreparedBattleRuntime();Check(revokedAtRelease,"Release revokes before physical cleanup");
 Console.WriteLine(count+" Task005 production Effect/binding/hit checks passed.");
 }
}
