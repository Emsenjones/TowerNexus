using System;
using UnityEngine;

[Serializable]
public class TowerLevelConfig
{
    [SerializeField] private int level = 1;
    [SerializeField] private GameObject towerModelPrefab;
    [SerializeField] private int basicDamage = 1;

    public int Level => level;
    public GameObject TowerModelPrefab => towerModelPrefab;
    public int BasicDamage => basicDamage;

    public bool IsValid()
    {
        return level > 0 && towerModelPrefab != null && basicDamage > 0;
    }
}
