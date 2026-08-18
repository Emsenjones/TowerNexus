using System;
using UnityEngine;

[Serializable]
public class MonsterWaveEntry
{
    [SerializeField] private float waveDelay;
    [SerializeField] private MonsterBehaviour monsterRuntimeTemplate;
    [SerializeField] private int count = 1;
    [SerializeField] private float spawnInterval = 1f;

    public float WaveDelay => waveDelay;
    public MonsterBehaviour MonsterRuntimeTemplate => monsterRuntimeTemplate;
    public int Count => count;
    public float SpawnInterval => spawnInterval;

    public bool HasValidSpawnData()
    {
        return monsterRuntimeTemplate != null &&
               count > 0 &&
               spawnInterval >= 0f;
    }
}
