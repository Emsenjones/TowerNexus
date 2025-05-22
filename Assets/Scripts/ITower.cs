using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public interface ITower
{
    [Serializable] class LevelConfig { }
    public void Initialize();
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
/// <summary>
/// Launching projectiles to attack monsters.
/// </summary>
[Serializable] class ProjectileTower : ITower
{
    [FormerlySerializedAs("archerAnimatorList")]
    [Title("Configs")]
    [SerializeField] Transform shootPoint;
    [SerializeField] List<Animator> animatorList;
    [SerializeField] int projectilesPerAttack;
    [SerializeField] GameObject projectilePrefab;

    public void Initialize() { }
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
            Debug.LogError($"{GetType()} is missing the projectilePrefab!");
            return;
        }
        #region To launch projectiles to attack the nearest monsters, the count of projectiles is projectilesPerAttack.

        var selectedMonsterList = monsterList
            .OrderBy(m => Vector3.Distance(shootPoint.transform.position,
                m.transform.position))
            .Take(projectilesPerAttack)
            .ToList();
        if (selectedMonsterList.Count <= 0) return;

        #endregion
        foreach (MonsterBehaviour monster in selectedMonsterList)
        {
            ProjectileBehaviour projectile = DungeonManager.Instance.RecyclePoolController
                .GenerateOneObject(projectilePrefab).GetComponent<ProjectileBehaviour>();
            if (projectile == null)
                Debug.LogError($"{projectilePrefab.name} is missing the ProjectileBehaviour!");
            else
            {
                projectile.transform.position = shootPoint.position;
                projectile.Initialize(monster.transform);
            }
        }
        #region Play the animation.

        if (animatorList.Count <= 0)
        {
            Debug.LogError($"{GetType()} is missing the animatorList!");
            return;
        }
        foreach (Animator animator in animatorList)
            if (animator == null)
                Debug.LogError($"{GetType()}'s archerAnimatorList is missing an animator!");
            else
                animator.SetTrigger(AnimatorParams.Attack);

        #endregion

    }
}
/// <summary>
/// Attacking all monsters that are in the range.
/// </summary>
[Serializable] class AoeTower : ITower
{
    [SerializeField] int damage;
    [SerializeField] List<Animator> animatorList;
    public void Initialize() { }
    public bool TryLevelUp(ref int shard, out int requiredShard)
    {
        throw new NotImplementedException();
    }
    public void Attack(List<MonsterBehaviour> monsterList)
    {
        foreach (MonsterBehaviour monster in monsterList)
            monster.TakeDamage(damage, false);
        #region Play the animation.

        if (animatorList.Count <= 0)
        {
            Debug.LogError($"{GetType()} is missing the animatorList!");
            return;
        }
        foreach (Animator animator in animatorList)
            if (animator == null)
                Debug.LogError($"{GetType()}'s archerAnimatorList is missing an animator!");
            else
                animator.SetTrigger(AnimatorParams.Attack);

        #endregion
    }
}
class SupportTower { }
