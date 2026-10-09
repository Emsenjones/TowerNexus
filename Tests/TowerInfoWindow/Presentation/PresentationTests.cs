using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

class PresentationTests
{
    static int checks;
    static void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
    static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(target);
    static void Frame(object target)=>target.GetType().GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null);
    static GameObject Child(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go;}
    static TowerUpgradeInfoItem CreateItem()
    {
        var item=new GameObject("prefab").AddComponent<TowerUpgradeInfoItem>();
        Set(item,"iconImage",Child(item.transform,"icon").AddComponent<Image>());
        Set(item,"backgroundImage",Child(item.transform,"background").AddComponent<Image>());
        Set(item,"nameText",Child(item.transform,"name").AddComponent<TMP_Text>());
        foreach(var field in new[]{"basicUpgradeIconBackground","behaviourUpgradeIconBackground","elementalUpgradeIconBackground"}) Set(item,field,new Sprite());
        return item;
    }
    class Fixture
    {
        internal BattleHUDUI Hud;
        internal TowerInfoWindow View;
        internal BattleCombatBinding Battle;
        internal BattleModalPauseAuthority Pause=new BattleModalPauseAuthority();
        internal TowerPlacementSubmission Members=new TowerPlacementSubmission();
        internal TowerInstance Target;
        internal TowerCombatBehaviour Combat;
        internal Button Close;
        internal RectTransform Grid;
        internal int Failures;
        internal TMP_Text Title;
        internal Fixture(int upgrades=2)
        {
            UnityEngine.Object.BeforeInstantiate=null;GameObject.Activated=null;GameObject.BeforeDeactivate=null;Time.timeScale=0.65f;
            var manager=new GameObject().AddComponent<MonsterManager>();
            Battle=new BattleCombatBinding(manager,r=>Failures++);manager.CombatBinding=Battle;Battle.Open();
            Pause.BindBattle(Battle);Pause.OpenBattle(Battle);
            var tower=new GameObject();Target=tower.AddComponent<TowerInstance>();Combat=tower.AddComponent<TowerCombatBehaviour>();Combat.Bind(Target,Battle);Members.Members.Add(Target);
            for(int i=0;i<upgrades;i++)Target.AppliedUpgrades.Add(new TowerUpgradeDefinition());
            Hud=new GameObject().AddComponent<BattleHUDUI>();
            var root=new GameObject("inspection");View=root.AddComponent<TowerInfoWindow>();
            var mask=Child(root.transform,"mask").AddComponent<Image>();
            var content=Child(root.transform,"content").transform;
            var basic=Child(content,"basic").transform;
            Grid=Child(content,"grid").transform;Grid.gameObject.AddComponent<GridLayoutGroup>();
            Title=Child(content,"title").AddComponent<TMP_Text>();Title.text="Tower Info";
            Set(View,"contentRoot",content);
            foreach(string field in new[]{"displayNameText","killCountText","levelText","attackRangeText","attackText","attackCycleDurationText"})
                Set(View,field,Child(basic,field).AddComponent<TMP_Text>());
            Set(View,"towerIcon",Child(basic,"icon").AddComponent<Image>());
            Set(View,"upgradeContainer",Grid);Set(View,"upgradeItemPrefab",CreateItem());
            Close=Child(content,"close").AddComponent<Button>();Set(View,"closeButton",Close);
            root.SetActive(false);Set(Hud,"towerInfoWindow",View);
            Check(Hud.BindTowerInspection(Battle,Members,Pause,out var reason),"bind: "+reason);
        }
        internal bool Open()=>Hud.TryOpenTowerInfo(Target,out _);
        internal void End(){Hud.ClearTowerInspectionBinding();Pause.CancelBattle(Battle);}
    }
    static void Main()
    {
        var item=CreateItem();
        foreach(var layer in new[]{TowerUpgradeLayer.Basic,TowerUpgradeLayer.Behaviour,TowerUpgradeLayer.Elemental})
        {
            var sprite=new Sprite();item.Initialize("Name "+layer,sprite,layer);
            string field=layer==TowerUpgradeLayer.Basic?"basicUpgradeIconBackground":layer==TowerUpgradeLayer.Behaviour?"behaviourUpgradeIconBackground":"elementalUpgradeIconBackground";
            Check(Get<Image>(item,"backgroundImage").sprite==Get<Sprite>(item,field)&&Get<TMP_Text>(item,"nameText").text=="Name "+layer&&Get<Image>(item,"iconImage").sprite==sprite,"layer background, captured name and icon");
        }
        Check(!item.TryInitialize("",null,(TowerUpgradeLayer)99,out _),"unsupported layer rejected");
        Set(item,"basicUpgradeIconBackground",null);Check(!item.TryValidateReferences(out _),"missing layer background rejected");
        var fresh=new Fixture();Check(fresh.Open()&&Get<TMP_Text>(fresh.View,"killCountText").text=="0","zero count is displayed");fresh.Hud.CloseTowerInfo();fresh.Target.KillCount=7;
        Check(fresh.Open()&&Get<TMP_Text>(fresh.View,"killCountText").text=="7","latest count on reopen");fresh.End();
        fresh=new Fixture();var captured=fresh;UnityEngine.Object.BeforeInstantiate=()=>captured.Target.KillCount++;
        Check(!fresh.Open()&&!fresh.Pause.HasRetainedPause&&fresh.Grid.children.Count==0,"count changed during preparation rolls back");fresh.End();
        fresh=new Fixture();captured=fresh;GameObject.Activated=go=>{if(go==captured.View.gameObject)captured.Target.RuntimeIdentity=new object();};
        Check(!fresh.Open()&&!fresh.Pause.HasRetainedPause,"same Tower object reinitialized during activation rejected");fresh.End();
        fresh=new Fixture();captured=fresh;UnityEngine.Object.BeforeInstantiate=()=>captured.Target.AppliedUpgrades[0].DisplayName="changed";
        Check(!fresh.Open()&&!fresh.Pause.HasRetainedPause,"upgrade name freshness");fresh.End();
        fresh=new Fixture();fresh.Target.AppliedUpgrades[0].UpgradeLayer=(TowerUpgradeLayer)99;
        Check(!fresh.Open()&&fresh.Grid.children.Count==0&&!fresh.Pause.HasRetainedPause,"invalid item generation is completely retired");fresh.End();
        fresh=new Fixture();captured=fresh;GameObject.Activated=go=>{if(go==captured.View.gameObject)captured.Target.KillCount++;};
        Check(!fresh.Open()&&!fresh.Pause.HasRetainedPause&&fresh.Grid.children.Count==0,"activation-time count freshness");fresh.End();
        fresh=new Fixture();captured=fresh;UnityEngine.Object.BeforeInstantiate=()=>captured.Target.AppliedUpgrades[0].UpgradeLayer=TowerUpgradeLayer.Elemental;
        Check(!fresh.Open()&&!fresh.Pause.HasRetainedPause,"upgrade layer freshness");fresh.End();
        Check(typeof(TowerInfoWindow).GetField("descriptionText",BindingFlags.NonPublic|BindingFlags.Instance)==null,"description binding removed");
        var f=new Fixture();var authored=Child(f.Grid,"authored");
        Check(f.Open()&&f.Hud.IsTowerInfoOpen&&f.Hud.IsTowerInspectionBusy&&Time.timeScale==0,"open hidden root and own pause");
        Check(f.View.gameObject.activeSelf&&f.Grid.children.Count==3,"generate one Image per acquired upgrade");
        Check(Get<TMP_Text>(f.View,"attackText").text=="15.5"&&Get<TMP_Text>(f.View,"attackRangeText").text=="3.25"&&Get<TMP_Text>(f.View,"attackCycleDurationText").text=="1 s"&&f.Title.text=="Tower Info","resolved stats and fixed Title");
        Check(Get<Image>(f.Grid.children[1].gameObject.GetComponent<TowerUpgradeInfoItem>(),"iconImage").sprite==f.Target.AppliedUpgrades[0].Icon&&
            !Get<Image>(f.Grid.children[1].gameObject.GetComponent<TowerUpgradeInfoItem>(),"iconImage").raycastTarget,"acquisition order and display-only icons");
        Check(!f.Open(),"second open cannot replace current target");
        f.Close.onClick.Invoke();
        Check(!f.Hud.IsTowerInspectionBusy&&!f.View.gameObject.activeSelf&&Time.timeScale==0.65f&&f.Grid.children.Count==1&&authored!=null,"close preserves authored child, clears generated items and restores rate");
        f.Combat.UpgradeBaseline();f.Target.AppliedUpgrades.Add(new TowerUpgradeDefinition());
        Check(f.Open()&&Get<TMP_Text>(f.View,"attackText").text=="20.5"&&f.Grid.children.Count==4&&Get<TMP_Text>(f.View,"attackCycleDurationText").text=="0.75 s","reopen reads fresh stats and upgrade list without old layout items");
        f.Hud.CloseTowerInfo();f.Hud.CloseTowerInfo();Check(Time.timeScale==0.65f,"duplicate close does not change rate");f.End();

        f=new Fixture(0);Check(f.Open()&&f.Grid.children.Count==0,"empty acquired upgrades");f.Hud.CloseTowerInfo();
        f.Combat.LevelBaseline();Check(f.Open()&&Get<TMP_Text>(f.View,"levelText").text=="2"&&Get<TMP_Text>(f.View,"attackText").text=="25.5","new committed level baseline");f.End();

        f=new Fixture();f.Target.TowerDefinition.Icon=null;f.Target.AppliedUpgrades[0].Icon=null;
        Check(f.Open()&&!Get<Image>(f.View,"towerIcon").enabled&&f.Grid.children.Count==2&&!Get<Image>(f.Grid.children[0].gameObject.GetComponent<TowerUpgradeInfoItem>(),"iconImage").enabled,"missing icons retain empty slots and never reuse old sprite");f.End();

        f=new Fixture();var foreign=new GameObject().AddComponent<TowerInstance>();
        Check(!f.Hud.TryOpenTowerInfo(foreign,out _)&&!f.Pause.HasRetainedPause,"uncommitted target rejected");
        f.Combat.InvalidateCache();Check(!f.Open()&&Time.timeScale==0.65f,"missing committed baseline rejected");f.End();

        f=new Fixture();Set(f.View,"attackText",null);Check(!f.Open()&&f.Grid.children.Count==0&&!f.Pause.HasRetainedPause,"missing required reference leaves no partial view or pause");f.End();

        f=new Fixture();int created=0;UnityEngine.Object.BeforeInstantiate=()=>{if(++created==2)throw new Exception("creation failure");};
        Check(!f.Open()&&f.Grid.children.Count==0&&!f.Hud.IsTowerInspectionBusy&&!f.Pause.HasRetainedPause,"halfway generation failure retires all icons");f.End();

        f=new Fixture();var changed=f;UnityEngine.Object.BeforeInstantiate=()=>{changed.Combat.UpgradeBaseline();changed.Target.AppliedUpgrades.Add(new TowerUpgradeDefinition());};
        Check(!f.Open()&&f.Grid.children.Count==0&&!f.Pause.HasRetainedPause,"same-target upgrade during preparation invalidates snapshot without retry");f.End();

        f=new Fixture();changed=f;GameObject.Activated=go=>{if(go==changed.View.gameObject)changed.Combat.LevelBaseline();};
        Check(!f.Open()&&!f.View.gameObject.activeSelf&&!f.Pause.HasRetainedPause&&Time.timeScale==0.65f,"activation-time level mutation rolls back acquired pause");f.End();

        f=new Fixture();changed=f;UnityEngine.Object.BeforeInstantiate=()=>changed.Hud.CancelTowerInspection();
        Check(!f.Open()&&f.Grid.children.Count==0&&!f.Hud.IsTowerInspectionBusy,"cancellation inside Instantiate also retires returned unregistered item");f.End();

        f=new Fixture();changed=f;GameObject.Activated=go=>{if(go==changed.View.gameObject){changed.Hud.CancelTowerInspection();Check(!changed.Hud.BindTowerInspection(changed.Battle,changed.Members,changed.Pause,out _),"reentrant bind blocked through outer opening cleanup");}};
        Check(!f.Open()&&Time.timeScale==0.65f&&!f.Pause.HasRetainedPause,"activation cancellation restores exact owner");f.End();

        f=new Fixture();f.Pause.TryAcquire(f.Battle,BattleModalKind.Draft,new object(),out var draft,out _);
        Check(!f.Open()&&f.Pause.Owns(draft)&&f.Grid.children.Count==0,"Draft overlap rejected without changing owner");f.Pause.Release(draft);f.End();

        f=new Fixture();Check(f.Open(),"external disable setup");f.View.gameObject.SetActive(false);
        Check(!f.Hud.IsTowerInspectionBusy&&!f.Pause.HasRetainedPause&&f.Grid.children.Count==0,"common root disable clears mask, content and pause");f.End();

        f=new Fixture();Check(f.Open(),"target removal setup");UnityEngine.Object.Destroy(f.Target.gameObject);Frame(f.Hud);
        Check(!f.Hud.IsTowerInfoOpen&&!f.Pause.HasRetainedPause,"destroyed Unity target cancels on presentation frame while paused");f.End();

        f=new Fixture();Check(f.Open(),"revocation setup");f.Pause.RevokeBattle(f.Battle);
        Check(f.Pause.HasRetainedPause&&!f.Pause.CanAcquire,"revoked modal is not idle");Frame(f.Hud);
        Check(!f.Pause.HasRetainedPause&&Time.timeScale==0.65f,"revoked handle remains releasable by cleanup");f.End();

        f=new Fixture();Check(f.Open(),"HUD disable setup");Get<DraftWindow>(f.Hud,"draftWindow").ThrowNotify=true;
        Get<TowerPlacementController>(f.Hud,"towerPlacementController").ThrowCancel=true;f.Hud.ThrowUnsubscribe=true;
        f.Hud.gameObject.SetActive(false);Check(!f.Pause.HasRetainedPause&&Time.timeScale==0.65f&&!f.View.gameObject.activeSelf,"HUD disable cleanup exceptions cannot retain inspection pause");f.End();

        f=new Fixture();var current=f.Battle;current.Close();
        Check(!f.Combat.TryGetInspectionStats(f.Target,current,out _,out _,out _)&&f.Failures==0,"inspection query does not report or mutate Battle failure");f.End();

        f=new Fixture();var prior=f.Target;Check(f.Open(),"switch setup");f.Hud.CloseTowerInfo();
        var second=new GameObject();var secondTower=second.AddComponent<TowerInstance>();var secondCombat=second.AddComponent<TowerCombatBehaviour>();secondCombat.Bind(secondTower,f.Battle);secondTower.TowerDefinition.DisplayName="Other";f.Members.Members.Add(secondTower);
        Check(f.Hud.TryOpenTowerInfo(secondTower,out _)&&Get<TMP_Text>(f.View,"displayNameText").text=="Other"&&f.Grid.children.Count==0,"switch after Close binds latest target with no old upgrades");f.End();
        f=new Fixture();Check(f.Open(),"stale callback setup");var oldClose=Get<Action>(f.View,"closeRequested");
        Check(!f.Hud.BindTowerInspection(f.Battle,f.Members,f.Pause,out _),"Bind requires previous session to finish before replacement");
        f.Hud.CancelTowerInspection();Check(f.Open(),"Cancel retains current Battle binding");
        oldClose();Check(f.Hud.IsTowerInfoOpen&&f.Pause.HasRetainedPause,"old Close cannot clear newer session");
        f.Hud.ClearTowerInspectionBinding();Check(!f.Open()&&!f.Pause.HasRetainedPause,"Clear removes binding after cancellation");
        Check(f.Hud.BindTowerInspection(f.Battle,f.Members,f.Pause,out _)&&f.Open(),"explicit rebind after clear");
        int authoredClicks=0;f.Close.onClick.AddListener(()=>authoredClicks++);f.Close.onClick.Invoke();
        Check(authoredClicks==1&&!f.Hud.IsTowerInspectionBusy&&Time.timeScale==0.65f,"own listener lifecycle preserves authored handlers");f.End();

        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1f})
        {
            f=new Fixture();Set(f.Combat,"cachedResolvedStats",new ResolvedTowerCombatStats(invalid,1,10,15.5f,5,0,0));
            Check(!f.Open()&&!f.Pause.HasRetainedPause&&f.Failures==0,"invalid range query rejects without Battle side effects");f.End();
        }
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1f})
        {
            f=new Fixture();Set(f.Combat,"cachedResolvedStats",new ResolvedTowerCombatStats(3,invalid,10,15.5f,5,0,0));
            Check(!f.Open()&&!f.Pause.HasRetainedPause&&f.Failures==0,"invalid cycle query rejects without Battle side effects");f.End();
        }
        f=new Fixture();Set(f.Combat,"cachedResolvedStats",new ResolvedTowerCombatStats(3,0,10,15.5f,5,0,0));
        Check(f.Open()&&Get<TMP_Text>(f.View,"attackCycleDurationText").text=="0 s","zero cycle is valid and shown in seconds");f.End();
        f=new Fixture();Set(f.View,"attackCycleDurationText",null);
        Check(!f.Open()&&!f.Pause.HasRetainedPause&&f.Grid.children.Count==0,"cycle text required");f.End();
        f=new Fixture();Set(f.Combat,"cachedResolvedStats",new ResolvedTowerCombatStats(3,1,10,0,0,0,0));
        Check(!f.Open()&&!f.Pause.HasRetainedPause,"nonpositive Attack rejected");f.End();
        f=new Fixture();Check(f.Open(),"throwing close setup");changed=f;
        GameObject.BeforeDeactivate=go=>{if(go==changed.View.gameObject)throw new Exception("native hide fault");};
        f.Hud.CloseTowerInfo();var visibility=f.View.GetComponent<CanvasGroup>();
        Check(!f.Pause.HasRetainedPause&&Time.timeScale==0.65f&&visibility.alpha==0&&!visibility.blocksRaycasts&&f.Failures==1,
            "hide failure releases pause, removes blocking surface, and reports exact Battle failure");
        GameObject.BeforeDeactivate=null;f.End();
        f=new Fixture();Check(f.Open(),"content validity setup");Get<RectTransform>(f.View,"contentRoot").gameObject.SetActive(false);Frame(f.Hud);
        Check(!f.Hud.IsTowerInfoOpen&&!f.Pause.HasRetainedPause,"loss of content cancels on presentation frame");f.End();
        f=new Fixture();Get<TMP_Text>(f.View,"displayNameText").transform.SetParent(Get<RectTransform>(f.View,"contentRoot"),false);
        Check(f.Open(),"stats references may be anywhere within Content without a required BasicStats parent");f.End();
        Console.WriteLine("PASS production TowerInfoWindow/HUD/Snapshot/Binding/Pause: "+checks+" assertions (native and membership boundaries doubled)");
    }
}
