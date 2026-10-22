using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
class PendingDraftUIItem
{
    public PendingDraftEntry Entry;
    public DraftResult DraftResult=>Entry.DraftResult;
}
class BattleHUDUI
{
    public bool IsDraftOpen,IsTowerInspectionBusy,IsExitConfirmationBusy;
    internal bool IsBattleModalBusy=>IsDraftOpen||IsTowerInspectionBusy||IsExitConfirmationBusy;
    internal bool IsDraftSessionBusy=>IsDraftOpen;
    public DraftSystem DraftOwner;
    public Action OnRelease;
    public bool IsCurrentPendingView(PendingDraftUIItem item)=>item!=null&&DraftOwner.PendingOwner.CanConsume(item.Entry);
    public bool IsScreenPositionInsideDraftItemInteractionArea(Vector3 p)=>false;
    public void ReleaseConsumedPendingDraftView(PendingDraftUIItem item){if(item?.Entry.IsConsumed==true)OnRelease?.Invoke();}
}
namespace UnityEngine.EventSystems { public class PointerEventData { public PointerEventData(object system) {} } }
partial class InteractionHarness : MonoBehaviour
{
    private CameraInputStub battlefieldInput;
    bool modalReleasePending;
    Mouse dragMouse=Mouse.current;
    TouchControl dragTouch;
    int dragTouchId;
    Vector3 dragScreenPosition;
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
        h.battleHUDUI.IsTowerInspectionBusy=false;h.battleHUDUI.IsExitConfirmationBusy=true;
        if(!h.IsAvailableForInspection||h.CanStartDraftInteraction)throw new Exception("exit session own validation must remain separate from new gameplay admission");
    }
    internal static void TestPointerMigration()
    {
        Mouse.current=new Mouse();
        var h=new InteractionHarness();
        Mouse.current.leftButton.isPressed=true;
        if(!h.CaptureDragPointer(null))throw new Exception("mouse capture");
        var owner=Mouse.current;
        Mouse.current=new Mouse();
        owner.position.value=new Vector3(42,0,0);
        if(!h.ReadDragPointer(out bool held,out bool released,out bool cancelled)||!held||h.dragScreenPosition.x!=42)
            throw new Exception("mouse ownership switched to current device");
        owner.rightButton.wasPressedThisFrame=true;
        h.ReadDragPointer(out held,out released,out cancelled);
        if(!cancelled)throw new Exception("right cancel missing");
        owner.added=false;
        if(h.ReadDragPointer(out held,out released,out cancelled))throw new Exception("removed mouse accepted");
        var screen=new Touchscreen();
        var first=new TouchControl{device=screen};first.touchId.value=11;first.press.isPressed=true;
        var second=new TouchControl{device=screen};second.touchId.value=12;second.press.isPressed=true;
        screen.touches=new[]{first,second};screen.primaryTouch=first;Touchscreen.current=screen;
        if(!h.CaptureDragPointer(new ExtendedPointerEventData{device=screen,touchId=12}))throw new Exception("UI touch capture");
        first.phase.value=TouchPhase.Ended;first.press.isPressed=false;second.position.value=new Vector3(17,0,0);
        h.ReadDragPointer(out held,out released,out cancelled);
        if(!held||released||h.dragScreenPosition.x!=17)throw new Exception("other finger released placement");
        second.phase.value=TouchPhase.Ended;second.press.isPressed=false;
        h.ReadDragPointer(out held,out released,out cancelled);
        if(held||!released||cancelled)throw new Exception("owned finger release");
        second.phase.value=TouchPhase.Canceled;
        h.ReadDragPointer(out held,out released,out cancelled);
        if(released||!cancelled)throw new Exception("touch cancel became submission");
        second.touchId.value=13;
        if(h.ReadDragPointer(out held,out released,out cancelled))throw new Exception("reused touch slot accepted");
        Touchscreen.current=null;Mouse.current=null;
        if(h.CaptureDragPointer(null))throw new Exception("missing device accepted");
        Mouse.current=new Mouse();
        Console.WriteLine("PASS placement Input System device/contact ownership, release, cancellation and missing devices");
    }
    internal static void TestModal()
    {
        TestPointerMigration();
        var f=new SubmissionFixture();var h=new InteractionHarness{submission=f.Submission,towerUpgradeSystem=f.System,
            battleHUDUI=new BattleHUDUI{DraftOwner=f.Draft,IsDraftOpen=true},currentDraftEntry=new PendingDraftUIItem{Entry=f.Item},
            currentDraftResult=f.Item.DraftResult,currentUpgradeTarget=f.Behaviour,isDragging=true};
        Mouse.current.leftButton.isPressed=true;Mouse.current.leftButton.wasReleasedThisFrame=false;Mouse.current.rightButton.wasPressedThisFrame=false;
        h.Update();h.CompletePlacement();
        if(h.previewMoves!=0||f.Item.IsConsumed||!h.isDragging)throw new Exception("modal movement/submit leak");
        Mouse.current.leftButton.isPressed=false;Mouse.current.leftButton.wasReleasedThisFrame=true;h.Update();
        if(!h.modalReleasePending||h.CanStartDraftInteraction||!h.isDragging)throw new Exception("modal release state");
        h.battleHUDUI.IsDraftOpen=false;Mouse.current.leftButton.wasReleasedThisFrame=false;h.Update();
        if(h.isDragging||h.modalReleasePending||f.Item.IsConsumed||h.previewMoves!=0)throw new Exception("release deployed on modal close");
        h.isDragging=true;h.battleHUDUI.IsDraftOpen=true;Mouse.current.leftButton.isPressed=true;h.Update();
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

    private bool isReleasing,isLifecycleOperationInProgress,releaseRequested,terminalObservationInProgress;
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
