using UnityEngine;
interface IAttack
{
    public void Do(MonsterBehaviour _targetMonster, int _attackPower)
    {
        _targetMonster.IsDamaged(_attackPower,true);
    }
}
class MeleeAttack:IAttack
{
      
}
class RangeAttack : IAttack
{
}

