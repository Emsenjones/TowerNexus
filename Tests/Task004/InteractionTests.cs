using System;
using UnityEngine;
class PendingDraftUIItem
{
    public PendingDraftEntry Entry;
    public DraftResult DraftResult=>Entry.DraftResult;
}
class BattleHUDUI
{
    public DraftSystem DraftOwner;
    public Action OnRelease;
    public bool IsCurrentPendingView(PendingDraftUIItem item)=>item!=null&&DraftOwner.PendingOwner.CanConsume(item.Entry);
    public bool IsScreenPositionInsideDraftItemInteractionArea(Vector3 p)=>false;
    public void ReleaseConsumedPendingDraftView(PendingDraftUIItem item){if(item?.Entry.IsConsumed==true)OnRelease?.Invoke();}
}
namespace UnityEngine { public static class Input {public static Vector3 mousePosition;} }
partial class InteractionHarness : MonoBehaviour
{
    private TowerPlacementSubmission submission;
    private TowerUpgradeSystem towerUpgradeSystem;
    private BattleHUDUI battleHUDUI;
    private PendingDraftUIItem currentDraftEntry;
    private DraftResult currentDraftResult;
    private TowerBehaviour currentUpgradeTarget,currentLevelUpTarget;
    private TowerPlacementPreview currentPreview;
    private TowerPlacementValidator placementValidator;
    private bool isBattleActive=true,isCompletingPlacement,isTowerTargetCandidateActive,isLevelUpPreviewActive;
    private Action OnCancel;
    private bool IsTowerUpgradeDraftDrag()=>true;
    private void CancelPlacement(){OnCancel?.Invoke();}
    public static void TestCleanup()
    {
        foreach(bool throws in new[]{false,true})
        {
            var f=new SubmissionFixture();
            var h=new InteractionHarness{submission=f.Submission,towerUpgradeSystem=f.System,
                battleHUDUI=new BattleHUDUI{DraftOwner=f.Draft},currentDraftEntry=new PendingDraftUIItem{Entry=f.Item},
                currentDraftResult=f.Item.DraftResult,currentUpgradeTarget=f.Behaviour};
            bool cancelled=false;
            h.battleHUDUI.OnRelease=()=>{
                if(h.CanStartDraftInteraction||f.Submission.CanStartOperation||f.System.ApplyDebugUpgrade(f.Tower,new TowerUpgradeDefinition()).IsCommitted)
                    throw new Exception("UI cleanup accepted a nested request");
                h.CompletePlacement();
                if(throws)throw new InvalidOperationException("native destroy fault");
            };
            h.OnCancel=()=>{cancelled=true;if(!f.Submission.IsBusy)throw new Exception("guard ended before cancellation");};
            try{h.CompletePlacement();}catch(InvalidOperationException){if(!throws)throw;}
            if(!cancelled||f.Submission.IsBusy||h.isCompletingPlacement||!f.Item.IsConsumed||f.EvidenceCount!=1)
                throw new Exception("outer cleanup invariant");
        }
    }
}
partial class RecorderBindingHarness
{
    private TowerPlacementSubmission subscribedSubmission;
    private BattleRuntimeCoordinator battleRuntimeCoordinator;
    private int evidence,deployments,routes;
    private void HandleTowerDeploymentCommitted(TowerInstance tower){deployments++;}
    private void HandleTowerInvestmentCommitted(TowerInvestmentCommitObservation value){evidence++;}
    private void HandlePlacementRouteRevisionCommitted(TowerInstance t,TowerPlacementTopologyPlan p,MonsterRouteRevisionBatch b){routes++;}
    internal static void TestSubscriptions()
    {
        var f=new SubmissionFixture(false);var r=new RecorderBindingHarness{battleRuntimeCoordinator=f.Battle};
        for(int stage=0;stage<3;stage++)
        {
            r.Subscribe();r.Subscribe();
            var e=f.Grant(DraftResult.CreateTowerDraft(f.Definition));f.Submission.SubmitDeployment(e,f.Candidate(stage));
            r.Unsubscribe();r.Unsubscribe();
            f.Release();f.Owner.BeginBattle((ulong)stage+2);f.Battle.IsBattleActive=true;
            f.Submission.Bind(f.Battle,f.Draft,f.Validator,f.Deployer,f.System,f.Monsters,f.Map);f.Submission.BeginBattle();
        }
        if(r.evidence!=3||r.deployments!=3||r.routes!=3)throw new Exception("subscription count");
        // A runtime reference change must not unsubscribe from the wrong source.
        r.Subscribe();r.battleRuntimeCoordinator=new BattleRuntimeCoordinator();r.Unsubscribe();
        var last=f.Grant(DraftResult.CreateTowerDraft(f.Definition));f.Submission.SubmitDeployment(last,f.Candidate(6));
        if(r.evidence!=3)throw new Exception("old event source retained");
    }
}

partial class ReleaseRoutingHarness
{
    private void RevokeCombatAuthority() { }

    private bool isReleasing,isLifecycleOperationInProgress,releaseRequested;
    private int calls;
    private void ReleasePreparedBattleRuntimeCore(){calls++;}
    internal static void TestRouting()
    {
        var h=new ReleaseRoutingHarness{isReleasing=true,isLifecycleOperationInProgress=true};
        h.ReleasePreparedBattleRuntime();
        if(!h.releaseRequested||h.calls!=0)throw new Exception("release must cancel incoming Stage without recursive cleanup");
        h=new ReleaseRoutingHarness{isReleasing=true};h.ReleasePreparedBattleRuntime();
        if(h.releaseRequested||h.calls!=0)throw new Exception("ordinary release reentry must be idempotent");
    }
}
