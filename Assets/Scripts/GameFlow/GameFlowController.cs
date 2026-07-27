using System;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    private const int InactiveStageIndex = -1;
    private const int DemoStageTargetCount = 5;

    [SerializeField] private List<StageDefinition> demoStages =
        new List<StageDefinition>();
    [SerializeField] private StageCompositionController stageCompositionController;

    private int currentStageIndex = InactiveStageIndex;
    private GameFlowState currentState = GameFlowState.MainMenu;
    private BattleResult? lastBattleResult;
    private BattleResult? pendingBattleResult;
    private bool isTransitionInProgress;
    private bool isBeginningPreparedStage;
    private bool cleanupRequested;
    private BattleRuntimeCoordinator subscribedBattleRuntimeCoordinator;

    public GameFlowState CurrentState => currentState;
    public int CurrentStageIndex => currentStageIndex;
    public StageDefinition CurrentStage =>
        IsCurrentStageIndexValid() ? demoStages[currentStageIndex] : null;
    public bool HasNextStage =>
        IsCurrentStageIndexValid() &&
        currentStageIndex < demoStages.Count - 1;
    public BattleResult? LastBattleResult => lastBattleResult;

    private BattleRuntimeCoordinator CurrentBattleRuntimeCoordinator =>
        stageCompositionController == null
            ? null
            : stageCompositionController.BattleRuntimeCoordinator;

    public event Action<GameFlowState> OnStateChanged;

    private void OnEnable()
    {
        cleanupRequested = false;
        SubscribeToBattleResults();
        isTransitionInProgress = true;

        try
        {
            stageCompositionController?.ReleaseStage();
            ClearRunStateCore();
            PublishState(GameFlowState.MainMenu, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            cleanupRequested = true;
        }
        finally
        {
            isTransitionInProgress = false;
        }

        if (cleanupRequested)
        {
            PerformSilentCleanupCore();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromBattleResults();
        cleanupRequested = true;
        stageCompositionController?.ReleaseStage();

        if (!isTransitionInProgress)
        {
            PerformSilentCleanupCore();
        }
    }

    public bool RequestStartNewRun()
    {
        if (!CanAcceptIntent(
                GameFlowState.MainMenu,
                "start a new run"))
        {
            return false;
        }

        if (!ValidateStableReferences() ||
            !ValidateDemoStageSequence())
        {
            return false;
        }

        return ExecuteGuardedTransition(StartNewRunCore);
    }

    public bool RequestConfirmStageIntroduction()
    {
        if (!CanAcceptIntent(
                GameFlowState.StageIntroduction,
                "confirm the Stage Introduction"))
        {
            return false;
        }

        if (!ValidateStableReferences())
        {
            return false;
        }

        return ExecuteGuardedTransition(BeginPreparedStageCore);
    }

    public bool RequestContinueAfterVictory()
    {
        if (!CanAcceptIntent(
                GameFlowState.StageVictory,
                "continue after Victory"))
        {
            return false;
        }

        if (!ValidateStableReferences())
        {
            return false;
        }

        return ExecuteGuardedTransition(ContinueAfterVictoryCore);
    }

    public bool RequestRetryCurrentStage()
    {
        if (!CanAcceptIntent(
                GameFlowState.StageDefeat,
                "retry the current Stage"))
        {
            return false;
        }

        if (!ValidateStableReferences())
        {
            return false;
        }

        return ExecuteGuardedTransition(RetryCurrentStageCore);
    }

    public bool RequestReturnToMainMenu()
    {
        if (isTransitionInProgress)
        {
            LogRejectedIntent(
                "return to Main Menu",
                "another Game Flow transition is in progress");
            return false;
        }

        if (currentState != GameFlowState.StageVictory &&
            currentState != GameFlowState.StageDefeat)
        {
            LogRejectedIntent(
                "return to Main Menu",
                $"the current state is {currentState}");
            return false;
        }

        if (!ValidateStableReferences())
        {
            return false;
        }

        return ExecuteGuardedTransition(EnterMainMenuCore);
    }

    private bool StartNewRunCore()
    {
        stageCompositionController.ReleaseStage();

        if (!CanContinueTransition())
        {
            return false;
        }

        currentStageIndex = 0;
        lastBattleResult = null;
        pendingBattleResult = null;
        return PrepareCurrentStageCore();
    }

    private bool ContinueAfterVictoryCore()
    {
        if (!IsCurrentStageIndexValid())
        {
            return FailTransitionToMainMenu(
                "Game Flow cannot continue because the current Stage index is invalid.");
        }

        stageCompositionController.ReleaseStage();

        if (!CanContinueTransition())
        {
            return false;
        }

        if (!HasNextStage)
        {
            ClearRunStateCore();
            PublishState(GameFlowState.MainMenu);
            return true;
        }

        currentStageIndex++;
        lastBattleResult = null;
        pendingBattleResult = null;
        return PrepareCurrentStageCore();
    }

    private bool RetryCurrentStageCore()
    {
        if (!IsCurrentStageIndexValid())
        {
            return FailTransitionToMainMenu(
                "Game Flow cannot retry because the current Stage index is invalid.");
        }

        stageCompositionController.ReleaseStage();

        if (!CanContinueTransition())
        {
            return false;
        }

        lastBattleResult = null;
        pendingBattleResult = null;
        return PrepareCurrentStageCore();
    }

    private bool PrepareCurrentStageCore()
    {
        StageDefinition selectedStage = CurrentStage;

        if (selectedStage == null)
        {
            return FailTransitionToMainMenu(
                "Game Flow cannot prepare because the current Stage is invalid.");
        }

        lastBattleResult = null;
        pendingBattleResult = null;
        PublishState(GameFlowState.StagePreparing);

        if (!CanContinueTransition())
        {
            return false;
        }

        if (!stageCompositionController.TryPrepareStage(selectedStage))
        {
            return FailTransitionToMainMenu(
                $"Game Flow failed to prepare Stage '{GetStageName(selectedStage)}'.");
        }

        if (!CanContinueTransition())
        {
            return false;
        }

        if (!IsPreparedCurrentStage(selectedStage))
        {
            return FailTransitionToMainMenu(
                $"Game Flow received an invalid prepared runtime for Stage " +
                $"'{GetStageName(selectedStage)}'.");
        }

        if (ShouldShowStageIntroduction(selectedStage))
        {
            PublishState(GameFlowState.StageIntroduction);
            return true;
        }

        return BeginPreparedStageCore();
    }

    private bool BeginPreparedStageCore()
    {
        StageDefinition selectedStage = CurrentStage;

        if (selectedStage == null ||
            !IsPreparedCurrentStage(selectedStage))
        {
            return FailTransitionToMainMenu(
                "Game Flow cannot begin because the current Stage is not prepared.");
        }

        pendingBattleResult = null;
        bool beginSucceeded;
        isBeginningPreparedStage = true;

        try
        {
            beginSucceeded =
                stageCompositionController.TryBeginPreparedStage();
        }
        finally
        {
            isBeginningPreparedStage = false;
        }

        if (!beginSucceeded)
        {
            pendingBattleResult = null;
            return FailTransitionToMainMenu(
                $"Game Flow failed to begin Stage '{GetStageName(selectedStage)}'.");
        }

        if (!CanContinueTransition())
        {
            return false;
        }

        if (!IsBegunCurrentStage(selectedStage))
        {
            pendingBattleResult = null;
            return FailTransitionToMainMenu(
                $"Game Flow received an invalid begun runtime for Stage " +
                $"'{GetStageName(selectedStage)}'.");
        }

        PublishState(GameFlowState.Battle);
        return true;
    }

    private bool EnterMainMenuCore()
    {
        stageCompositionController.ReleaseStage();

        if (!CanContinueTransition())
        {
            return false;
        }

        ClearRunStateCore();
        PublishState(GameFlowState.MainMenu);
        return true;
    }

    private bool ExecuteGuardedTransition(Func<bool> transition)
    {
        if (isTransitionInProgress)
        {
            return false;
        }

        isTransitionInProgress = true;
        bool transitionSucceeded = false;

        try
        {
            transitionSucceeded = transition();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            cleanupRequested = true;
        }
        finally
        {
            isTransitionInProgress = false;
        }

        if (cleanupRequested)
        {
            pendingBattleResult = null;
            PerformSilentCleanupCore();
            return false;
        }

        if (!transitionSucceeded)
        {
            pendingBattleResult = null;
            return false;
        }

        if (!pendingBattleResult.HasValue)
        {
            return true;
        }

        BattleResult bufferedResult = pendingBattleResult.Value;
        pendingBattleResult = null;
        return TryAcceptBattleResult(bufferedResult);
    }

    private void HandleBattleResultPublished(BattleResult result)
    {
        if (cleanupRequested)
        {
            return;
        }

        if (isTransitionInProgress)
        {
            if ((isBeginningPreparedStage ||
                 currentState == GameFlowState.Battle) &&
                CanBufferSynchronousBattleResult())
            {
                if (!pendingBattleResult.HasValue)
                {
                    pendingBattleResult = result;
                }
                else
                {
                    Debug.LogWarning(
                        "Game Flow ignored a duplicate synchronous Battle result.",
                        this);
                }
            }
            else
            {
                Debug.LogWarning(
                    "Game Flow ignored a Battle result published during an " +
                    "unrelated transition.",
                    this);
            }

            return;
        }

        TryAcceptBattleResult(result);
    }

    private bool TryAcceptBattleResult(BattleResult result)
    {
        if (currentState != GameFlowState.Battle)
        {
            Debug.LogWarning(
                $"Game Flow ignored a {result} result while in {currentState}.",
                this);
            return false;
        }

        if (lastBattleResult.HasValue)
        {
            Debug.LogWarning(
                $"Game Flow ignored duplicate Battle result {result}.",
                this);
            return false;
        }

        if (!IsSupportedBattleResult(result))
        {
            Debug.LogError(
                $"Game Flow received unsupported Battle result value {(int)result}.",
                this);
            return false;
        }

        if (!IsCurrentBattleResultSourceValid())
        {
            Debug.LogWarning(
                $"Game Flow ignored {result} because it does not belong to the " +
                "current composed Battle.",
                this);
            return false;
        }

        return ExecuteGuardedTransition(
            () =>
            {
                lastBattleResult = result;
                PublishState(
                    result == BattleResult.Victory
                        ? GameFlowState.StageVictory
                        : GameFlowState.StageDefeat);
                return true;
            });
    }

    private bool CanBufferSynchronousBattleResult()
    {
        return (currentState == GameFlowState.StagePreparing ||
                currentState == GameFlowState.StageIntroduction ||
                currentState == GameFlowState.Battle) &&
               !lastBattleResult.HasValue &&
               IsCurrentBattleResultSourceValid();
    }

    private bool IsCurrentBattleResultSourceValid()
    {
        BattleRuntimeCoordinator coordinator = CurrentBattleRuntimeCoordinator;

        return coordinator != null &&
               subscribedBattleRuntimeCoordinator == coordinator &&
               IsCurrentStageIndexValid() &&
               stageCompositionController.IsCompositionReady &&
               stageCompositionController.ActiveStage == CurrentStage &&
               stageCompositionController.HasBattleBegun &&
               !coordinator.IsBattleActive;
    }

    private bool IsPreparedCurrentStage(StageDefinition selectedStage)
    {
        BattleRuntimeCoordinator coordinator = CurrentBattleRuntimeCoordinator;

        return coordinator != null &&
               stageCompositionController.IsCompositionReady &&
               stageCompositionController.ActiveStage == selectedStage &&
               !stageCompositionController.HasBattleBegun &&
               coordinator.IsBattlePrepared &&
               !coordinator.IsBattleActive;
    }

    private bool IsBegunCurrentStage(StageDefinition selectedStage)
    {
        BattleRuntimeCoordinator coordinator = CurrentBattleRuntimeCoordinator;

        return coordinator != null &&
               stageCompositionController.IsCompositionReady &&
               stageCompositionController.ActiveStage == selectedStage &&
               stageCompositionController.HasBattleBegun &&
               !coordinator.IsBattlePrepared &&
               (coordinator.IsBattleActive ||
                pendingBattleResult.HasValue);
    }

    private bool ValidateStableReferences()
    {
        if (stageCompositionController == null)
        {
            Debug.LogError(
                "Game Flow requires a Stage Composition Controller.",
                this);
            return false;
        }

        if (CurrentBattleRuntimeCoordinator == null)
        {
            Debug.LogError(
                "Game Flow requires Stage Composition Controller to have a " +
                "Battle Runtime Coordinator.",
                this);
            return false;
        }

        return true;
    }

    private bool ValidateDemoStageSequence()
    {
        if (demoStages == null || demoStages.Count == 0)
        {
            Debug.LogError(
                "Game Flow requires a non-empty ordered Demo Stage list.",
                this);
            return false;
        }

        HashSet<StageDefinition> uniqueStages =
            new HashSet<StageDefinition>();

        for (int i = 0; i < demoStages.Count; i++)
        {
            StageDefinition stageDefinition = demoStages[i];

            if (stageDefinition == null)
            {
                Debug.LogError(
                    $"Game Flow Demo Stage entry {i} is null.",
                    this);
                return false;
            }

            if (!uniqueStages.Add(stageDefinition))
            {
                Debug.LogError(
                    $"Game Flow Demo Stage entry {i} duplicates Stage " +
                    $"'{GetStageName(stageDefinition)}'.",
                    this);
                return false;
            }
        }

        if (demoStages.Count < DemoStageTargetCount)
        {
            Debug.LogWarning(
                $"Game Flow Demo Stage list contains {demoStages.Count} entries; " +
                $"the current Demo target is at least {DemoStageTargetCount}.",
                this);
        }

        return true;
    }

    private bool CanAcceptIntent(
        GameFlowState requiredState,
        string intentDescription)
    {
        if (!isActiveAndEnabled)
        {
            LogRejectedIntent(
                intentDescription,
                "Game Flow Controller is disabled");
            return false;
        }

        if (isTransitionInProgress)
        {
            LogRejectedIntent(
                intentDescription,
                "another Game Flow transition is in progress");
            return false;
        }

        if (currentState != requiredState)
        {
            LogRejectedIntent(
                intentDescription,
                $"the current state is {currentState}");
            return false;
        }

        return true;
    }

    private bool CanContinueTransition()
    {
        return !cleanupRequested && isActiveAndEnabled;
    }

    private void PublishState(
        GameFlowState state,
        bool forceNotification = false)
    {
        bool changed = currentState != state;
        currentState = state;

        if (changed || forceNotification)
        {
            OnStateChanged?.Invoke(currentState);
        }
    }

    private void ClearRunStateCore()
    {
        currentStageIndex = InactiveStageIndex;
        lastBattleResult = null;
        pendingBattleResult = null;
        isBeginningPreparedStage = false;
    }

    private void PerformSilentCleanupCore()
    {
        pendingBattleResult = null;
        isBeginningPreparedStage = false;
        stageCompositionController?.ReleaseStage();
        ClearRunStateCore();
        currentState = GameFlowState.MainMenu;
        cleanupRequested = false;
    }

    private bool FailTransitionToMainMenu(string failureReason)
    {
        Debug.LogError(failureReason, this);
        EnterMainMenuCore();
        return false;
    }

    private bool IsCurrentStageIndexValid()
    {
        return demoStages != null &&
               currentStageIndex >= 0 &&
               currentStageIndex < demoStages.Count;
    }

    private static bool ShouldShowStageIntroduction(
        StageDefinition stageDefinition)
    {
        if (!stageDefinition.ShowStageIntroduction)
        {
            return false;
        }

        return HasPresentableReference(
                   stageDefinition.IntroducedTowers,
                   stageDefinition.TowerDraftPool) ||
               HasPresentableReference(
                   stageDefinition.IntroducedTowerUpgrades,
                   stageDefinition.TowerUpgradeDraftPool);
    }

    private static bool HasPresentableReference<T>(
        IReadOnlyList<T> introducedEntries,
        IReadOnlyList<T> matchingPool)
        where T : UnityEngine.Object
    {
        if (introducedEntries == null || matchingPool == null)
        {
            return false;
        }

        for (int introducedIndex = 0;
             introducedIndex < introducedEntries.Count;
             introducedIndex++)
        {
            T introducedEntry = introducedEntries[introducedIndex];

            if (introducedEntry == null)
            {
                continue;
            }

            for (int poolIndex = 0;
                 poolIndex < matchingPool.Count;
                 poolIndex++)
            {
                if (introducedEntry == matchingPool[poolIndex])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SubscribeToBattleResults()
    {
        UnsubscribeFromBattleResults();
        BattleRuntimeCoordinator coordinator = CurrentBattleRuntimeCoordinator;

        if (coordinator == null)
        {
            return;
        }

        coordinator.OnBattleResultPublished +=
            HandleBattleResultPublished;
        subscribedBattleRuntimeCoordinator = coordinator;
    }

    private void UnsubscribeFromBattleResults()
    {
        if (subscribedBattleRuntimeCoordinator == null)
        {
            return;
        }

        subscribedBattleRuntimeCoordinator.OnBattleResultPublished -=
            HandleBattleResultPublished;
        subscribedBattleRuntimeCoordinator = null;
    }

    private void LogRejectedIntent(
        string intentDescription,
        string rejectionReason)
    {
        Debug.LogWarning(
            $"Game Flow rejected intent to {intentDescription}: {rejectionReason}.",
            this);
    }

    private static bool IsSupportedBattleResult(BattleResult result)
    {
        return result == BattleResult.Victory ||
               result == BattleResult.Defeat;
    }

    private static string GetStageName(StageDefinition stageDefinition)
    {
        if (stageDefinition == null)
        {
            return "<null>";
        }

        return string.IsNullOrEmpty(stageDefinition.DisplayName)
            ? stageDefinition.name
            : stageDefinition.DisplayName;
    }
}
