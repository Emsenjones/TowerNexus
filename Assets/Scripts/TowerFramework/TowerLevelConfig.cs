using System;
using UnityEngine;

[Serializable]
public class TowerLevelConfig
{
    [SerializeField] private int level = 1;
    [SerializeField] private int basicDamage = 1;
    [SerializeField] private GameObject towerModelPrefab;

    public int Level => level;
    public int BasicDamage => basicDamage;
    public GameObject TowerModelPrefab => towerModelPrefab;

    public bool IsValid()
    {
        return level > 0 && basicDamage >= 0;
    }
}
