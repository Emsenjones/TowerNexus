using System;
using UnityEngine;

[Serializable]
public class TowerLevelConfig
{
    [SerializeField] private int level = 1;
    [SerializeField] private float baseDamageModifier;
    [SerializeField] private float attackIntervalModifier;
    [SerializeField] private float rangeModifier;
    [SerializeField] private GameObject towerModelPrefab;
    [SerializeField] private Sprite displayIcon;

    public int Level => level;
    public float BaseDamageModifier => baseDamageModifier;
    public float AttackIntervalModifier => attackIntervalModifier;
    public float RangeModifier => rangeModifier;
    public GameObject TowerModelPrefab => towerModelPrefab;
    public Sprite DisplayIcon => displayIcon;

    public bool IsValid()
    {
        return level > 0;
    }
}
