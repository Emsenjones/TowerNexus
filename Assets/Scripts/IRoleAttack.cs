using System.Collections.Generic;
using System.Linq;
using UnityEngine;
interface IRoleAttack
{
    public void Do(List<MonsterBehaviour> targetMonsterList, int attackPower);
}
class MeleeRoleAttack:IRoleAttack
{
    public void Do(List<MonsterBehaviour> targetMonsterList, int attackPower)
    {
        foreach (MonsterBehaviour monster in targetMonsterList.Where(monster => monster.Health > 0)) 
            monster.TakeDamage(attackPower,true);
    }

}
class RangeRoleAttack : IRoleAttack
{
    public void Do(List<MonsterBehaviour> targetMonsterList, int attackPower)
    {
        Debug.LogWarning("RangeAttack not yet implemented.");
    }

}

