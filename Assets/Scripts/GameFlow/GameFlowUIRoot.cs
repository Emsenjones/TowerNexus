using System;
using UnityEngine;
using UnityEngine.Serialization;

public class GameFlowUIRoot : MonoBehaviour
{
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private StageIntroductionView stageIntroductionView;
    [SerializeField] private StageResultWindowView stageResultWindowView;

    private bool isSubscribed;

    private void Awake()
    {
        TryEnterSafePresentationState();
    }

    private void OnEnable()
    {
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
            stageResultWindowView,
            "Stage Result Window View",
            view => view.TryValidateReferences(out string failureReason)
                ? null
                : failureReason);

        if (!AreViewObjectsDistinct())
        {
            Debug.LogError(
                "Game Flow UI Root requires three distinct fixed view objects.",
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
            stageResultWindowView == null)
        {
            return false;
        }

        GameObject mainMenuObject = mainMenuView.gameObject;
        GameObject introductionObject = stageIntroductionView.gameObject;
        GameObject resultObject = stageResultWindowView.gameObject;

        return mainMenuObject != introductionObject &&
               mainMenuObject != resultObject &&
               introductionObject != resultObject;
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
        stageResultWindowView.NextStageRequested +=
            HandleNextStageRequested;
        stageResultWindowView.RetryRequested += HandleRetryRequested;
        stageResultWindowView.ReturnToMainMenuRequested +=
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

        if (stageResultWindowView != null)
        {
            stageResultWindowView.NextStageRequested -=
                HandleNextStageRequested;
            stageResultWindowView.RetryRequested -= HandleRetryRequested;
            stageResultWindowView.ReturnToMainMenuRequested -=
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
                stageResultWindowView.ShowVictory(
                    gameFlowController.HasNextStage);
                break;
            case GameFlowState.StageDefeat:
                stageResultWindowView.ShowDefeat();
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
             state != GameFlowState.StageDefeat &&
             stageResultWindowView.IsInteractionEnabled))
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
            () => stageResultWindowView?.HideAndLock(),
            "Stage Result Window View");
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

    private int CountActiveViews()
    {
        int activeViewCount = 0;

        if (mainMenuView != null && mainMenuView.gameObject.activeSelf)
        {
            activeViewCount++;
        }

        if (stageIntroductionView != null &&
            stageIntroductionView.IsVisible)
        {
            activeViewCount++;
        }

        if (stageResultWindowView != null &&
            stageResultWindowView.IsVisible)
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

    private void HandleNextStageRequested()
    {
        ForwardIntent(
            gameFlowController.RequestContinueAfterVictory,
            () => stageResultWindowView.SetInteractionEnabled(false),
            "continue to the next Stage after Victory");
    }

    private void HandleRetryRequested()
    {
        ForwardIntent(
            gameFlowController.RequestRetryCurrentStage,
            () => stageResultWindowView.SetInteractionEnabled(false),
            "retry the current Stage");
    }

    private void HandleReturnToMainMenuRequested()
    {
        ForwardIntent(
            gameFlowController.RequestReturnToMainMenu,
            () => stageResultWindowView.SetInteractionEnabled(false),
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
