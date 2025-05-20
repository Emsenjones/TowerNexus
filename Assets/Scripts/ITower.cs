using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public interface ITower
{
    [Serializable] class LevelConfig { }
    public void Initialize(Transform shootPointTransform);
    /// <summary>
    /// Will return true if the level up is successful.
    /// </summary>
    /// <param name="shard">shard amount which the player has.</param>
    /// <param name="requiredShard">the shard amount which the player needs.</param>
    /// <returns></returns>
    public bool TryLevelUp(ref int shard, out int requiredShard);
    /// <summary>
    /// To attack monsters.
    /// </summary>
    /// <param name="monsterList">Attackable monsters.</param>
    public void Attack(List<MonsterBehaviour> monsterList);

}
class ArcherTower : ITower
{
    [Title("Configs")]
    [SerializeField] List<Animator> archerAnimatorList;
    [SerializeField] float fireSpeed;
    [SerializeField] int projectilesPerAttack;
    [SerializeField] GameObject projectilePrefab;
    Transform shootPoint;

    public void Initialize(Transform shootPointTransform)
    {
        shootPoint = shootPointTransform;
    }
    public bool TryLevelUp(ref int shard, out int requiredShard)
    {
        throw new NotImplementedException();
    }
    public void Attack(List<MonsterBehaviour> monsterList)
    {
        if (shootPoint == null)
        {
            Debug.LogError($"{GetType()} is missing the shootPoint!");
            return;
        }
        if (projectilePrefab == null)
        {
            Debug.Log($"{GetType()} is missing the projectilePrefab!");
            return;
        }
        if (monsterList == null || monsterList.Count == 0)
        {
            Debug.Log($"{GetType()} doesn't have monsters to attack...");
            return;
        }
        var selectedMonsterList = monsterList
            .OrderBy(m => Vector3.Distance(shootPoint.transform.position,
                m.transform.position))
            .Take(projectilesPerAttack)
            .ToList();
        if (selectedMonsterList.Count <= 0) return;
        foreach (MonsterBehaviour monster in selectedMonsterList)
        {
            ProjectileBehaviour projectile = DungeonManager.Instance.RecyclePoolController
                .GenerateOneObject(projectilePrefab).GetComponent<ProjectileBehaviour>();
            if (projectile == null)
                Debug.LogError($"{projectilePrefab.name} is missing the ProjectileBehaviour!");
            else
                projectile.Initialize(monster.transform);

        }
    }
}
class MagicTower { }
class StoneTower { }
class SupportTower { }
