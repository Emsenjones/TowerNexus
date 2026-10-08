using System;
using UnityEngine;

public partial class BattleHUDUI
{
    [SerializeField] private TowerInfoWindow towerInfoWindow;
    private enum TowerInspectionPhase { Idle, Opening, Open, Closing }
    private TowerInspectionPhase inspectionPhase;
    private bool inspectionOperation;
    private bool inspectionClearing;
    private ulong inspectionBindingRevision;
    private BattleCombatBinding inspectionBattle;
    private TowerPlacementSubmission inspectionMembers;
    private BattleModalPauseAuthority inspectionPause;
    private InspectionSession inspectionSession;

    private sealed class InspectionSession
    {
        internal readonly BattleCombatBinding Battle;
        internal readonly TowerInfoWindow View;
        internal readonly TowerInfoWindow.PreparedContent Content = new TowerInfoWindow.PreparedContent();
        internal TowerInspectionSnapshot Snapshot;
        internal BattleModalPauseHandle Pause;
        internal InspectionSession(BattleCombatBinding battle, TowerInfoWindow view) { Battle = battle; View = view; }
    }

    public bool IsTowerInfoOpen => inspectionPhase == TowerInspectionPhase.Open;
    public bool IsTowerInspectionBusy => inspectionPhase != TowerInspectionPhase.Idle || inspectionOperation;

    internal bool BindTowerInspection(BattleCombatBinding battle, TowerPlacementSubmission members,
        BattleModalPauseAuthority pause, out string reason)
    {
        reason = "Cannot bind Tower inspection during an active operation.";
        if (IsTowerInspectionBusy) return false;
        ClearTowerInspectionBinding();
        ulong revision = inspectionBindingRevision;
        if (!isActiveAndEnabled || battle == null || members == null || pause == null || towerInfoWindow == null)
        { reason = "Tower inspection dependencies are incomplete."; return false; }
        if (!towerInfoWindow.TryValidateReferences(out reason)) return false;
        inspectionOperation = true;
        try
        {
            towerInfoWindow.Attach();
            // Attach may disable authored objects and reenter HUD cleanup.
            if (!isActiveAndEnabled || towerInfoWindow == null || revision != inspectionBindingRevision)
            { reason = "Tower inspection binding was cancelled."; return false; }
            inspectionBattle = battle;
            inspectionMembers = members;
            inspectionPause = pause;
            reason = string.Empty;
            return true;
        }
        catch (Exception error) { reason = error.Message; Debug.LogException(error, this); return false; }
        finally { inspectionOperation = false; }
    }

    internal bool TryOpenTowerInfo(TowerInstance target, out string reason)
    {
        reason = "Tower inspection is busy or Battle/modal/placement authority is unavailable.";
        if (IsTowerInspectionBusy || !CanOpenInspection()) return false;
        var session = new InspectionSession(inspectionBattle, towerInfoWindow);
        inspectionSession = session;
        inspectionPhase = TowerInspectionPhase.Opening;
        inspectionOperation = true;
        bool succeeded = false;
        try
        {
            if (!session.View.TryValidateReferences(out reason) ||
                !TowerInspectionSnapshot.TryCapture(target, session.Battle, inspectionMembers, out session.Snapshot, out reason) ||
                !session.View.TryPrepare(session.Snapshot, session.Content, out reason)) return false;
            if (!IsCurrent(session) || !session.Snapshot.IsCurrent(session.Battle, inspectionMembers))
            { reason = "Tower or inspection authority changed during preparation."; return false; }
            if (!inspectionPause.TryAcquire(session.Battle, BattleModalKind.TowerInspection, session, out session.Pause, out reason))
                return false;
            session.View.Show(() => CloseInspectionSession(session), () => CloseInspectionSession(session));
            if (!IsCurrent(session) || session.View == null || !session.View.IsVisible ||
                !inspectionPause.Owns(session.Pause) || !session.Snapshot.IsCurrent(session.Battle, inspectionMembers))
            { reason = "Tower or inspection authority changed during activation."; return false; }
            inspectionPhase = TowerInspectionPhase.Open;
            succeeded = true;
            reason = string.Empty;
            return true;
        }
        catch (Exception error) { reason = error.Message; Debug.LogException(error, this); return false; }
        finally
        {
            try { if (!succeeded) CloseInspectionSession(session); }
            finally { inspectionOperation = false; }
        }
    }

    private bool CanOpenInspection() => isActiveAndEnabled && towerInfoWindow != null && !IsDraftOpen &&
        inspectionBattle != null && inspectionBattle.IsOpenForRead && inspectionMembers != null &&
        !inspectionMembers.IsBusy && inspectionMembers.CanStartOperation &&
        (towerPlacementController == null || !towerPlacementController.IsDragging) &&
        inspectionPause != null && inspectionPause.IsBattleOpen(inspectionBattle) && inspectionPause.CanAcquire;

    private bool IsCurrent(InspectionSession session) => ReferenceEquals(session, inspectionSession) &&
        ReferenceEquals(session.Battle, inspectionBattle) && isActiveAndEnabled &&
        inspectionPhase == TowerInspectionPhase.Opening && !session.Content.Disposed &&
        inspectionPause != null && inspectionPause.IsBattleOpen(session.Battle) &&
        inspectionMembers != null && !inspectionMembers.IsBusy &&
        (session.Pause != null || inspectionMembers.CanStartOperation) &&
        (towerPlacementController == null || !towerPlacementController.IsDragging) && !IsDraftOpen;

    public void CloseTowerInfo() { CancelTowerInspection(); }
    internal void CancelTowerInspection() { CloseInspectionSession(inspectionSession); }

    internal void ClearTowerInspectionBinding()
    {
        if (inspectionClearing) return;
        inspectionClearing = true;
        bool outerOperation = inspectionOperation;
        inspectionOperation = true;
        try
        {
            inspectionBindingRevision++;
            // Remove admission first, including preparation/activation callbacks.
            inspectionBattle = null;
            CancelTowerInspection();
        }
        finally
        {
            inspectionMembers = null;
            inspectionPause = null;
            try { TryHudCleanup(() => { if (towerInfoWindow != null) towerInfoWindow.Detach(); }); }
            finally { inspectionOperation = outerOperation; inspectionClearing = false; }
        }
    }

    private void CloseInspectionSession(InspectionSession session)
    {
        if (session == null || !ReferenceEquals(session, inspectionSession) || inspectionPhase == TowerInspectionPhase.Closing) return;
        inspectionPhase = TowerInspectionPhase.Closing;
        inspectionSession = null;
        var pause = session.Pause;
        session.Pause = null;
        bool hidden = true;
        try
        {
            try { if (session.View != null) session.View.Hide(); }
            catch (Exception error) { hidden = false; Debug.LogException(error, this); }
            try { session.Content.Dispose(); }
            catch (Exception error) { Debug.LogException(error, this); }
        }
        finally
        {
            try { pause?.Authority.Release(pause); }
            finally { inspectionPhase = TowerInspectionPhase.Idle; }
        }
        if (!hidden) session.Battle.ReportPresentationFailure("TowerInfoWindow could not close its modal surface.");
    }

    private void LateUpdate()
    {
        var session = inspectionSession;
        if (inspectionPhase != TowerInspectionPhase.Open || session == null) return;
        // Lifecycle checks remain responsive at timeScale zero; display values are not polled.
        if (session.View == null || !session.View.IsVisible || inspectionPause == null ||
            !inspectionPause.Owns(session.Pause) || !session.Snapshot.IsTargetAvailable(session.Battle, inspectionMembers))
            CloseInspectionSession(session);
    }

    private void OnDestroy() { TryHudCleanup(ClearTowerInspectionBinding); }
    private void TryHudCleanup(Action cleanup)
    {
        try { cleanup(); }
        catch (Exception error) { Debug.LogException(error, this); }
    }
}
