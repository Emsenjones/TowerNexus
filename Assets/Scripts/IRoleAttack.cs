using System.Collections.Generic;
using System.Linq;
using UnityEngine;
interface IRoleAttack
{
    public void Do(List<MonsterBehaviour> targetMonsterList, int damage);
}
class MeleeRoleAttack:IRoleAttack
{
    public void Do(List<MonsterBehaviour> targetMonsterList, int damage)
    {
        foreach (MonsterBehaviour monster in targetMonsterList.Where(monster => monster.Health > 0)) 
            monster.TakeDamage(damage,true);
    }

}
class RangeRoleAttack : IRoleAttack
{
    public void Do(List<MonsterBehaviour> targetMonsterList, int damage)
    {
        Debug.LogWarning("RangeAttack not yet implemented.");
    }

}

