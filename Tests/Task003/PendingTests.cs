using System;
using System.Collections.Generic;
using UnityEngine;
public class PendingDraftUIItem
{
    public bool isActiveAndEnabled = true;
    public PendingDraftEntry Entry;
    public DraftResult DraftResult => Entry.DraftResult;
    public TowerUpgradeDefinition TowerUpgradeDefinition => Entry.TowerUpgradeDefinition;
}
public partial class BattleHUDUI
{
    public bool isBattleActive = true, isActiveAndEnabled = true;
    public DraftSystem DraftOwner;
    private List<PendingDraftUIItem> pendingDraftItems => Views;
    public List<PendingDraftUIItem> Views = new List<PendingDraftUIItem>();
    public IReadOnlyList<PendingDraftUIItem> PendingDraftItems => Views; // Baseline sampler only.
    public int PreparationCount, FailAt = -1, Discarded, Closed;
    public Action OnPrepare;
    public void PrepareViewCapacity(int count) {}
    public bool TryPreparePendingView(PendingDraftEntry entry, out PendingDraftUIItem item, out string reason)
    {
        PreparationCount++; OnPrepare?.Invoke(); item = null; reason = "injected preparation failure";
        if (PreparationCount == FailAt) return false;
        item = new PendingDraftUIItem { Entry = entry }; return true;
    }
    public bool ArePreparedViewsUsable(IReadOnlyList<PendingDraftUIItem> views)
    { foreach(var item in views) if (!item.isActiveAndEnabled) return false; return isActiveAndEnabled; }
    public void RegisterPreparedViews(IReadOnlyList<PendingDraftUIItem> views) { Views.AddRange(views); }
    public void DiscardPreparedViews(IReadOnlyList<PendingDraftUIItem> views) { Discarded += views.Count; }
    public void ReplacePendingViews(IReadOnlyList<PendingDraftUIItem> views)
    { Views.Clear(); Views.AddRange(views); }
    internal void ReleaseConsumedPendingDraftView(PendingDraftUIItem item) {}
    public void CloseDraft() { Closed++; }
}
public partial class DraftSystem : UnityEngine.MonoBehaviour
{
    private enum DraftSessionPhase { None, AwaitingSelection, CommittingSelection, Failed, Completed }
    private enum DraftSessionKind { Initial, LevelUp }
    internal PendingDraftCollection PendingOwner = new PendingDraftCollection();
    public IReadOnlyList<PendingDraftEntry> PendingDrafts => PendingOwner.Held;
    public BattleHUDUI battleHUDUI = new BattleHUDUI();
    public TowerPlacementController towerPlacementController = new TowerPlacementController();
    internal bool IsPendingMutationBusy => isPreparingPendingViews || sessionPhase == DraftSessionPhase.CommittingSelection;
    private bool isPreparingPendingViews, isBattleActive = true;
    private ulong currentBattleGeneration = 1;
    private DraftAttemptToken activeToken = new DraftAttemptToken(1,1), completedInitialToken;
    private PendingDraftEntry committedInitialHeldItem;
    private DraftSessionPhase sessionPhase = DraftSessionPhase.AwaitingSelection;
    private DraftSessionKind sessionKind = DraftSessionKind.Initial;
    private HashSet<UnityEngine.Object> displayedIdentities = new HashSet<UnityEngine.Object>();
    public int Completed, Failed, PauseReleases;
    private Action<DraftAttemptToken> OnInitialDraftCompleted;
    private Action<DraftAttemptToken,string> OnInitialDraftFailed;
    public DraftSystem()
    { battleHUDUI.DraftOwner = this; PendingOwner.BeginBattle(1); OnInitialDraftCompleted = t => Completed++; OnInitialDraftFailed = (t,s) => Failed++; }
    private bool IsCommittingSelection(DraftAttemptToken token) => isBattleActive && activeToken == token && sessionPhase == DraftSessionPhase.CommittingSelection;
    private void InvalidateActiveAuthority() { activeToken = default; }
    private void ReleasePause(DraftAttemptToken token) { PauseReleases++; }
    public void EnterGameplay() { sessionPhase = DraftSessionPhase.Completed; }
    public void Stop() { isBattleActive = false; PendingOwner.Stop(); activeToken = default; }
    public void Select(DraftResult result) { displayedIdentities.Add(result.Identity); HandleDraftSelected(activeToken, result); }
    public bool Grant(params DraftResult[] results)
    {
        var tokens = new List<DraftAttemptToken>();
        for(int i=0;i<results.Length;i++) tokens.Add(new DraftAttemptToken(99,(ulong)i+1));
        return TryGrantPendingBatch(results,tokens,default,out _,out _);
    }
}
class PendingTests
{
    static int count;
    static void Check(bool condition, string reason) { if(!condition) throw new Exception(reason); }
    static void Run(string name, Action action) { action(); count++; Console.WriteLine("PASS "+name); }
    static DraftResult Reward() => DraftResult.CreateTowerDraft(new TowerDefinition());
    static void Main()
    {
        Run("initial success registers model before completion", () => {
            var d = new DraftSystem(); d.Select(Reward());
            Check(d.PendingDrafts.Count == 1 && d.Completed == 1 && d.PauseReleases == 1 &&
                ReferenceEquals(d.PendingDrafts[0], d.battleHUDUI.Views[0].Entry),"complete initial");
        });
        Run("initial preparation failure cannot authorize spawning", () => {
            var d = new DraftSystem(); d.battleHUDUI.FailAt = 1; d.Select(Reward());
            Check(d.PendingDrafts.Count == 0 && d.Completed == 0 && d.Failed == 1 && d.PauseReleases == 1,"failed initial");
        });
        Run("initial Stop during preparation rejects stale registration", () => {
            var d = new DraftSystem(); d.battleHUDUI.OnPrepare = d.Stop; d.Select(Reward());
            Check(d.PendingDrafts.Count == 0 && d.Completed == 0 && d.battleHUDUI.Discarded == 1,"stale initial");
        });
        Run("presentation lost during preparation cannot commit Initial", () => {
            var d=new DraftSystem(); d.battleHUDUI.OnPrepare=()=>d.battleHUDUI.isActiveAndEnabled=false;
            d.Select(Reward()); Check(d.Completed==0 && d.Failed==1 && d.PendingDrafts.Count==0,"usable presentation required");
        });
        Run("batch halfway preparation failure retains earlier accepted rewards", () => {
            var d = new DraftSystem(); Check(d.Grant(Reward()),"first grant"); var first=d.PendingDrafts[0];
            d.battleHUDUI.FailAt=3; Check(!d.Grant(Reward(),Reward()),"batch fail");
            Check(d.PendingDrafts.Count==1 && ReferenceEquals(first,d.PendingDrafts[0]) && !first.IsConsumed &&
                d.battleHUDUI.Views.Count==1 && d.battleHUDUI.Discarded==1,"no rollback consumption");
        });
        Run("batch preparation is non-reentrant and atomic", () => {
            var d = new DraftSystem(); d.battleHUDUI.OnPrepare=()=> {
                Check(d.PendingDrafts.Count==0,"nothing committed during preparation");
                Check(!d.Grant(Reward()),"nested grant blocked");
            };
            Check(d.Grant(Reward(),Reward()) && d.PendingDrafts.Count==2,"whole batch");
        });
        Run("rebuild preserves identities and order and cancels gesture", () => {
            var d = new DraftSystem(); d.Grant(Reward(),Reward()); var first=d.PendingDrafts[0]; var old=d.battleHUDUI.Views[0];
            Check(d.TryRebuildPendingViews(out _) && d.towerPlacementController.Cancellations==1,"rebuilt");
            Check(ReferenceEquals(first,d.PendingDrafts[0]) && ReferenceEquals(first,d.battleHUDUI.Views[0].Entry) &&
                !d.battleHUDUI.IsCurrentPendingView(old) && d.battleHUDUI.IsCurrentPendingView(d.battleHUDUI.Views[0]) && d.PendingOwner.CanConsume(first),"same ownership, replaced view");
        });
        Run("failed rebuild keeps old views and rewards", () => {
            var d = new DraftSystem(); d.Grant(Reward(),Reward()); var old=d.battleHUDUI.Views[0];
            d.battleHUDUI.FailAt=4; Check(!d.TryRebuildPendingViews(out _),"rebuild fail");
            Check(d.PendingDrafts.Count==2 && ReferenceEquals(old,d.battleHUDUI.Views[0]),"retain old presentation");
        });
        Run("Stop retains read-only snapshot and invalidates ticket", () => {
            var d=new DraftSystem();d.Grant(Reward());var e=d.PendingDrafts[0];
            d.PendingOwner.TryPrepareConsumption(e,out var ticket);d.Stop();
            Check(!d.PendingOwner.TryCommitConsumption(ticket) && !e.IsConsumed && d.PendingDrafts.Count==1,"stopped");
            Check(((ICollection<PendingDraftEntry>)d.PendingDrafts).IsReadOnly,"read only");
        });
        Run("single use, cross owner and expired generation", () => {
            var d=new DraftSystem();d.Grant(Reward());var e=d.PendingDrafts[0];var other=new PendingDraftCollection();other.BeginBattle(1);
            Check(!other.TryPrepareConsumption(e,out _),"owner identity");
            d.PendingOwner.TryPrepareConsumption(e,out var t);Check(!other.TryCommitConsumption(t),"foreign ticket");
            Check(d.PendingOwner.TryCommitConsumption(t) && !d.PendingOwner.TryCommitConsumption(t),"once");
            d.PendingOwner.BeginBattle(1);Check(!d.PendingOwner.IsCurrent(e),"even reused numeric generation invalidates");
        });
        Run("hidden and consumed views reject production interaction predicate", () => {
            var d=new DraftSystem(); d.Grant(Reward()); var view=d.battleHUDUI.Views[0];
            view.isActiveAndEnabled=false; Check(!d.battleHUDUI.IsCurrentPendingView(view),"hidden view");
            view.isActiveAndEnabled=true; d.battleHUDUI.isActiveAndEnabled=false;
            Check(!d.battleHUDUI.IsCurrentPendingView(view),"hidden HUD");
            d.battleHUDUI.isActiveAndEnabled=true; d.PendingOwner.TryPrepareConsumption(view.Entry,out var t);
            d.PendingOwner.TryCommitConsumption(t); Check(!d.battleHUDUI.IsCurrentPendingView(view),"late destruction");
        });
        Run("invalid middle batch result grants nothing", () => {
            var owner=new PendingDraftCollection();owner.BeginBattle(1);
            Check(!owner.TryPrepareGrant(new[]{Reward(),null,Reward()},new[]{new DraftAttemptToken(1,1),new DraftAttemptToken(1,2),new DraftAttemptToken(1,3)},out _) && owner.Held.Count==0,"invalid batch");
        });
        Run("real Draft grant/rebuild entries reject through refresh, notification and outer cleanup", () => {
            var f=new SubmissionFixture(); f.Draft.EnterGameplay(); f.Draft.towerPlacementController.Submission=f.Submission;
            Action blocked=()=> {
                Check(!f.Draft.TryGrantDebugPendingBatch(new[]{Reward()},new[]{new DraftAttemptToken(99,1)},out _),"grant gate");
                Check(!f.Draft.TryRebuildPendingViews(out _),"rebuild gate");
            };
            f.Combat.OnRefresh=blocked; f.Submission.OnTowerInvestmentCommitted+=e=>blocked();
            Check(f.Submission.TryBeginInteraction(out var lease),"outer interaction");
            Check(f.Submission.SubmitUpgrade(f.Item,f.Tower,lease).IsCommitted,"authorized operation");
            blocked(); lease.Dispose();
            Check(f.Draft.TryGrantDebugPendingBatch(new[]{Reward()},new[]{new DraftAttemptToken(99,2)},out _),"grant recovers after cleanup");
        });
        Run("fixed seed baseline matches weights, choices and subsequent RNG", () => {
            var upgrades=new List<TowerUpgradeDefinition>{new TowerUpgradeDefinition{name="Basic"},new TowerUpgradeDefinition{name="Behaviour",UpgradeLayer=TowerUpgradeLayer.Behaviour},
                new TowerUpgradeDefinition{name="ElementalA",UpgradeLayer=TowerUpgradeLayer.Elemental},new TowerUpgradeDefinition{name="ElementalB",UpgradeLayer=TowerUpgradeLayer.Elemental},
                new TowerUpgradeDefinition{name="MagicElemental",UpgradeLayer=TowerUpgradeLayer.Elemental,TowerFamily=TowerFamily.Magic}};
            var towers=new List<TowerDefinition>{new TowerDefinition{name="Archer"},new TowerDefinition{name="Magic",TowerFamily=TowerFamily.Magic}};
            for(int seed=0;seed<100;seed++) for(int state=0;state<3;state++)
                Check(new CurrentSampler().Trace(seed,upgrades,towers,state)==new BaselineSampler().Trace(seed,upgrades,towers,state),"seed/state "+seed+"/"+state);
        });
        Console.WriteLine(count+" managed Pending tests passed; includes 300 baseline Draft traces. Native Unity acceptance remains separate.");
    }
}

public class TowerPlacementController
{
    internal TowerPlacementSubmission Submission;
    public bool CanStartDraftInteraction=>Submission==null || !Submission.IsBusy;
    public int Cancellations;
    public IReadOnlyList<TowerInstance> DeployedTowerInstances=>Array.Empty<TowerInstance>();
    public void CancelPlacement(){Cancellations++;}
}
