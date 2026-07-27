using System;
using UnityEngine;

public class GameFlowUIRoot : MonoBehaviour
{
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private StageIntroductionView stageIntroductionView;
    [SerializeField] private StageVictoryWindowView stageVictoryWindowView;
    [SerializeField] private StageDefeatWindowView stageDefeatWindowView;

    private bool isSubscribed;

    private void OnEnable()
    {
        ReportAuthoredActivationIssues();

        if (!TryEnterSafePresentationState() ||
            !TryValidateComposition())
        {
            return;
        }

        Subscribe();
        ReconcileSafely(gameFlowController.CurrentState);
    }

    private void OnDisable()
    {
        Unsubscribe();
        TryEnterSafePresentationState();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        TryEnterSafePresentationState();
    }

    private bool TryValidateComposition()
    {
        bool isValid = true;

        if (gameFlowController == null)
        {
            Debug.LogError(
                "Game Flow UI Root is missing its GameFlowController reference.",
                this);
            isValid = false;
        }

        isValid &= TryValidateView(
            mainMenuView,
            "Main Menu View",
            view => view.TryValidateReferences(out string failureReason)
                ? null
                : failureReason);
        isValid &= TryValidateView(
            stageIntroductionView,
            "Stage Introduction View",
            view => view.TryValidateReferences(out string failureReason)
                ? null
                : failureReason);
        isValid &= TryValidateView(
            stageVictoryWindowView,
            "Stage Victory Window View",
            view => view.TryValidateReferences(out string failureReason)
                ? null
                : failureReason);
        isValid &= TryValidateView(
            stageDefeatWindowView,
            "Stage Defeat Window View",
            view => view.TryValidateReferences(out string failureReason)
                ? null
                : failureReason);

        if (!AreViewObjectsDistinct())
        {
            Debug.LogError(
                "Game Flow UI Root requires four distinct fixed view objects.",
                this);
            isValid = false;
        }

        if (!isValid)
        {
            Debug.LogError(
                "Game Flow UI Root validation failed. All Game Flow views " +
                "remain hidden and locked, and no flow subscriptions were created.",
                this);
        }

        return isValid;
    }

    private bool TryValidateView<T>(
        T view,
        string displayName,
        Func<T, string> getFailureReason)
        where T : Component
    {
        if (view == null)
        {
            Debug.LogError(
                $"Game Flow UI Root is missing its {displayName} reference.",
                this);
            return false;
        }

        if (!view.transform.IsChildOf(transform))
        {
            Debug.LogError(
                $"{displayName} must be an authored child of Game Flow UI Root.",
                view);
            return false;
        }

        string failureReason = getFailureReason(view);

        if (failureReason == null)
        {
            return true;
        }

        Debug.LogError(
            $"{displayName} validation failed: {failureReason}",
            view);
        return false;
    }

    private bool AreViewObjectsDistinct()
    {
        if (mainMenuView == null ||
            stageIntroductionView == null ||
            stageVictoryWindowView == null ||
            stageDefeatWindowView == null)
        {
            return false;
        }

        GameObject mainMenuObject = mainMenuView.gameObject;
        GameObject introductionObject = stageIntroductionView.gameObject;
        GameObject victoryObject = stageVictoryWindowView.gameObject;
        GameObject defeatObject = stageDefeatWindowView.gameObject;

        return mainMenuObject != introductionObject &&
               mainMenuObject != victoryObject &&
               mainMenuObject != defeatObject &&
               introductionObject != victoryObject &&
               introductionObject != defeatObject &&
               victoryObject != defeatObject;
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        gameFlowController.OnStateChanged += HandleStateChanged;
        mainMenuView.StartRequested += HandleStartRequested;
        stageIntroductionView.ConfirmRequested +=
            HandleIntroductionConfirmRequested;
        stageVictoryWindowView.ContinueRequested +=
            HandleContinueRequested;
        stageDefeatWindowView.RetryRequested += HandleRetryRequested;
        stageDefeatWindowView.ReturnToMainMenuRequested +=
            HandleReturnToMainMenuRequested;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (gameFlowController != null)
        {
            gameFlowController.OnStateChanged -= HandleStateChanged;
        }

        if (mainMenuView != null)
        {
            mainMenuView.StartRequested -= HandleStartRequested;
        }

        if (stageIntroductionView != null)
        {
            stageIntroductionView.ConfirmRequested -=
                HandleIntroductionConfirmRequested;
        }

        if (stageVictoryWindowView != null)
        {
            stageVictoryWindowView.ContinueRequested -=
                HandleContinueRequested;
        }

        if (stageDefeatWindowView != null)
        {
            stageDefeatWindowView.RetryRequested -= HandleRetryRequested;
            stageDefeatWindowView.ReturnToMainMenuRequested -=
                HandleReturnToMainMenuRequested;
        }

        isSubscribed = false;
    }

    private void HandleStateChanged(GameFlowState state)
    {
        ReconcileSafely(state);
    }

    private void ReconcileSafely(GameFlowState state)
    {
        try
        {
            if (TryApplyPresentationState(state))
            {
                return;
            }

            Debug.LogError(
                $"Game Flow UI failed to reconcile state {state}. " +
                "All views will remain hidden and locked.",
                this);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Game Flow UI threw while reconciling state {state}. " +
                "The presentation failure was isolated from Game Flow runtime.",
                this);
            Debug.LogException(exception, this);
        }

        TryEnterSafePresentationState();
    }

    private bool TryApplyPresentationState(GameFlowState state)
    {
        if (!TryEnterSafePresentationState())
        {
            return false;
        }

        switch (state)
        {
            case GameFlowState.MainMenu:
                mainMenuView.Show();
                break;
            case GameFlowState.StagePreparing:
            case GameFlowState.Battle:
                break;
            case GameFlowState.StageIntroduction:
                if (!stageIntroductionView.TryConfigure(
                        gameFlowController.CurrentStage))
                {
                    return false;
                }

                stageIntroductionView.Show();
                break;
            case GameFlowState.StageVictory:
                stageVictoryWindowView.Show(
                    gameFlowController.HasNextStage);
                break;
            case GameFlowState.StageDefeat:
                stageDefeatWindowView.Show();
                break;
            default:
                Debug.LogError(
                    $"Game Flow UI cannot present unsupported state " +
                    $"{(int)state}.",
                    this);
                return false;
        }

        return ValidateAppliedPresentation(state);
    }

    private bool ValidateAppliedPresentation(GameFlowState state)
    {
        int activeViewCount = CountActiveViews();
        int expectedActiveViewCount =
            state == GameFlowState.StagePreparing ||
            state == GameFlowState.Battle
                ? 0
                : 1;

        if (activeViewCount != expectedActiveViewCount)
        {
            Debug.LogError(
                $"Game Flow UI state {state} has {activeViewCount} active " +
                $"fixed views; expected {expectedActiveViewCount}.",
                this);
            return false;
        }

        if ((state != GameFlowState.MainMenu &&
             mainMenuView.IsInteractionEnabled) ||
            (state != GameFlowState.StageIntroduction &&
             stageIntroductionView.IsInteractionEnabled) ||
            (state != GameFlowState.StageVictory &&
             stageVictoryWindowView.IsInteractionEnabled) ||
            (state != GameFlowState.StageDefeat &&
             stageDefeatWindowView.IsInteractionEnabled))
        {
            Debug.LogError(
                $"Game Flow UI state {state} left a hidden view interactive.",
                this);
            return false;
        }

        return true;
    }

    private bool TryEnterSafePresentationState()
    {
        bool succeeded = true;
        succeeded &= TryHideView(
            () => mainMenuView?.HideAndLock(),
            "Main Menu View");
        succeeded &= TryHideView(
            () => stageIntroductionView?.HideAndClear(),
            "Stage Introduction View");
        succeeded &= TryHideView(
            () => stageVictoryWindowView?.HideAndLock(),
            "Stage Victory Window View");
        succeeded &= TryHideView(
            () => stageDefeatWindowView?.HideAndLock(),
            "Stage Defeat Window View");
        return succeeded;
    }

    private bool TryHideView(Action hideView, string displayName)
    {
        try
        {
            hideView();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Game Flow UI could not hide and lock {displayName}.",
                this);
            Debug.LogException(exception, this);
            return false;
        }
    }

    private void ReportAuthoredActivationIssues()
    {
        int activeViewCount = CountActiveViews();

        if (activeViewCount > 1)
        {
            Debug.LogError(
                $"Game Flow UI Root found {activeViewCount} authored active " +
                "fixed views. Fixed child views should be inactive by default.",
                this);
        }
        else if (activeViewCount == 1)
        {
            Debug.LogWarning(
                "Game Flow UI Root found an authored active fixed view. " +
                "Fixed child views should be inactive by default so runtime " +
                "state is the only visibility authority.",
                this);
        }
    }

    private int CountActiveViews()
    {
        int activeViewCount = 0;

        if (mainMenuView != null && mainMenuView.gameObject.activeSelf)
        {
            activeViewCount++;
        }

        if (stageIntroductionView != null &&
            stageIntroductionView.gameObject.activeSelf)
        {
            activeViewCount++;
        }

        if (stageVictoryWindowView != null &&
            stageVictoryWindowView.gameObject.activeSelf)
        {
            activeViewCount++;
        }

        if (stageDefeatWindowView != null &&
            stageDefeatWindowView.gameObject.activeSelf)
        {
            activeViewCount++;
        }

        return activeViewCount;
    }

    private void HandleStartRequested()
    {
        ForwardIntent(
            gameFlowController.RequestStartNewRun,
            () => mainMenuView.SetInteractionEnabled(false),
            "start a new run");
    }

    private void HandleIntroductionConfirmRequested()
    {
        ForwardIntent(
            gameFlowController.RequestConfirmStageIntroduction,
            () => stageIntroductionView.SetInteractionEnabled(false),
            "confirm the Stage Introduction");
    }

    private void HandleContinueRequested()
    {
        ForwardIntent(
            gameFlowController.RequestContinueAfterVictory,
            () => stageVictoryWindowView.SetInteractionEnabled(false),
            "continue after Victory");
    }

    private void HandleRetryRequested()
    {
        ForwardIntent(
            gameFlowController.RequestRetryCurrentStage,
            () => stageDefeatWindowView.SetInteractionEnabled(false),
            "retry the current Stage");
    }

    private void HandleReturnToMainMenuRequested()
    {
        ForwardIntent(
            gameFlowController.RequestReturnToMainMenu,
            () => stageDefeatWindowView.SetInteractionEnabled(false),
            "return to Main Menu");
    }

    private void ForwardIntent(
        Func<bool> requestIntent,
        Action lockSourceView,
        string intentDescription)
    {
        if (!isSubscribed)
        {
            return;
        }

        try
        {
            if (requestIntent())
            {
                lockSourceView();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Game Flow UI failed while forwarding intent to " +
                $"{intentDescription}. All views will be hidden and locked.",
                this);
            Debug.LogException(exception, this);
            TryEnterSafePresentationState();
        }
    }
}
