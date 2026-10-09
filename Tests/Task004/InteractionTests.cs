using System;
using UnityEngine;
class PendingDraftUIItem
{
    public PendingDraftEntry Entry;
    public DraftResult DraftResult=>Entry.DraftResult;
}
class BattleHUDUI
{
    public bool IsDraftOpen,IsTowerInspectionBusy;
    internal bool IsDraftSessionBusy=>IsDraftOpen;
    public DraftSystem DraftOwner;
    public Action OnRelease;
    public bool IsCurrentPendingView(PendingDraftUIItem item)=>item!=null&&DraftOwner.PendingOwner.CanConsume(item.Entry);
    public bool IsScreenPositionInsideDraftItemInteractionArea(Vector3 p)=>false;
    public void ReleaseConsumedPendingDraftView(PendingDraftUIItem item){if(item?.Entry.IsConsumed==true)OnRelease?.Invoke();}
}
namespace UnityEngine { public static class Input {
    public static Vector3 mousePosition;public static bool LeftHeld=true,LeftUp,RightDown;
    public static bool GetMouseButton(int button)=>LeftHeld;
    public static bool GetMouseButtonUp(int button)=>LeftUp;
    public static bool GetMouseButtonDown(int button)=>RightDown;
} }
partial class InteractionHarness : MonoBehaviour
{
    private CameraInputStub battlefieldInput;
    bool modalReleasePending;
    bool isDragging;
    MonsterDashedPathSession pathSession;
    int previewMoves;
    private void UpdatePreviewPosition(Vector3 p){previewMoves++;}
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
    private void CancelPlacement(){isDragging=false;modalReleasePending=false;OnCancel?.Invoke();}
    internal static void TestInspectionAvailability()
    {
        var f=new SubmissionFixture();var h=new InteractionHarness{submission=f.Submission,towerUpgradeSystem=f.System,battleHUDUI=new BattleHUDUI{DraftOwner=f.Draft}};
        h.isCompletingPlacement=true;
        if(h.IsAvailableForInspection)throw new Exception("outer completion admitted inspection");
        h.isCompletingPlacement=false;h.modalReleasePending=true;
        if(h.IsAvailableForInspection)throw new Exception("pending release admitted inspection");
        h.modalReleasePending=false;h.battleHUDUI.IsTowerInspectionBusy=true;
        if(!h.IsAvailableForInspection||h.CanStartDraftInteraction)throw new Exception("inspection own preparation and gameplay permissions not separated");
    }
    internal static void TestModal()
    {
        var f=new SubmissionFixture();var h=new InteractionHarness{submission=f.Submission,towerUpgradeSystem=f.System,
            battleHUDUI=new BattleHUDUI{DraftOwner=f.Draft,IsDraftOpen=true},currentDraftEntry=new PendingDraftUIItem{Entry=f.Item},
            currentDraftResult=f.Item.DraftResult,currentUpgradeTarget=f.Behaviour,isDragging=true};
        Input.LeftHeld=true;Input.LeftUp=false;Input.RightDown=false;
        h.Update();h.CompletePlacement();
        if(h.previewMoves!=0||f.Item.IsConsumed||!h.isDragging)throw new Exception("modal movement/submit leak");
        Input.LeftHeld=false;Input.LeftUp=true;h.Update();
        if(!h.modalReleasePending||h.CanStartDraftInteraction||!h.isDragging)throw new Exception("modal release state");
        h.battleHUDUI.IsDraftOpen=false;Input.LeftUp=false;h.Update();
        if(h.isDragging||h.modalReleasePending||f.Item.IsConsumed||h.previewMoves!=0)throw new Exception("release deployed on modal close");
        h.isDragging=true;h.battleHUDUI.IsDraftOpen=true;Input.LeftHeld=true;h.Update();
        h.battleHUDUI.IsDraftOpen=false;h.Update();
        if(!h.isDragging||h.previewMoves!=1)throw new Exception("held drag failed to resume");
    }

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

partial class RevocationHarness
{
    private CameraInputStub cameraPanController=new CameraInputStub();
    private BattleModalPauseAuthority ModalPause = new BattleModalPauseAuthority();
    internal RevocationHarness() { ModalPause.BindBattle(combatBinding); ModalPause.OpenBattle(combatBinding); }
    private MonsterDashedPathSession DashedPathSession=new MonsterDashedPathSession();
    private CombatBindingDouble combatBinding=new CombatBindingDouble();
    private TowerPlacementSubmission Submission=new TowerPlacementSubmission();
    private class CombatBindingDouble {internal bool Closed;internal void Close(){Closed=true;}}
    private void RunCleanupSafely(Action action){action();}
    internal static void TestRevocation()
    {
        var h=new RevocationHarness();h.RevokeCombatAuthority();
        if(h.ModalPause.CanAcquire)throw new Exception("pause acquisition survives revocation");
        if(!h.DashedPathSession.Closed||!h.combatBinding.Closed)throw new Exception("path not revoked at earliest boundary");
    }
}

class CameraInputStub {public bool IsPointerGestureOwned;public void ClearInspectionBinding(){}}
