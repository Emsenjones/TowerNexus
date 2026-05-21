using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MonsterWaveEntry
{
    [SerializeField] private string waveId;
    [SerializeField] private float waveDelay;
    [SerializeField] private List<MonsterSpawnEntry> spawnEntries = new List<MonsterSpawnEntry>();

    public string WaveId => waveId;
    public float WaveDelay => waveDelay;
    public IReadOnlyList<MonsterSpawnEntry> SpawnEntries => spawnEntries;
}
