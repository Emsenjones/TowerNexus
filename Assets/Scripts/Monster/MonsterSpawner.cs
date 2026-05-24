using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private MonsterWaveConfig waveConfig;
    [SerializeField] private MapGeneratorBehaviour mapGenerator;
    [SerializeField] private AStarPathfindingService pathfindingService;
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private Transform monsterRoot;
    [SerializeField] private bool playOnStart;

    private Coroutine spawnRoutine;

    public bool IsSpawning => spawnRoutine != null;

    private void Start()
    {
        if (playOnStart)
        {
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (waveConfig == null)
        {
            Debug.LogWarning("Monster spawner cannot start: wave config is not assigned.", this);
            return;
        }

        if (mapGenerator == null)
        {
            Debug.LogWarning("Monster spawner cannot start: map generator is not assigned.", this);
            return;
        }

        spawnRoutine = StartCoroutine(SpawnWavesRoutine());
    }

    private void OnDisable()
    {
        StopSpawning();
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
            MonsterWaveEntry wave = waves[waveIndex];

            if (wave == null)
            {
                continue;
            }

            if (wave.WaveDelay > 0f)
            {
                yield return new WaitForSeconds(wave.WaveDelay);
            }

            IReadOnlyList<MonsterSpawnEntry> spawnEntries = wave.SpawnEntries;

            for (int entryIndex = 0; entryIndex < spawnEntries.Count; entryIndex++)
            {
                MonsterSpawnEntry spawnEntry = spawnEntries[entryIndex];

                if (spawnEntry == null || !spawnEntry.IsValid())
                {
                    Debug.LogWarning($"Monster spawner skipped invalid spawn entry in wave '{wave.WaveId}'.", this);
                    continue;
                }

                for (int countIndex = 0; countIndex < spawnEntry.Count; countIndex++)
                {
                    SpawnMonster(spawnEntry.MonsterDefinition);

                    bool hasMoreMonstersInEntry = countIndex < spawnEntry.Count - 1;
                    bool hasMoreEntriesInWave = entryIndex < spawnEntries.Count - 1;
                    bool hasMoreWaves = waveIndex < waves.Count - 1;

                    if ((hasMoreMonstersInEntry || hasMoreEntriesInWave || hasMoreWaves) && spawnEntry.SpawnInterval > 0f)
                    {
                        yield return new WaitForSeconds(spawnEntry.SpawnInterval);
                    }
                }
            }
        }

        spawnRoutine = null;
    }

    private MonsterBehaviour SpawnMonster(MonsterDefinition monsterDefinition)
    {
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
        monsterBehaviour.SetRuntimeReferences(monsterManager, playerSystem);
        monsterBehaviour.SetCurrentNode(spawnNode);
        monsterBehaviour.SetTargetNode(targetNode);

        if (monsterManager != null)
        {
            monsterManager.RegisterMonster(monsterBehaviour);
        }
        else
        {
            Debug.LogWarning("Monster spawner cannot register monster: monster manager is not assigned.", this);
        }

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
}
