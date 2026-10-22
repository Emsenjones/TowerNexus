using System;
using UnityEngine;

public partial class GameFlowController
{
    private sealed class ExitTransitionWork
    {
        internal readonly StageCompositionController Stage;
        internal readonly StageDefinition SelectedStage;
        internal readonly ExitStageAction Action;
        internal bool ReleaseIssued, RestartStarted, Recovering;
        internal ExitTransitionWork(StageCompositionController stage, StageDefinition selectedStage, ExitStageAction action)
        { Stage = stage; SelectedStage = selectedStage; Action = action; }
    }

    private ExitTransitionWork exitTransitionWork;
    private bool exitTransitionRunning;

    internal bool OwnsBattleCoordinator(BattleRuntimeCoordinator source) => source != null &&
        ReferenceEquals(source, CurrentBattleRuntimeCoordinator);
    internal bool AllowsBattleHUDInput(BattleRuntimeCoordinator source) =>
        isActiveAndEnabled && OwnsBattleCoordinator(source) && !isTransitionInProgress && !cleanupRequested &&
        currentState == GameFlowState.Battle;
    internal bool AllowsBattleHUDRebind(BattleRuntimeCoordinator source) =>
        isActiveAndEnabled && OwnsBattleCoordinator(source) && !isTransitionInProgress && !cleanupRequested &&
        (currentState == GameFlowState.Battle || currentState == GameFlowState.StageIntroduction);

    internal bool RecoverPreparedHUD(BattleRuntimeCoordinator source, string reason)
    {
        if (!AllowsBattleHUDRebind(source) || currentState != GameFlowState.StageIntroduction ||
            stageCompositionController == null || stageCompositionController.HasBattleBegun) return false;
        Debug.LogError("Prepared Battle HUD became unavailable: " + reason, this);
        isTransitionInProgress = true;
        exitTransitionWork = new ExitTransitionWork(stageCompositionController, CurrentStage, ExitStageAction.MainMenu)
        { Recovering = true };
        ContinueExitStageTransition();
        return true;
    }

    internal ExitStageRequestDisposition RequestExitStageTransition(
        BattleRuntimeCoordinator source, ExitStageTransitionRequest request)
    {
        if (!CanAcceptExitTransition(source, request)) return ExitStageRequestDisposition.Rejected;
        isTransitionInProgress = true;
        try
        {
            // Revalidate under the guard. This acceptance helper performs no external callbacks.
            if (!IsCurrentExitTransitionSource(source, request) || !source.TryAcceptExitStageTransition(request))
                return ExitStageRequestDisposition.Rejected;
            exitTransitionWork = new ExitTransitionWork(stageCompositionController, CurrentStage, request.Action);
            ContinueExitStageTransition();
            return ExitStageRequestDisposition.Accepted;
        }
        finally
        {
            if (exitTransitionWork == null) isTransitionInProgress = false;
        }
    }

    private bool CanAcceptExitTransition(BattleRuntimeCoordinator source, ExitStageTransitionRequest request) =>
        isActiveAndEnabled && !isTransitionInProgress && !cleanupRequested && !isBeginningPreparedStage &&
        currentState == GameFlowState.Battle && !lastBattleResult.HasValue && !pendingBattleResult.HasValue &&
        IsCurrentExitTransitionSource(source, request);

    private bool IsCurrentExitTransitionSource(BattleRuntimeCoordinator source, ExitStageTransitionRequest request) =>
        source != null && ReferenceEquals(source, CurrentBattleRuntimeCoordinator) &&
        ReferenceEquals(source, subscribedBattleRuntimeCoordinator) && IsCurrentStageIndexValid() &&
        stageCompositionController != null && stageCompositionController.CanStartFlowTransition &&
        stageCompositionController.IsCompositionReady && stageCompositionController.HasBattleBegun &&
        stageCompositionController.ActiveStage == CurrentStage && source.CanAcceptExitStageRequest(request);

    private void Update() { ContinueExitStageTransition(); }

    private void MarkExitRecovery()
    {
        var work = exitTransitionWork;
        if (work == null || work.Recovering) return;
        work.Recovering = true;
        // Initial release may still be pending. Do not issue it again just to select Main Menu.
        // Once restart began, a separate partial incoming runtime may need retirement.
        if (work.RestartStarted) work.ReleaseIssued = false;
        pendingBattleResult = null;
    }

    private void ContinueExitStageTransition()
    {
        if (exitTransitionWork == null || exitTransitionRunning) return;
        exitTransitionRunning = true;
        bool completedRestart = false;
        try
        {
            // At most initial work plus one bounded recovery attempt per dispatch.
            for (int pass = 0; pass < 2 && exitTransitionWork != null; pass++)
            {
                var work = exitTransitionWork;
                try
                {
                    if (cleanupRequested || !isActiveAndEnabled) MarkExitRecovery();
                    if (!work.ReleaseIssued)
                    {
                        work.ReleaseIssued = true;
                        work.Stage.ReleaseStage();
                    }
                    // The release owner finishes deferred work. Update only observes retirement;
                    // it never reissues a pending release or prepares against an outgoing runtime.
                    if (!work.Stage.IsRuntimeRetired) break;

                    if (!work.Recovering && work.Action == ExitStageAction.Restart)
                    {
                        if (CurrentStage != work.SelectedStage || stageCompositionController != work.Stage)
                            throw new InvalidOperationException("Exit restart lost its selected Stage/composition identity.");
                        work.RestartStarted = true;
                        lastBattleResult = null;
                        pendingBattleResult = null;
                        bool prepared = PrepareCurrentStageCore();
                        if (!prepared || cleanupRequested || !isActiveAndEnabled) MarkExitRecovery();
                        if (work.Recovering) continue;
                        completedRestart = true;
                    }
                    else
                    {
                        ClearRunStateCore();
                        // Keep the guard throughout notification, including reentrant Start/disable.
                        PublishExitMainMenu();
                    }
                    cleanupRequested = false;
                    exitTransitionWork = null;
                    isTransitionInProgress = false;
                }
                catch (Exception error)
                {
                    Debug.LogException(error, this);
                    if (work.Recovering)
                    {
                        // A failed recovery is not a completed retirement. Keep the gate closed;
                        // external lifecycle cleanup may still complete it. Never spin on release.
                        break;
                    }
                    MarkExitRecovery();
                    work.ReleaseIssued = false;
                }
            }
        }
        finally { exitTransitionRunning = false; }

        // Preserve normal synchronous startup-result buffering for the newly begun Battle.
        if (completedRestart && exitTransitionWork == null && pendingBattleResult.HasValue)
        {
            var result = pendingBattleResult.Value;
            pendingBattleResult = null;
            TryAcceptBattleResult(result);
        }
    }

    private void PublishExitMainMenu()
    {
        currentState = GameFlowState.MainMenu;
        if (!isActiveAndEnabled) return;
        foreach (Delegate subscriber in OnStateChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            if (!isActiveAndEnabled) break;
            try { ((Action<GameFlowState>)subscriber)(GameFlowState.MainMenu); }
            catch (Exception error) { Debug.LogException(error, this); }
        }
    }
}
