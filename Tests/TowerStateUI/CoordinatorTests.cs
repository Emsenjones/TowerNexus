// Runs extracted, unchanged production coordinator methods with injected UI failures.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    static class Debug
    {
        public static void LogException(Exception exception, object context) { }
        public static void LogWarning(string message, object context) { }
        public static void LogError(string message, object context) { }
    }
}
struct DraftAttemptToken { }
class Consumer
{
    public object PlacementCamera = new object();
    public int InitialDrafts;
    public Action OnBegin, OnDraft, OnDestroy;
    public void BeginBattle() => OnBegin?.Invoke();
    public void Open() { }
    public bool TryOpenInitialTowerDraft(out DraftAttemptToken token, out string reason)
    { token=default;reason=null;InitialDrafts++;OnDraft?.Invoke();return true; }
    public bool IsAwaitingDraft(DraftAttemptToken token) => true;
    public void ForceCleanupAllMonsters() { }
    public void ClearStageUi() { }
    public void DestroyTrackedTowers() => OnDestroy?.Invoke();
    public void ClearStageBinding() { }
    public void ClearStagePools() { }
    public void ClearStageLevelRules() { }
    public void ClearActiveMap() { }
    public void StopTrackedTowerCombat() { }
}
class TowerStateUIManager
{
    public Action BindAction, ClearAction;
    public int Binds, Clears;
    public void Bind(Consumer submission, object camera) { Binds++;BindAction?.Invoke(); }
    public void Clear() { Clears++;ClearAction?.Invoke(); }
}
partial class CoordinatorHarness
{
    public TowerStateUIManager towerStateUIManager;
    public Consumer Submission=new Consumer(), playerSystem=new Consumer(), monsterManager=new Consumer(),
        combatBinding=new Consumer(), towerPlacementController=new Consumer(), draftSystem=new Consumer(),
        monsterSpawner=new Consumer(), towerUpgradeSystem=new Consumer(), pathfindingService=new Consumer();
    public bool IsBattleActive, hasFreshPlayerState, isBattlePrepared, isReleasing;
    public int preparedPlayerMaxHealth;
    public List<int> preparedPlayerProgressRequirements=new List<int>();
    public DraftAttemptToken expectedInitialDraftToken;
    bool CanBeginPreparedBattle(out string reason) { reason=null;return true; }
    bool AreConsumerGatesOpen() => true;
    bool CanContinueLifecycleOperation() => true;
    void BeginDashedPath() { }
    void ReleaseDashedPath() { }
    void RevokeCombatAuthority() { }
    void CaptureBeforeCleanup() { }
    void CloseBattleAuthorityAndGates() { IsBattleActive=false; }
    void ResetResultTracking() { }
    public bool Begin() => BeginPreparedBattleCore();
    public void Release() => ReleasePreparedBattleRuntimeCore();
}
class CoordinatorTests
{
    static void Check(bool value,string message) { if(!value)throw new Exception(message); }
    static void Main()
    {
        var missing=new CoordinatorHarness();
        Check(missing.Begin()&&missing.draftSystem.InitialDrafts==1,"missing manager must preserve startup");
        Console.WriteLine("PASS missing optional UI preserves Initial Draft");

        foreach(bool clearThrows in new[]{false,true})
        {
            var manager=new TowerStateUIManager { BindAction=()=>throw new Exception("injected bind failure") };
            if(clearThrows)manager.ClearAction=()=>throw new Exception("injected cleanup failure");
            var h=new CoordinatorHarness { towerStateUIManager=manager };
            Check(h.Begin()&&h.IsBattleActive&&h.draftSystem.InitialDrafts==1&&manager.Clears==1,
                "UI exception aborted Initial Draft");
            Console.WriteLine("PASS Bind failure, cleanup throws="+clearThrows+", Initial Draft still opens");
        }

        var order=new List<string>();var ui=new TowerStateUIManager { BindAction=()=>order.Add("bind"),ClearAction=()=>order.Add("clear") };
        var runtime=new CoordinatorHarness { towerStateUIManager=ui };
        runtime.Submission.OnBegin=()=>order.Add("submission");runtime.draftSystem.OnDraft=()=>order.Add("draft");
        runtime.Submission.OnDestroy=()=>order.Add("destroy");
        Check(runtime.Begin()&&string.Join(",",order)=="submission,bind,draft","binding order");
        runtime.StopBattle();Check(ui.Clears==0,"Stop cleared UI");
        runtime.Release();Check(string.Join(",",order)=="submission,bind,draft,clear,destroy","release ordering");
        Console.WriteLine("PASS bind precedes Initial Draft; Stop retains UI; Release clears before Tower destruction");

        bool towersDestroyed=false;
        var failingRelease=new CoordinatorHarness { towerStateUIManager=new TowerStateUIManager{ClearAction=()=>throw new Exception("clear")} };
        failingRelease.Submission.OnDestroy=()=>towersDestroyed=true;failingRelease.Release();
        Check(towersDestroyed&&!failingRelease.isReleasing,"UI cleanup blocked runtime release");
        Console.WriteLine("PASS throwing UI clear does not block Tower release");
        Console.WriteLine("5 coordinator integration cases passed (production method extraction, boundary doubles).");
    }
}
