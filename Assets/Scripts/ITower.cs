using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public interface ITower
{
    [Serializable] class LevelConfig
    {
        [FormerlySerializedAs("index")]
        [SerializeField] int level;
        public int Level { get { return level; } }
        
        [SerializeField] int requiredShard;
        public int RequiredShard
        {
            get {
                return requiredShard;
            }
        }
        [SerializeField] int attackPower;
        [SerializeField] int projectilePerAttack;
        [SerializeField] List<Animator> archerAnimatorList;
    }
}
class ArcherTower : ITower
{
    [Title("Configs")]
    [SerializeField] Sprite iconSprite;
    public Sprite IconSprite
    {
        get {
            return iconSprite;
        }
    }
    [SerializeField] Transform firePoint;
    //[SerializeField] List<Animator> 
    [SerializeField] float fireSpeed;
    [SerializeField] GameObject projectilePrefab;

    [SerializeField] List<ITower.LevelConfig> levelConfigList;
    ITower.LevelConfig levelConfig;

    public void Initialize()
    {
        if (levelConfigList.Count <= 0)
        {
            Debug.LogError($"{GetType()}'s LevelConfigList is empty");
            return;
        }
        levelConfig = levelConfigList[0];
    }
    /// <summary>
    /// Return the required Shard to level up.
    /// </summary>
    /// <returns>Will return int.MaxValue if it has reached the maximum level.</returns>
    public int GetLevelUpShard()
    {
        int requiredShard = 0;
        if (levelConfigList.Count <= 0)
            Debug.LogError($"{GetType()}'s LevelConfigList is empty");
        else if (levelConfig == levelConfigList[^1])
        {
            Debug.Log($"{GetType()} has reached the maximum level.");
            requiredShard = int.MaxValue;
        }
        else
            for (int i = 0; i < levelConfigList.Count; i++)
                if (levelConfig == levelConfigList[i])
                {
                    requiredShard = levelConfigList[i + 1].RequiredShard;
                    break;
                }
        return requiredShard;
    }
    public void LevelUp()
    {
        if (levelConfigList.Count <= 0)
            Debug.LogError($"{GetType()}'s LevelConfigList is empty");
        else if (levelConfig == levelConfigList[^1])
            Debug.Log($"{GetType()} has reached the maximum level.");
        else
            for (int i = 0; i < levelConfigList.Count; i++)
                if (levelConfig == levelConfigList[i])
                {
                    levelConfig = levelConfigList[i + 1];
                    break;
                }
    }





}
class MagitTower : ITower { }
class StoneTower : ITower { }
