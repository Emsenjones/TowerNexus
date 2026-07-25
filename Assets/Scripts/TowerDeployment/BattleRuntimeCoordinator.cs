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
    public bool IsBattleActive { get; private set; }
    public bool IsBattlePrepared => isBattlePrepared;

    private void OnEnable()
    {
        if (playerSystem != null)
        {
            playerSystem.OnPlayerDefeated += HandlePlayerDefeated;
        }
    }

    private void OnDisable()
    {
        if (playerSystem != null)
        {
            playerSystem.OnPlayerDefeated -= HandlePlayerDefeated;
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
        if (IsBattleActive)
        {
            Debug.LogError(
                "Battle runtime coordinator cannot prepare a Stage while a battle is active.",
                this);
            return false;
        }

        ReleasePreparedBattleRuntime();

        if (!HasStableReferences(out string failureReason))
        {
            Debug.LogError(
                $"Battle runtime coordinator cannot prepare Stage runtime: {failureReason}",
                this);
            return false;
        }

        if (activeMap == null ||
            waveConfig == null ||
            towerPool == null ||
            upgradePool == null)
        {
            Debug.LogError(
                "Battle runtime coordinator cannot prepare Stage runtime because one " +
                "or more Stage dependency slices are missing.",
                this);
            return false;
        }

        if (!pathfindingService.BindActiveMap(activeMap))
        {
            return FailPreparation("A* pathfinding binding failed.");
        }

        if (!monsterSpawner.BindStage(activeMap, waveConfig))
        {
            return FailPreparation("Monster Spawner binding failed.");
        }

        if (!towerPlacementController.BindActiveMap(activeMap))
        {
            return FailPreparation("Tower Placement binding failed.");
        }

        if (!draftSystem.BindStagePools(towerPool, upgradePool))
        {
            return FailPreparation("Draft pool binding failed.");
        }

        if (!InitializeFreshPlayerState(playerMaxHealth))
        {
            return FailPreparation("fresh Player state initialization failed.");
        }

        isBattlePrepared = true;

        if (!CanBeginPreparedBattle(out failureReason))
        {
            return FailPreparation(
                $"prepared consumer validation failed: {failureReason}");
        }

        return true;
    }

    private bool InitializeFreshPlayerState(int playerMaxHealth)
    {
        hasFreshPlayerState = false;

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

        hasFreshPlayerState =
            !playerSystem.IsBattleActive &&
            !playerSystem.IsDefeated &&
            playerSystem.MaxHealth == playerMaxHealth &&
            playerSystem.CurrentHealth == playerMaxHealth;
        return hasFreshPlayerState;
    }

    public bool BeginPreparedBattle()
    {
        if (IsBattleActive)
        {
            Debug.LogError(
                "Battle runtime coordinator cannot begin a prepared battle while " +
                "another battle is active.",
                this);
            return false;
        }

        if (!CanBeginPreparedBattle(out string failureReason))
        {
            Debug.LogError(
                $"Battle runtime coordinator cannot begin prepared battle: {failureReason}",
                this);
            CloseBattleAuthorityAndGates();
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
            CloseBattleAuthorityAndGates();
            return false;
        }

        if (!monsterSpawner.StartSpawning())
        {
            Debug.LogError(
                "Battle runtime coordinator failed to start the selected Monster Waves.",
                this);
            CloseBattleAuthorityAndGates();
            return false;
        }

        hasFreshPlayerState = false;
        isBattlePrepared = false;
        return true;
    }

    private void CloseBattleAuthorityAndGates()
    {
        IsBattleActive = false;
        hasFreshPlayerState = false;
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
        CloseBattleAuthorityAndGates();
        draftSystem?.ClearStageUi();
        monsterManager?.ForceCleanupAllMonsters();
        towerPlacementController?.StopTrackedTowerCombat();
        towerPlacementController?.DestroyTrackedTowers();
        monsterSpawner?.ClearStageBinding();
        draftSystem?.ClearStagePools();
        towerPlacementController?.ClearActiveMap();
        pathfindingService?.ClearActiveMap();
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

        if (!hasFreshPlayerState)
        {
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
        ReleasePreparedBattleRuntime();
        return false;
    }

    private bool AreConsumerGatesOpen()
    {
        return playerSystem.IsBattleActive &&
               monsterManager.IsBattleActive &&
               towerPlacementController.IsBattleActive &&
               draftSystem.IsBattleActive &&
               monsterSpawner.IsBattleActive;
    }

    private void HandlePlayerDefeated()
    {
        StopBattle();
    }
}
