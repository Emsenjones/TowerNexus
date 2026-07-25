using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MonsterSpawner : MonoBehaviour
{
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

    public bool IsSpawning => spawnRoutine != null;
    public bool IsBattleActive => isBattleActive;
    public bool HasStageBinding => waveConfig != null && mapGenerator != null;

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
        return true;
    }

    public void ClearStageBinding()
    {
        StopBattle();
        mapGenerator = null;
        waveConfig = null;
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

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (!CanBeginBattle(out string failureReason))
        {
            Debug.LogError(
                $"Monster spawner cannot start: {failureReason}",
                this);
            return false;
        }

        spawnRoutine = StartCoroutine(SpawnWavesRoutine());
        return spawnRoutine != null;
    }

    private void OnDisable()
    {
        StopBattle();
    }

    public void StopSpawning()
    {
        if (spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private IEnumerator SpawnWavesRoutine()
    {
        IReadOnlyList<MonsterWaveEntry> waves = waveConfig.Waves;

        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            if (!isBattleActive)
            {
                break;
            }

            MonsterWaveEntry wave = waves[waveIndex];

            if (wave == null)
            {
                Debug.LogError(
                    $"Monster spawner stopped because Wave entry {waveIndex} became invalid.",
                    this);
                break;
            }

            if (wave.WaveDelay > 0f)
            {
                yield return new WaitForSeconds(wave.WaveDelay);

                if (!isBattleActive)
                {
                    break;
                }
            }

            IReadOnlyList<MonsterSpawnEntry> spawnEntries = wave.SpawnEntries;

            if (spawnEntries == null || spawnEntries.Count == 0)
            {
                Debug.LogError(
                    $"Monster spawner stopped because Wave entry {waveIndex} has no Spawn Entries.",
                    this);
                break;
            }

            for (int entryIndex = 0; entryIndex < spawnEntries.Count; entryIndex++)
            {
                if (!isBattleActive)
                {
                    break;
                }

                MonsterSpawnEntry spawnEntry = spawnEntries[entryIndex];

                if (spawnEntry == null || !spawnEntry.IsValid())
                {
                    Debug.LogError(
                        $"Monster spawner stopped because Spawn Entry {entryIndex} " +
                        $"in Wave {waveIndex} became invalid.",
                        this);
                    spawnRoutine = null;
                    yield break;
                }

                for (int countIndex = 0; countIndex < spawnEntry.Count; countIndex++)
                {
                    if (!isBattleActive)
                    {
                        break;
                    }

                    SpawnMonster(spawnEntry.MonsterDefinition);

                    bool hasMoreMonstersInEntry = countIndex < spawnEntry.Count - 1;

                    if (hasMoreMonstersInEntry && spawnEntry.SpawnInterval > 0f)
                    {
                        yield return new WaitForSeconds(spawnEntry.SpawnInterval);

                        if (!isBattleActive)
                        {
                            break;
                        }
                    }
                }
            }
        }

        spawnRoutine = null;
    }

    private MonsterBehaviour SpawnMonster(MonsterDefinition monsterDefinition)
    {
        if (!isBattleActive)
        {
            return null;
        }

        if (monsterDefinition == null || !monsterDefinition.IsValid())
        {
            Debug.LogWarning("Monster spawner cannot spawn monster: monster definition is invalid.", this);
            return null;
        }

        GridNodeBehaviour spawnNode = mapGenerator.GetSpawnNode();

        if (spawnNode == null)
        {
            Debug.LogWarning("Monster spawner cannot spawn monster: map has no spawn node.", mapGenerator);
            return null;
        }

        GridNodeBehaviour targetNode = mapGenerator.GetTargetNode();

        if (targetNode == null)
        {
            Debug.LogWarning("Monster spawner cannot assign movement path: map has no target node.", mapGenerator);
        }

        GameObject monsterObject = Instantiate(
            monsterDefinition.MonsterPrefab,
            spawnNode.WorldPosition,
            Quaternion.identity,
            monsterRoot
        );

        if (!monsterObject.TryGetComponent(out MonsterBehaviour monsterBehaviour))
        {
            Debug.LogWarning($"Monster spawner spawned prefab '{monsterObject.name}' without MonsterBehaviour.", monsterObject);
            Destroy(monsterObject);
            return null;
        }

        monsterBehaviour.Initialize(monsterDefinition);
        monsterBehaviour.SetRuntimeReferences(monsterManager, damageNumberManager);
        monsterBehaviour.SetCurrentNode(spawnNode);
        monsterBehaviour.SetTargetNode(targetNode);

        if (monsterManager == null || !monsterManager.RegisterMonster(monsterBehaviour))
        {
            Debug.LogWarning("Monster spawner cannot register monster because the Monster Manager is missing or the battle is inactive.", this);
            monsterBehaviour.ForceCleanup();
            return null;
        }

        CreateStatusUi(monsterBehaviour, monsterDefinition);

        if (pathfindingService != null && targetNode != null)
        {
            List<GridNodeBehaviour> path = pathfindingService.FindPath(spawnNode, targetNode);
            monsterBehaviour.SetPath(path);
        }
        else if (pathfindingService == null)
        {
            Debug.LogWarning("Monster spawner cannot assign movement path: pathfinding service is not assigned.", this);
        }

        return monsterBehaviour;
    }

    private void CreateStatusUi(MonsterBehaviour monsterBehaviour, MonsterDefinition monsterDefinition)
    {
        if (monsterBehaviour == null || monsterDefinition == null)
        {
            return;
        }

        if (statusUiManager == null)
        {
            Debug.LogWarning("Monster spawner cannot create monster status UI: status UI manager is not assigned.", this);
            return;
        }

        statusUiManager.CreateStatusUi(monsterBehaviour, monsterDefinition.HealthBarOffset);
    }
}
