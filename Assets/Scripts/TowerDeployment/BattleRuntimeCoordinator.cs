using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleRuntimeCoordinator : MonoBehaviour
{
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private DraftSystem draftSystem;
    [SerializeField] private TowerPlacementController towerPlacementController;

    private bool hasFreshPlayerState;
    private bool isBattlePrepared;
    private bool hasNormalSpawningCompleted;
    private bool hasEstablishedResult;
    private int preparedPlayerMaxHealth;
    private bool isLifecycleOperationInProgress;
    private bool releaseRequested;
    public bool IsBattleActive { get; private set; }
    public bool IsBattlePrepared => isBattlePrepared;

    public event Action<BattleResult> OnBattleResultPublished;

    private void OnEnable()
    {
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

        ReleasePreparedBattleRuntime();
    }

    public bool TryPrepareBattleRuntime(
        MapGeneratorBehaviour activeMap,
        MonsterWaveConfig waveConfig,
        int playerMaxHealth,
        IReadOnlyList<TowerDefinition> towerPool,
        IReadOnlyList<TowerUpgradeDefinition> upgradePool)
    {
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
            towerPool == null ||
            upgradePool == null)
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
                towerPool,
                upgradePool);
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
        IReadOnlyList<TowerDefinition> towerPool,
        IReadOnlyList<TowerUpgradeDefinition> upgradePool)
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

        if (!draftSystem.BindStagePools(towerPool, upgradePool))
        {
            return FailPreparation("Draft pool binding failed.");
        }

        if (!CanContinueLifecycleOperation())
        {
            return FailPreparation("preparation was cancelled by a deferred release.");
        }

        if (!InitializeFreshPlayerState(playerMaxHealth))
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

    private bool InitializeFreshPlayerState(int playerMaxHealth)
    {
        hasFreshPlayerState = false;
        preparedPlayerMaxHealth = 0;

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

        if (!playerSystem.TryInitializeFreshBattle(playerMaxHealth))
        {
            return false;
        }

        preparedPlayerMaxHealth = playerMaxHealth;
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

        bool spawningStarted = monsterSpawner.StartSpawning();

        if (!spawningStarted)
        {
            Debug.LogError(
                "Battle runtime coordinator failed to start the selected Monster Waves.",
                this);
            return false;
        }

        if (hasEstablishedResult)
        {
            hasFreshPlayerState = false;
            preparedPlayerMaxHealth = 0;
            isBattlePrepared = false;
            return true;
        }

        if (!IsBattleActive || !AreConsumerGatesOpen())
        {
            Debug.LogError(
                "Battle runtime coordinator did not retain every Battle gate after " +
                "Monster spawning started.",
                this);
            return false;
        }

        hasFreshPlayerState = false;
        preparedPlayerMaxHealth = 0;
        isBattlePrepared = false;
        return true;
    }

    private void CloseBattleAuthorityAndGates()
    {
        IsBattleActive = false;
        hasFreshPlayerState = false;
        preparedPlayerMaxHealth = 0;
        isBattlePrepared = false;
        monsterSpawner?.StopBattle();
        playerSystem?.StopBattle();
        monsterManager?.CloseBattleGate();
        towerPlacementController?.CloseBattleGate();
        draftSystem?.StopBattle();
    }

    public void StopBattle()
    {
        CloseBattleAuthorityAndGates();
        monsterManager?.ForceCleanupAllMonsters();
        towerPlacementController?.StopTrackedTowerCombat();
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
               !playerSystem.IsDefeated;
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
        if (!IsBattleActive || hasEstablishedResult)
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
        if (!IsBattleActive || hasEstablishedResult)
        {
            return;
        }

        Debug.LogError(
            $"Battle runtime coordinator stopped after Monster Spawner failure: " +
            $"{failureReason}",
            this);
        StopBattle();
    }

    private void TryCompleteVictory()
    {
        if (!IsBattleActive ||
            hasEstablishedResult ||
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
        if (!IsBattleActive || hasEstablishedResult)
        {
            return;
        }

        hasEstablishedResult = true;
        StopBattle();
        OnBattleResultPublished?.Invoke(result);
    }

    private void ResetResultTracking()
    {
        hasNormalSpawningCompleted = false;
        hasEstablishedResult = false;
    }
}
