using System;
using UnityEngine;

[Serializable]
public class MonsterSpawnEntry
{
    [SerializeField] private MonsterBehaviour monsterPrefab;
    [SerializeField] private int count = 1;
    [SerializeField] private float spawnInterval = 1f;

    public MonsterBehaviour MonsterPrefab => monsterPrefab;
    public int Count => count;
    public float SpawnInterval => spawnInterval;

    public bool IsValid()
    {
        if (monsterPrefab == null)
        {
            return false;
        }

        return count > 0 && spawnInterval >= 0f;
    }
}
