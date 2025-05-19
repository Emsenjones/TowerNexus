using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public interface ITower
{
    public void Initialize();
    /// <summary>
    /// Will return true if the level up is successful.
    /// </summary>
    /// <param name="shard">shard amount which the player has.</param>
    /// <param name="requiredShard">the shard amount which the player need.</param>
    /// <returns></returns>
    public bool TryLevelUp(ref int shard, out int requiredShard);

}
class ArcherTower : ITower
{
    [Title("Configs")]
    [SerializeField] Transform firePoint;
    [SerializeField] List<Animator> archerAnimatorList;
    [SerializeField] float fireSpeed;
    [SerializeField] GameObject projectilePrefab;

    public void Initialize() { }
    public List<Transform> GetGridTransformList()
    {
        throw new NotImplementedException();
    }
    public bool TryLevelUp(ref int shard, out int requiredShard)
    {
        throw new NotImplementedException();
    }
    class MagitTower : ITower
    {
        public void Initialize()
        {
            throw new NotImplementedException();
        }
        public bool TryLevelUp(ref int shard, out int requiredShard)
        {
            throw new NotImplementedException();
        }
   
    }
    class StoneTower : ITower
    {
        public void Initialize()
        {
            throw new NotImplementedException();
        }
        public bool TryLevelUp(ref int shard, out int requiredShard)
        {
            throw new NotImplementedException();
        }

    }
}
