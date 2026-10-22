using System;
using UnityEngine;

internal enum ExitStageAction { Restart, MainMenu }
internal enum ExitStageRequestDisposition { Rejected, Accepted }

// Acceptance is ownership transfer, not the eventual success of preparation.
internal sealed class ExitStageTransitionRequest
{
    internal readonly BattleHUDUI Source;
    internal readonly BattleCombatBinding Battle;
    internal readonly object Session;
    internal readonly ulong BindingRevision;
    internal readonly ExitStageAction Action;
    internal bool Accepted { get; private set; }

    internal ExitStageTransitionRequest(BattleHUDUI source, BattleCombatBinding battle,
        object session, ulong revision, ExitStageAction action)
    { Source = source; Battle = battle; Session = session; BindingRevision = revision; Action = action; }
    internal void Accept() { Accepted = true; }
}

public partial class BattleHUDUI
{
    [SerializeField] private ConfirmExitStageWindow exitStageConfirmWindow;
    private enum ExitConfirmationPhase { Idle, Opening, Open, AwaitingTransition, Closing }
    private ExitConfirmationPhase exitPhase;
    private bool exitOperation, exitClearing;
    private ulong exitBindingRevision;
    private BattleCombatBinding exitBattle;
    private TowerPlacementSubmission exitMembers;
    private BattleModalPauseAuthority exitPause;
    private ConfirmExitStageWindow boundExitView;
    private Func<ExitStageTransitionRequest, ExitStageRequestDisposition> exitTransition;
    private Func<bool> exitOwnerAvailable, exitOpeningAllowed;
    private ExitConfirmationSession exitSession;

    private sealed class ExitConfirmationSession
    {
        internal readonly BattleCombatBinding Battle;
        internal readonly ConfirmExitStageWindow View;
        internal readonly BattleModalPauseAuthority Authority;
        internal readonly ulong Revision;
        internal BattleModalPauseHandle Pause;
        internal ExitStageTransitionRequest Request;
        internal ExitConfirmationSession(BattleCombatBinding battle, ConfirmExitStageWindow view,
            BattleModalPauseAuthority authority, ulong revision)
        { Battle = battle; View = view; Authority = authority; Revision = revision; }
    }

    public bool IsExitConfirmationOpen => exitPhase == ExitConfirmationPhase.Open;
    public bool IsExitConfirmationBusy => exitPhase != ExitConfirmationPhase.Idle || exitOperation || exitClearing;

    internal bool BindExitConfirmation(BattleCombatBinding battle, TowerPlacementSubmission members,
        BattleModalPauseAuthority pause, Func<ExitStageTransitionRequest, ExitStageRequestDisposition> transition,
        out string reason, Func<bool> ownerAvailable = null, Func<bool> openingAllowed = null)
    {
        reason = "Exit confirmation cannot bind during a session or protected operation.";
        if (IsExitConfirmationBusy) return false;
        ClearExitConfirmationBinding();
        ulong revision = exitBindingRevision;
        var view = exitStageConfirmWindow;
        if (!isActiveAndEnabled || battle == null || members == null || pause == null || view == null)
        { reason = "Exit confirmation dependencies are incomplete."; return false; }
        if (!view.TryValidateReferences(out reason)) return false;
        exitOperation = true;
        bool attached = false;
        try
        {
            view.Attach();
            if (!isActiveAndEnabled || view == null || revision != exitBindingRevision)
            { reason = "Exit confirmation binding was cancelled."; return false; }
            exitBattle = battle;
            exitMembers = members;
            exitPause = pause;
            boundExitView = view;
            exitTransition = transition;
            exitOwnerAvailable = ownerAvailable;
            exitOpeningAllowed = openingAllowed;
            attached = true;
            reason = string.Empty;
            return true;
        }
        catch (Exception error) { reason = error.Message; Debug.LogException(error, this); return false; }
        finally
        {
            try { if (!attached && view != null) TryHudCleanup(view.Detach); }
            finally { exitOperation = false; }
        }
    }

    internal bool TryOpenExitConfirmation(out string reason)
    {
        reason = "Exit confirmation is busy or Battle/modal/placement authority is unavailable.";
        if (IsExitConfirmationBusy || (exitOpeningAllowed != null && !exitOpeningAllowed()) ||
            !ExitBattleAvailable() || !exitPause.CanAcquire) return false;
        var session = new ExitConfirmationSession(exitBattle, boundExitView, exitPause, exitBindingRevision);
        exitSession = session;
        exitPhase = ExitConfirmationPhase.Opening;
        exitOperation = true;
        bool succeeded = false;
        try
        {
            InvalidateBattlefieldInput();
            if (!IsExitSessionCurrent(session) || !session.View.TryValidateReferences(out reason)) return false;
            if (!session.Authority.TryAcquire(session.Battle, BattleModalKind.ExitConfirmation, session,
                    out session.Pause, out reason)) return false;
            session.View.Show(() => CancelExitSession(session),
                () => SubmitExitRequest(session, ExitStageAction.Restart),
                () => SubmitExitRequest(session, ExitStageAction.MainMenu), () => CloseExitSession(session));
            if (!IsExitSessionCurrent(session) || !session.View.IsVisible || !session.Authority.Owns(session.Pause) ||
                !session.View.TryValidateReferences(out reason))
            { reason = "Exit confirmation activation was cancelled."; return false; }
            exitPhase = ExitConfirmationPhase.Open;
            session.View.SetInteractionEnabled(true);
            succeeded = true;
            reason = string.Empty;
            return true;
        }
        catch (Exception error) { reason = error.Message; Debug.LogException(error, this); return false; }
        finally
        {
            try { if (!succeeded) CloseExitSession(session); }
            finally { exitOperation = false; }
        }
    }

    private bool ExitBattleAvailable() => isActiveAndEnabled && IsHUDInputReady && !exitClearing && boundExitView != null &&
        (exitOwnerAvailable == null || exitOwnerAvailable()) &&
        exitBattle != null && exitBattle.IsOpenForRead && exitMembers != null && !exitMembers.IsBusy &&
        exitMembers.CanStartOperation && !IsDraftSessionBusy && !IsTowerInspectionBusy &&
        (towerPlacementController == null || towerPlacementController.IsAvailableForInspection) &&
        exitPause != null && exitPause.IsBattleOpen(exitBattle);

    private bool IsExitSessionCurrent(ExitConfirmationSession session) => session != null &&
        ReferenceEquals(session, exitSession) && ReferenceEquals(session.Battle, exitBattle) &&
        ReferenceEquals(session.View, boundExitView) && ReferenceEquals(boundExitView, exitStageConfirmWindow) &&
        session.Revision == exitBindingRevision &&
        exitPhase != ExitConfirmationPhase.Idle && exitPhase != ExitConfirmationPhase.Closing && ExitBattleAvailable();

    internal bool IsCurrentExitStageRequest(ExitStageTransitionRequest request) => request != null &&
        ReferenceEquals(request.Source, this) && exitPhase == ExitConfirmationPhase.AwaitingTransition &&
        IsExitSessionCurrent(exitSession) && ReferenceEquals(request, exitSession.Request) &&
        ReferenceEquals(request.Session, exitSession) && ReferenceEquals(request.Battle, exitBattle) &&
        request.BindingRevision == exitBindingRevision && !request.Accepted &&
        (request.Action == ExitStageAction.Restart || request.Action == ExitStageAction.MainMenu) &&
        exitSession.Authority.Owns(exitSession.Pause);

    // Task005 calls this under its transition guard BEFORE revoking/releasing the old Battle.
    internal bool TryAcceptExitStageRequest(ExitStageTransitionRequest request)
    {
        if (!IsCurrentExitStageRequest(request)) return false;
        request.Accept();
        return true;
    }

    private void SubmitExitRequest(ExitConfirmationSession session, ExitStageAction action)
    {
        if (exitOperation || exitPhase != ExitConfirmationPhase.Open || !IsExitSessionCurrent(session) ||
            !session.Authority.Owns(session.Pause)) return;
        var request = new ExitStageTransitionRequest(this, session.Battle, session, session.Revision, action);
        var transition = exitTransition;
        session.Request = request;
        exitPhase = ExitConfirmationPhase.AwaitingTransition;
        session.View.SetInteractionEnabled(false);
        // No outer operation spans dispatch: a synchronous transition may bind a fresh Battle.
        try
        {
            var disposition = transition == null ? ExitStageRequestDisposition.Rejected : transition(request);
            if (request.Accepted)
            {
                CheckAcceptedExitRequest(session);
                return;
            }
            if (disposition != ExitStageRequestDisposition.Rejected)
            {
                FailExitRequest(session, "Exit request returned Accepted without acquiring transition ownership.");
                return;
            }
            RestoreRejectedExitRequest(session, request);
        }
        catch (Exception error)
        {
            Debug.LogException(error, this);
            if (request.Accepted) { CheckAcceptedExitRequest(session); return; }
            RestoreRejectedExitRequest(session, request);
        }
    }

    private void CheckAcceptedExitRequest(ExitConfirmationSession session)
    {
        // Only inspect captured outgoing identity. Never mutate a newly bound view/Battle.
        if (session.Battle.IsOpenForRead || session.Authority.IsBattleOpen(session.Battle))
            FailExitRequest(session, "Accepted exit request returned before revoking the outgoing Battle.");
    }

    private void RestoreRejectedExitRequest(ExitConfirmationSession session, ExitStageTransitionRequest request)
    {
        if (!ReferenceEquals(session, exitSession)) return;
        if (!IsCurrentExitStageRequest(request) || session.View == null || !session.View.IsVisible)
        { CloseExitSession(session); return; }
        session.Request = null;
        exitPhase = ExitConfirmationPhase.Open;
        session.View.SetInteractionEnabled(true);
    }

    private void FailExitRequest(ExitConfirmationSession session, string reason)
    {
        try { session.Battle.ReportPresentationFailure(reason); }
        finally { CloseExitSession(session); }
    }

    private void CancelExitSession(ExitConfirmationSession session)
    {
        if (exitPhase == ExitConfirmationPhase.Open && !exitOperation) CloseExitSession(session);
    }
    internal void CancelExitConfirmation() { CloseExitSession(exitSession); }

    internal void ClearExitConfirmationBinding()
    {
        if (exitClearing) return;
        exitClearing = true;
        bool outerOperation = exitOperation;
        exitOperation = true;
        var view = boundExitView;
        try
        {
            exitBindingRevision++;
            exitBattle = null;
            InvalidateBattlefieldInput();
            CloseExitSession(exitSession);
        }
        finally
        {
            exitMembers = null;
            exitPause = null;
            boundExitView = null;
            exitTransition = null;
            exitOwnerAvailable = exitOpeningAllowed = null;
            try { if (view != null) TryHudCleanup(view.Detach); }
            finally { exitOperation = outerOperation; exitClearing = false; }
        }
    }

    private void CloseExitSession(ExitConfirmationSession session)
    {
        if (session == null || !ReferenceEquals(session, exitSession) || exitPhase == ExitConfirmationPhase.Closing) return;
        exitPhase = ExitConfirmationPhase.Closing;
        exitSession = null;
        var pause = session.Pause;
        session.Pause = null;
        bool hidden = true;
        try
        {
            try { if (session.View != null) session.View.HideAndLock(); }
            catch (Exception error) { hidden = false; Debug.LogException(error, this); }
        }
        finally
        {
            try { pause?.Authority.Release(pause); }
            finally { exitPhase = ExitConfirmationPhase.Idle; }
        }
        if (!hidden) session.Battle.ReportPresentationFailure("Exit confirmation could not hide its modal surface.");
    }

    private void CheckExitConfirmationLifetime()
    {
        var session = exitSession;
        if (session == null || (exitPhase != ExitConfirmationPhase.Open &&
            exitPhase != ExitConfirmationPhase.AwaitingTransition)) return;
        if (!IsExitSessionCurrent(session) || session.View == null || !session.View.IsVisible ||
            !session.Authority.Owns(session.Pause) || !session.View.TryValidateReferences(out _)) CloseExitSession(session);
    }
}
