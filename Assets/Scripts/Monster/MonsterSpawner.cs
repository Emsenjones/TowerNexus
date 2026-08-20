using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MonsterSpawner : MonoBehaviour
{
    private enum SpawnExecutionTerminalState
    {
        None = 0,
        NormalCompletion = 1,
        TechnicalFailure = 2
    }

    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private MonsterManager monsterManager;
    [FormerlySerializedAs("healthBarManager")]
    [SerializeField] private MonsterStatusUIManager statusUiManager;
    [SerializeField] private DamageNumberManager damageNumberManager;
    [SerializeField] private Transform monsterRoot;

    private MonsterWaveConfig waveConfig;
    private MapGeneratorBehaviour mapGenerator;
    private Coroutine spawnRoutine;
    private bool isBattleActive;
    private bool isSpawnExecutionRunning;
    private SpawnExecutionTerminalState spawnExecutionTerminalState;

    public bool IsSpawning => isSpawnExecutionRunning;
    public bool IsBattleActive => isBattleActive;
    public bool HasStageBinding => waveConfig != null && mapGenerator != null;
    public string BoundWaveConfigName => waveConfig != null
        ? waveConfig.name
        : string.Empty;

    public event Action OnAllSpawningCompleted;
    public event Action OnSpawningStarted;
    public event Action<string> OnSpawningFailed;
    public event Action<int, MonsterWaveEntry> OnWaveSpawningStarted;
    public event Action<int, MonsterWaveEntry> OnWaveSpawningCompleted;
    public event Action<int, int, MonsterBehaviour> OnMonsterSpawnedFromWave;

    public bool TryGetExpectedMonsterCount(out int expectedMonsterCount)
    {
        if (waveConfig == null)
        {
            expectedMonsterCount = 0;
            return false;
        }

        return waveConfig.TryGetExpectedMonsterCount(out expectedMonsterCount);
    }

    public bool TryGetExpectedWaveCount(out int expectedWaveCount)
    {
        expectedWaveCount = waveConfig != null && waveConfig.Waves != null
            ? waveConfig.Waves.Count
            : 0;
        return expectedWaveCount > 0;
    }

    public bool BindStage(
        MapGeneratorBehaviour activeMap,
        MonsterWaveConfig selectedWaveConfig)
    {
        if (activeMap == null || selectedWaveConfig == null)
        {
            Debug.LogError(
                "Monster spawner cannot bind Stage data because the Active Map or " +
                "Monster Wave Config is missing.",
                this);
            return false;
        }

        mapGenerator = activeMap;
        waveConfig = selectedWaveConfig;
        ResetSpawnExecutionState();
        return true;
    }

    public void ClearStageBinding()
    {
        StopBattle();
        mapGenerator = null;
        waveConfig = null;
        ResetSpawnExecutionState();
    }

    public bool CanBeginBattle(out string failureReason)
    {
        if (mapGenerator == null)
        {
            failureReason = "Active Map is not bound.";
            return false;
        }

        if (waveConfig == null)
        {
            failureReason = "Monster Wave Config is not bound.";
            return false;
        }

        if (pathfindingService == null)
        {
            failureReason = "A* pathfinding service is not assigned.";
            return false;
        }

        if (pathfindingService.ActiveMap != mapGenerator)
        {
            failureReason = "A* pathfinding is not bound to the same Active Map.";
            return false;
        }

        if (monsterManager == null)
        {
            failureReason = "Monster Manager is not assigned.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public void BeginBattle()
    {
        ResetSpawnExecutionState();
        isBattleActive = true;
    }

    public void StopBattle()
    {
        isBattleActive = false;
        StopSpawning();
    }

    public bool StartSpawning()
    {
        if (!isBattleActive)
        {
            Debug.LogError(
                "Monster spawner cannot start because its battle gate is closed.",
                this);
            return false;
        }

        if (isSpawnExecutionRunning)
        {
            Debug.LogError(
                "Monster spawner cannot start because spawning is already running.",
                this);
            return false;
        }

        if (spawnExecutionTerminalState != SpawnExecutionTerminalState.None)
        {
            Debug.LogError(
                "Monster spawner cannot restart a completed execution in the same battle.",
                this);
            return false;
        }

        if (!CanBeginBattle(out string failureReason))
        {
            Debug.LogError(
                $"Monster spawner cannot start: {failureReason}",
                this);
            return false;
        }

        isSpawnExecutionRunning = true;
        OnSpawningStarted?.Invoke();
        Coroutine startedRoutine = StartCoroutine(SpawnWavesRoutine());

        if (isSpawnExecutionRunning && startedRoutine == null)
        {
            FailSpawnExecution(
                "Unity did not return a Coroutine handle for the active execution.");
        }

        spawnRoutine = isSpawnExecutionRunning ? startedRoutine : null;

        return spawnExecutionTerminalState !=
                   SpawnExecutionTerminalState.TechnicalFailure &&
               (isSpawnExecutionRunning ||
                spawnExecutionTerminalState ==
                    SpawnExecutionTerminalState.NormalCompletion);
    }

    private void OnDisable()
    {
        StopBattle();
    }

    public void StopSpawning()
    {
        Coroutine routineToStop = spawnRoutine;
        spawnRoutine = null;
        isSpawnExecutionRunning = false;

        if (routineToStop == null)
        {
            return;
        }

        StopCoroutine(routineToStop);
    }

    private IEnumerator SpawnWavesRoutine()
    {
        IReadOnlyList<MonsterWaveEntry> waves =
            waveConfig != null ? waveConfig.Waves : null;

        if (waves == null || waves.Count == 0)
        {
            FailSpawnExecution(
                "the selected Monster Wave Config has no Waves.");
            yield break;
        }

        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            if (!isBattleActive)
            {
                CancelSpawnExecution();
                yield break;
            }

            MonsterWaveEntry wave = waves[waveIndex];

            if (wave == null)
            {
                FailSpawnExecution(
                    $"Wave entry {waveIndex} became invalid.");
                yield break;
            }

            if (wave.WaveDelay > 0f)
            {
                yield return new WaitForSeconds(wave.WaveDelay);

                if (!isBattleActive)
                {
                    CancelSpawnExecution();
                    yield break;
                }
            }

            if (!wave.HasValidSpawnData())
            {
                FailSpawnExecution(
                    $"Wave entry {waveIndex} has invalid spawn data.");
                yield break;
            }

            OnWaveSpawningStarted?.Invoke(waveIndex, wave);

            for (int countIndex = 0; countIndex < wave.Count; countIndex++)
            {
                if (!isBattleActive)
                {
                    CancelSpawnExecution();
                    yield break;
                }

                if (!TrySpawnMonster(
                        wave.MonsterRuntimeTemplate,
                        out MonsterBehaviour spawnedMonster,
                        out string failureReason))
                {
                    FailSpawnExecution(
                        $"Monster {countIndex} in Wave {waveIndex} failed: " +
                        failureReason);
                    yield break;
                }

                OnMonsterSpawnedFromWave?.Invoke(
                    waveIndex,
                    countIndex,
                    spawnedMonster);

                bool hasMoreMonstersInWave = countIndex < wave.Count - 1;

                if (hasMoreMonstersInWave && wave.SpawnInterval > 0f)
                {
                    yield return new WaitForSeconds(wave.SpawnInterval);

                    if (!isBattleActive)
                    {
                        CancelSpawnExecution();
                        yield break;
                    }
                }
            }

            OnWaveSpawningCompleted?.Invoke(waveIndex, wave);
        }

        CompleteSpawnExecution();
    }

    private bool TrySpawnMonster(
        MonsterBehaviour monsterRuntimeTemplate,
        out MonsterBehaviour spawnedMonster,
        out string failureReason)
    {
        spawnedMonster = null;

        if (!isBattleActive)
        {
            failureReason = "the battle gate is closed.";
            return false;
        }

        if (monsterRuntimeTemplate == null)
        {
            failureReason = "the Monster runtime prefab is missing.";
            return false;
        }

        if (monsterRuntimeTemplate.transform.parent != null)
        {
            failureReason =
                $"Monster Runtime Template '{monsterRuntimeTemplate.name}' " +
                "must reference " +
                "MonsterBehaviour on the prefab root.";
            return false;
        }

        if (!monsterRuntimeTemplate.TryValidateAuthoredConfiguration(
                out string monsterFailureReason))
        {
            failureReason =
                $"Monster Runtime Template '{monsterRuntimeTemplate.name}' " +
                "is invalid: " +
                monsterFailureReason;
            return false;
        }

        if (mapGenerator == null)
        {
            failureReason = "the Active Map binding is missing.";
            return false;
        }

        GridNodeBehaviour spawnNode = mapGenerator.GetSpawnNode();

        if (spawnNode == null)
        {
            failureReason = "the Active Map has no Spawn node.";
            return false;
        }

        GridNodeBehaviour targetNode = mapGenerator.GetTargetNode();

        if (targetNode == null)
        {
            failureReason = "the Active Map has no Target node.";
            return false;
        }

        if (pathfindingService == null)
        {
            failureReason = "the A* pathfinding service is missing.";
            return false;
        }

        List<GridNodeBehaviour> initialPath =
            pathfindingService.FindPath(spawnNode, targetNode);

        if (!IsUsableInitialPath(initialPath, targetNode))
        {
            failureReason =
                "no usable initial route exists from Spawn to Target.";
            return false;
        }

        MonsterBehaviour monsterBehaviour = Instantiate(
            monsterRuntimeTemplate,
            spawnNode.WorldPosition,
            Quaternion.identity,
            monsterRoot
        );

        if (monsterBehaviour == null)
        {
            failureReason =
                $"Monster Runtime Template '{monsterRuntimeTemplate.name}' " +
                "could not be instantiated.";
            return false;
        }

        if (!monsterBehaviour.TryInitializeRuntime(
                out string initializationFailureReason))
        {
            monsterBehaviour.ForceCleanup();
            failureReason =
                $"Monster runtime initialization failed: " +
                initializationFailureReason;
            return false;
        }

        monsterBehaviour.SetRuntimeReferences(monsterManager, damageNumberManager);
        monsterBehaviour.SetCurrentNode(spawnNode);
        monsterBehaviour.SetTargetNode(targetNode);

        if (monsterManager == null || !monsterManager.RegisterMonster(monsterBehaviour))
        {
            monsterBehaviour.ForceCleanup();
            failureReason =
                "the Monster Manager rejected runtime registration.";
            return false;
        }

        CreateStatusUi(monsterBehaviour);
        monsterBehaviour.SetPath(initialPath);
        spawnedMonster = monsterBehaviour;
        failureReason = string.Empty;
        return true;
    }

    private static bool IsUsableInitialPath(
        IReadOnlyList<GridNodeBehaviour> initialPath,
        GridNodeBehaviour targetNode)
    {
        if (initialPath == null ||
            initialPath.Count == 0 ||
            targetNode == null ||
            initialPath[initialPath.Count - 1] != targetNode)
        {
            return false;
        }

        for (int i = 0; i < initialPath.Count; i++)
        {
            if (initialPath[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void CompleteSpawnExecution()
    {
        if (!isSpawnExecutionRunning ||
            spawnExecutionTerminalState != SpawnExecutionTerminalState.None)
        {
            return;
        }

        spawnExecutionTerminalState =
            SpawnExecutionTerminalState.NormalCompletion;
        isSpawnExecutionRunning = false;
        spawnRoutine = null;
        OnAllSpawningCompleted?.Invoke();
    }

    private void FailSpawnExecution(string failureReason)
    {
        if (!isSpawnExecutionRunning ||
            spawnExecutionTerminalState != SpawnExecutionTerminalState.None)
        {
            return;
        }

        spawnExecutionTerminalState =
            SpawnExecutionTerminalState.TechnicalFailure;
        isSpawnExecutionRunning = false;
        spawnRoutine = null;

        string concreteReason = string.IsNullOrWhiteSpace(failureReason)
            ? "an unspecified Wave execution failure occurred."
            : failureReason;
        Debug.LogError(
            $"Monster spawner terminated Wave execution: {concreteReason}",
            this);
        OnSpawningFailed?.Invoke(concreteReason);
    }

    private void CancelSpawnExecution()
    {
        isSpawnExecutionRunning = false;
        spawnRoutine = null;
    }

    private void ResetSpawnExecutionState()
    {
        StopSpawning();
        spawnExecutionTerminalState = SpawnExecutionTerminalState.None;
    }

    private void CreateStatusUi(MonsterBehaviour monsterBehaviour)
    {
        if (monsterBehaviour == null)
        {
            return;
        }

        if (statusUiManager == null)
        {
            Debug.LogWarning("Monster spawner cannot create monster status UI: status UI manager is not assigned.", this);
            return;
        }

        statusUiManager.CreateStatusUi(
            monsterBehaviour,
            monsterBehaviour.StatusUiOffset);
    }
}
