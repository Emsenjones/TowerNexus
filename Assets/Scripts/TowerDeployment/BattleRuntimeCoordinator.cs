using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleRuntimeCoordinator : MonoBehaviour
{
    private enum BattleTerminalState
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
        TechnicalFailure = 3
    }

    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private DraftSystem draftSystem;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private TowerUpgradeSystem towerUpgradeSystem;

    private bool hasFreshPlayerState;
    private bool isBattlePrepared;
    private bool hasNormalSpawningCompleted;
    private BattleTerminalState battleTerminalState;
    private DraftAttemptToken expectedInitialDraftToken;
    private bool hasInitialDraftAuthorizedSpawning;
    private bool isStartingSpawner;
    private string pendingSynchronousSpawningFailureReason;
    private int preparedPlayerMaxHealth;
    private readonly List<int> preparedPlayerProgressRequirements =
        new List<int>();
    private bool isLifecycleOperationInProgress;
    private bool releaseRequested;
    public bool IsBattleActive { get; private set; }
    public bool IsBattlePrepared => isBattlePrepared;

    public event Action<BattleResult> OnBattleResultPublished;
    public event Action<string> OnBattleRuntimeFailed;

    internal event Action OnBeforeBattleCleanup;
    internal event Action<BattleResult> OnBattleResultEvidence;
    internal event Action<string> OnBattleFailureEvidence;

    internal void FailCommittedUpgrade(TowerUpgradeSystem owner, string reason)
    {
        if (owner == towerUpgradeSystem) TryFailBattleRuntime(reason);
    }

    private void PublishSafely<T>(Action<T> handlers, T value)
    {
        if (handlers == null) return;
        foreach (Delegate subscriber in handlers.GetInvocationList())
        {
            try { ((Action<T>)subscriber)(value); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }

    private void CaptureBeforeCleanup()
    {
        towerUpgradeSystem?.FlushCommittedInvestment();
        if (OnBeforeBattleCleanup == null) return;
        foreach (Delegate subscriber in OnBeforeBattleCleanup.GetInvocationList())
        {
            try { ((Action)subscriber)(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }

    private void OnEnable()
    {
        towerUpgradeSystem?.BindUpgradeRuntime(this, towerPlacementController);
        if (playerSystem != null)
        {
            playerSystem.OnPlayerDefeated += HandlePlayerDefeated;
        }

        if (monsterSpawner != null)
        {
            monsterSpawner.OnAllSpawningCompleted +=
                HandleAllSpawningCompleted;
            monsterSpawner.OnSpawningFailed += HandleSpawningFailed;
        }

        if (monsterManager != null)
        {
            monsterManager.OnMonsterResolutionCompleted +=
                HandleMonsterResolutionCompleted;
        }

        if (draftSystem != null)
        {
            draftSystem.OnInitialDraftCompleted +=
                HandleInitialDraftCompleted;
            draftSystem.OnInitialDraftFailed +=
                HandleInitialDraftFailed;
        }
    }

    private void OnDisable()
    {
        if (playerSystem != null)
        {
            playerSystem.OnPlayerDefeated -= HandlePlayerDefeated;
        }

        if (monsterSpawner != null)
        {
            monsterSpawner.OnAllSpawningCompleted -=
                HandleAllSpawningCompleted;
            monsterSpawner.OnSpawningFailed -= HandleSpawningFailed;
        }

        if (monsterManager != null)
        {
            monsterManager.OnMonsterResolutionCompleted -=
                HandleMonsterResolutionCompleted;
        }

        if (draftSystem != null)
        {
            draftSystem.OnInitialDraftCompleted -=
                HandleInitialDraftCompleted;
            draftSystem.OnInitialDraftFailed -=
                HandleInitialDraftFailed;
        }

        ReleasePreparedBattleRuntime();
    }

    public bool TryPrepareBattleRuntime(
        MapGeneratorBehaviour activeMap,
        MonsterWaveConfig waveConfig,
        int playerMaxHealth,
        IReadOnlyList<int> playerProgressRequirements,
        IReadOnlyList<TowerDefinition> towerPool,
        IReadOnlyList<TowerUpgradeDefinition> upgradePool,
        float towerDraftSlotProbability)
    {
        if (towerUpgradeSystem != null && towerUpgradeSystem.IsApplyingUpgrade) return false;
        towerUpgradeSystem?.BindUpgradeRuntime(this, towerPlacementController);
        if (!TryValidatePreparationReferences(out string failureReason))
        {
            Debug.LogError(
                $"Battle runtime coordinator cannot prepare Stage runtime: " +
                failureReason,
                this);
            return false;
        }

        if (IsBattleActive)
        {
            Debug.LogError(
                "Battle runtime coordinator cannot prepare a Stage while a battle is active.",
                this);
            return false;
        }

        if (activeMap == null ||
            waveConfig == null ||
            playerMaxHealth <= 0 ||
            playerProgressRequirements == null ||
            playerProgressRequirements.Count == 0 ||
            towerPool == null ||
            upgradePool == null ||
            float.IsNaN(towerDraftSlotProbability) ||
            float.IsInfinity(towerDraftSlotProbability) ||
            towerDraftSlotProbability < 0f ||
            towerDraftSlotProbability > 1f)
        {
            Debug.LogError(
                "Battle runtime coordinator cannot prepare Stage runtime because " +
                "one or more Stage dependency slices are missing or invalid.",
                this);
            return false;
        }

        isLifecycleOperationInProgress = true;
        bool preparationSucceeded = false;

        try
        {
            preparationSucceeded = TryPrepareBattleRuntimeCore(
                activeMap,
                waveConfig,
                playerMaxHealth,
                playerProgressRequirements,
                towerPool,
                upgradePool,
                towerDraftSlotProbability);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            preparationSucceeded = FinalizeLifecycleOperation(
                preparationSucceeded);
        }

        return preparationSucceeded;
    }

    public bool TryValidatePreparationReferences(out string failureReason)
    {
        if (!isActiveAndEnabled)
        {
            failureReason = "Battle Runtime Coordinator is disabled.";
            return false;
        }

        if (isLifecycleOperationInProgress)
        {
            failureReason =
                "another Battle runtime lifecycle operation is already in progress.";
            return false;
        }

        return HasStableReferences(out failureReason);
    }

    private bool TryPrepareBattleRuntimeCore(
        MapGeneratorBehaviour activeMap,
        MonsterWaveConfig waveConfig,
        int playerMaxHealth,
        IReadOnlyList<int> playerProgressRequirements,
        IReadOnlyList<TowerDefinition> towerPool,
        IReadOnlyList<TowerUpgradeDefinition> upgradePool,
        float towerDraftSlotProbability)
    {
        ReleasePreparedBattleRuntimeCore();

        if (!pathfindingService.BindActiveMap(activeMap))
        {
            return FailPreparation("A* pathfinding binding failed.");
        }

        if (!CanContinueLifecycleOperation())
        {
            return FailPreparation("preparation was cancelled by a deferred release.");
        }

        if (!monsterSpawner.BindStage(activeMap, waveConfig))
        {
            return FailPreparation("Monster Spawner binding failed.");
        }

        if (!CanContinueLifecycleOperation())
        {
            return FailPreparation("preparation was cancelled by a deferred release.");
        }

        if (!towerPlacementController.BindActiveMap(activeMap))
        {
            return FailPreparation("Tower Placement binding failed.");
        }

        if (!CanContinueLifecycleOperation())
        {
            return FailPreparation("preparation was cancelled by a deferred release.");
        }

        if (!towerUpgradeSystem.TryBindStageLevelRules(
                upgradePool,
                out string levelRuleFailureReason))
        {
            return FailPreparation(
                $"Tower Upgrade Stage rule binding failed: {levelRuleFailureReason}");
        }

        if (!CanContinueLifecycleOperation())
        {
            return FailPreparation("preparation was cancelled by a deferred release.");
        }

        if (!draftSystem.BindStagePools(
                towerPool,
                upgradePool,
                towerDraftSlotProbability))
        {
            return FailPreparation("Draft pool binding failed.");
        }

        if (!CanContinueLifecycleOperation())
        {
            return FailPreparation("preparation was cancelled by a deferred release.");
        }

        if (!InitializeFreshPlayerState(
                playerMaxHealth,
                playerProgressRequirements))
        {
            return FailPreparation("fresh Player state initialization failed.");
        }

        if (!CanContinueLifecycleOperation())
        {
            return FailPreparation("preparation was cancelled by a deferred release.");
        }

        isBattlePrepared = true;

        if (!CanBeginPreparedBattle(out string failureReason))
        {
            return FailPreparation(
                $"prepared consumer validation failed: {failureReason}");
        }

        return true;
    }

    private bool InitializeFreshPlayerState(
        int playerMaxHealth,
        IReadOnlyList<int> playerProgressRequirements)
    {
        hasFreshPlayerState = false;
        preparedPlayerMaxHealth = 0;
        preparedPlayerProgressRequirements.Clear();

        if (playerSystem == null)
        {
            Debug.LogError(
                "Battle runtime coordinator cannot initialize fresh Player state " +
                "because Player System is missing.",
                this);
            return false;
        }

        if (IsBattleActive)
        {
            Debug.LogError(
                "Battle runtime coordinator cannot initialize fresh Player state " +
                "while a battle is active.",
                this);
            return false;
        }

        if (!playerSystem.TryInitializeFreshBattle(
                playerMaxHealth,
                playerProgressRequirements))
        {
            return false;
        }

        preparedPlayerMaxHealth = playerMaxHealth;

        for (int i = 0; i < playerProgressRequirements.Count; i++)
        {
            preparedPlayerProgressRequirements.Add(
                playerProgressRequirements[i]);
        }

        hasFreshPlayerState = HasValidFreshPlayerState();
        return hasFreshPlayerState;
    }

    public bool BeginPreparedBattle()
    {
        if (isLifecycleOperationInProgress)
        {
            Debug.LogError(
                "Battle runtime coordinator rejected a reentrant Battle begin request.",
                this);
            return false;
        }

        if (IsBattleActive)
        {
            Debug.LogError(
                "Battle runtime coordinator cannot begin a prepared battle while " +
                "another battle is active.",
                this);
            return false;
        }

        isLifecycleOperationInProgress = true;
        bool beginSucceeded = false;

        try
        {
            beginSucceeded = BeginPreparedBattleCore();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            beginSucceeded = FinalizeLifecycleOperation(beginSucceeded);
        }

        return beginSucceeded;
    }

    private bool BeginPreparedBattleCore()
    {
        if (!CanBeginPreparedBattle(out string failureReason))
        {
            Debug.LogError(
                $"Battle runtime coordinator cannot begin prepared battle: {failureReason}",
                this);
            return false;
        }

        IsBattleActive = true;
        playerSystem.BeginBattle();
        monsterManager.BeginBattle();
        towerPlacementController.BeginBattle();
        draftSystem.BeginBattle();
        monsterSpawner.BeginBattle();

        if (!AreConsumerGatesOpen())
        {
            Debug.LogError(
                "Battle runtime coordinator failed to open every consumer battle gate.",
                this);
            return false;
        }

        if (!draftSystem.TryOpenInitialTowerDraft(
                out DraftAttemptToken initialDraftToken,
                out string initialDraftFailureReason))
        {
            Debug.LogError(
                "Battle runtime coordinator failed to open the Initial Tower " +
                $"Draft: {initialDraftFailureReason}",
                this);
            return false;
        }

        expectedInitialDraftToken = initialDraftToken;

        if (!CanContinueLifecycleOperation())
        {
            Debug.LogWarning(
                "Battle runtime coordinator cancelled Battle begin after a " +
                "deferred release request.",
                this);
            return false;
        }

        if (!IsBattleActive ||
            !AreConsumerGatesOpen() ||
            !draftSystem.IsAwaitingDraft(initialDraftToken))
        {
            Debug.LogError(
                "Battle runtime coordinator did not retain every Battle gate after " +
                "opening the Initial Tower Draft.",
                this);
            return false;
        }

        hasFreshPlayerState = false;
        preparedPlayerMaxHealth = 0;
        preparedPlayerProgressRequirements.Clear();
        isBattlePrepared = false;
        return true;
    }

    private void RunCleanupSafely(Action cleanup)
    {
        try { cleanup(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    private void CloseBattleAuthorityAndGates()
    {
        IsBattleActive = false;
        hasFreshPlayerState = false;
        preparedPlayerMaxHealth = 0;
        preparedPlayerProgressRequirements.Clear();
        isBattlePrepared = false;
        RunCleanupSafely(() => monsterSpawner?.StopBattle());
        RunCleanupSafely(() => playerSystem?.StopBattle());
        RunCleanupSafely(() => monsterManager?.CloseBattleGate());
        RunCleanupSafely(() => towerPlacementController?.CloseBattleGate());
        RunCleanupSafely(() => draftSystem?.StopBattle());
    }

    public void StopBattle()
    {
        CaptureBeforeCleanup();
        CloseBattleAuthorityAndGates();
        RunCleanupSafely(() => monsterManager?.ForceCleanupAllMonsters());
        RunCleanupSafely(() => towerPlacementController?.StopTrackedTowerCombat());
    }

    public void ReleasePreparedBattleRuntime()
    {
        if (isLifecycleOperationInProgress)
        {
            releaseRequested = true;
            return;
        }

        ReleasePreparedBattleRuntimeCore();
    }

    private void ReleasePreparedBattleRuntimeCore()
    {
        StopBattle();
        draftSystem?.ClearStageUi();
        towerPlacementController?.DestroyTrackedTowers();
        monsterSpawner?.ClearStageBinding();
        draftSystem?.ClearStagePools();
        towerUpgradeSystem?.ClearStageLevelRules();
        towerPlacementController?.ClearActiveMap();
        pathfindingService?.ClearActiveMap();
        ResetResultTracking();
    }

    private bool CanBeginPreparedBattle(out string failureReason)
    {
        if (!HasStableReferences(out failureReason))
        {
            return false;
        }

        if (!isBattlePrepared)
        {
            failureReason = "Stage runtime has not completed preparation.";
            return false;
        }

        if (!hasFreshPlayerState || !HasValidFreshPlayerState())
        {
            hasFreshPlayerState = false;
            failureReason = "fresh Player battle state has not been initialized.";
            return false;
        }

        if (!monsterManager.CanBeginBattle(out failureReason))
        {
            failureReason = $"Monster Manager is not ready: {failureReason}";
            return false;
        }

        if (!towerPlacementController.CanBeginBattle(out failureReason))
        {
            failureReason = $"Tower Placement is not ready: {failureReason}";
            return false;
        }

        if (!draftSystem.CanBeginBattle(out failureReason))
        {
            failureReason = $"Draft System is not ready: {failureReason}";
            return false;
        }

        if (!towerUpgradeSystem.HasStageLevelRules)
        {
            failureReason = "Tower Upgrade System has no bound Stage level rules.";
            return false;
        }

        if (!monsterSpawner.CanBeginBattle(out failureReason))
        {
            failureReason = $"Monster Spawner is not ready: {failureReason}";
            return false;
        }

        if (!AreConsumerGatesClosed())
        {
            failureReason =
                "one or more Battle consumer gates are open during preparation.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private bool HasStableReferences(out string failureReason)
    {
        if (playerSystem == null)
        {
            failureReason = "Player System is not assigned.";
            return false;
        }

        if (pathfindingService == null)
        {
            failureReason = "A* Pathfinding Service is not assigned.";
            return false;
        }

        if (monsterSpawner == null)
        {
            failureReason = "Monster Spawner is not assigned.";
            return false;
        }

        if (monsterManager == null)
        {
            failureReason = "Monster Manager is not assigned.";
            return false;
        }

        if (draftSystem == null)
        {
            failureReason = "Draft System is not assigned.";
            return false;
        }

        if (towerPlacementController == null)
        {
            failureReason = "Tower Placement Controller is not assigned.";
            return false;
        }

        if (towerUpgradeSystem == null)
        {
            failureReason = "Tower Upgrade System is not assigned.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private bool FailPreparation(string failureReason)
    {
        Debug.LogError(
            $"Battle runtime coordinator failed to prepare Stage runtime: {failureReason}",
            this);
        return false;
    }

    private bool HasValidFreshPlayerState()
    {
        return playerSystem != null &&
               !playerSystem.IsBattleActive &&
               playerSystem.CurrentLevel == 1 &&
               playerSystem.CurrentProgress == 0 &&
               playerSystem.MaxHealth == preparedPlayerMaxHealth &&
               playerSystem.CurrentHealth == preparedPlayerMaxHealth &&
               HasMatchingPreparedPlayerProgressRequirements() &&
               !playerSystem.IsDefeated;
    }

    private bool HasMatchingPreparedPlayerProgressRequirements()
    {
        IReadOnlyList<int> playerRequirements =
            playerSystem.ProgressRequirements;

        if (playerRequirements == null ||
            playerRequirements.Count != preparedPlayerProgressRequirements.Count)
        {
            return false;
        }

        for (int i = 0; i < playerRequirements.Count; i++)
        {
            if (playerRequirements[i] != preparedPlayerProgressRequirements[i])
            {
                return false;
            }
        }

        return true;
    }

    private bool AreConsumerGatesClosed()
    {
        return !IsBattleActive &&
               !playerSystem.IsBattleActive &&
               !monsterManager.IsBattleActive &&
               !monsterSpawner.IsBattleActive &&
               !monsterSpawner.IsSpawning &&
               !draftSystem.IsBattleActive &&
               !towerPlacementController.IsBattleActive;
    }

    private bool AreConsumerGatesOpen()
    {
        return playerSystem.IsBattleActive &&
               monsterManager.IsBattleActive &&
               towerPlacementController.IsBattleActive &&
               draftSystem.IsBattleActive &&
               monsterSpawner.IsBattleActive;
    }

    private bool CanContinueLifecycleOperation()
    {
        return !releaseRequested && isActiveAndEnabled;
    }

    private bool FinalizeLifecycleOperation(bool operationSucceeded)
    {
        isLifecycleOperationInProgress = false;

        bool deferredReleaseRequested = releaseRequested;
        releaseRequested = false;

        if (!operationSucceeded || deferredReleaseRequested)
        {
            ReleasePreparedBattleRuntimeCore();
        }

        return operationSucceeded && !deferredReleaseRequested;
    }

    private void HandlePlayerDefeated()
    {
        TryCompleteBattleResult(BattleResult.Defeat);
    }

    private void HandleAllSpawningCompleted()
    {
        if (!IsBattleActive ||
            battleTerminalState != BattleTerminalState.None)
        {
            return;
        }

        hasNormalSpawningCompleted = true;
        TryCompleteVictory();
    }

    private void HandleMonsterResolutionCompleted()
    {
        TryCompleteVictory();
    }

    private void HandleSpawningFailed(string failureReason)
    {
        if (!IsBattleActive ||
            battleTerminalState != BattleTerminalState.None)
        {
            return;
        }

        if (isStartingSpawner)
        {
            if (string.IsNullOrWhiteSpace(
                    pendingSynchronousSpawningFailureReason))
            {
                pendingSynchronousSpawningFailureReason = failureReason;
            }

            return;
        }

        TryFailBattleRuntime(failureReason);
    }

    private void TryCompleteVictory()
    {
        if (!IsBattleActive ||
            battleTerminalState != BattleTerminalState.None ||
            !hasNormalSpawningCompleted ||
            monsterManager == null ||
            monsterManager.AliveMonsterCount != 0 ||
            playerSystem == null ||
            playerSystem.IsDefeated)
        {
            return;
        }

        TryCompleteBattleResult(BattleResult.Victory);
    }

    private void TryCompleteBattleResult(BattleResult result)
    {
        BattleTerminalState requestedTerminalState =
            result == BattleResult.Victory
                ? BattleTerminalState.Victory
                : BattleTerminalState.Defeat;

        if (!TryClaimBattleTerminalState(requestedTerminalState))
        {
            return;
        }

        CaptureBeforeCleanup();
        PublishSafely(OnBattleResultEvidence, result);
        StopBattle();
        PublishSafely(OnBattleResultPublished, result);
    }

    private void ResetResultTracking()
    {
        hasNormalSpawningCompleted = false;
        battleTerminalState = BattleTerminalState.None;
        expectedInitialDraftToken = default;
        hasInitialDraftAuthorizedSpawning = false;
        isStartingSpawner = false;
        pendingSynchronousSpawningFailureReason = null;
    }

    private void HandleInitialDraftCompleted(
        DraftAttemptToken attemptToken)
    {
        if (!IsBattleActive ||
            battleTerminalState != BattleTerminalState.None ||
            hasInitialDraftAuthorizedSpawning ||
            attemptToken != expectedInitialDraftToken)
        {
            return;
        }

        if (!draftSystem.TryConfirmCommittedInitialDraft(
                attemptToken,
                out PendingDraftUIItem committedItem) ||
            committedItem == null ||
            committedItem.DraftResult == null ||
            !committedItem.DraftResult.IsValid ||
            committedItem.TowerDefinition == null)
        {
            TryFailBattleRuntime(
                "Initial Tower Draft completion could not confirm its exact " +
                "committed held item.");
            return;
        }

        hasInitialDraftAuthorizedSpawning = true;
        bool spawningStarted = false;
        string localFailureReason = null;
        pendingSynchronousSpawningFailureReason = null;
        isStartingSpawner = true;

        try
        {
            spawningStarted = monsterSpawner.StartSpawning();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            localFailureReason =
                $"Monster Spawner threw {exception.GetType().Name} while starting.";
        }
        finally
        {
            isStartingSpawner = false;
        }

        if (string.IsNullOrWhiteSpace(localFailureReason))
        {
            localFailureReason =
                pendingSynchronousSpawningFailureReason;
        }

        pendingSynchronousSpawningFailureReason = null;

        if (!spawningStarted ||
            !string.IsNullOrWhiteSpace(localFailureReason))
        {
            TryFailBattleRuntime(
                string.IsNullOrWhiteSpace(localFailureReason)
                    ? "Monster Spawner rejected Initial Draft authorization."
                    : localFailureReason);
            return;
        }
    }

    private void HandleInitialDraftFailed(
        DraftAttemptToken attemptToken,
        string failureReason)
    {
        if (!IsBattleActive ||
            battleTerminalState != BattleTerminalState.None ||
            attemptToken != expectedInitialDraftToken)
        {
            return;
        }

        TryFailBattleRuntime(
            string.IsNullOrWhiteSpace(failureReason)
                ? "Initial Tower Draft failed after Battle start."
                : $"Initial Tower Draft failed: {failureReason}");
    }

    private bool TryClaimBattleTerminalState(
        BattleTerminalState requestedState)
    {
        if (!IsBattleActive ||
            requestedState == BattleTerminalState.None ||
            battleTerminalState != BattleTerminalState.None)
        {
            return false;
        }

        battleTerminalState = requestedState;
        return true;
    }

    private void TryFailBattleRuntime(string failureReason)
    {
        if (!TryClaimBattleTerminalState(
                BattleTerminalState.TechnicalFailure))
        {
            return;
        }

        string concreteReason = string.IsNullOrWhiteSpace(failureReason)
            ? "an unspecified Battle runtime failure occurred."
            : failureReason;

        Debug.LogError(
            $"Battle runtime coordinator terminated the Battle: " +
            $"{concreteReason}",
            this);

        CaptureBeforeCleanup();
        PublishSafely(OnBattleFailureEvidence, concreteReason);
        StopBattle();
        PublishSafely(OnBattleRuntimeFailed, concreteReason);
    }
}
