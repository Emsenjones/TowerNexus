using System;
using UnityEngine;

[Serializable]
public class MonsterSpawnEntry
{
    [SerializeField] private MonsterDefinition monsterDefinition;
    [SerializeField] private int count = 1;
    [SerializeField] private float spawnInterval = 1f;

    public MonsterDefinition MonsterDefinition => monsterDefinition;
    public int Count => count;
    public float SpawnInterval => spawnInterval;

    public bool IsValid()
    {
        if (monsterDefinition == null)
        {
            return false;
        }

        return count > 0 && spawnInterval >= 0f;
    }
}
