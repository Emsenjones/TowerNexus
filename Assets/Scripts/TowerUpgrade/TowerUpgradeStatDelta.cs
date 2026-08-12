using System;
using UnityEngine;

[Serializable]
public class TowerUpgradeStatDelta
{
    [SerializeField] private TowerUpgradeBasicStatType statType;
    [SerializeField] private float additiveValue;

    public TowerUpgradeBasicStatType StatType => statType;
    public float AdditiveValue => additiveValue;

    public bool IsCompatibleWithTowerFamily(TowerFamily towerFamily)
    {
        switch (statType)
        {
            case TowerUpgradeBasicStatType.AttackRange:
            case TowerUpgradeBasicStatType.AttackCycleDuration:
            case TowerUpgradeBasicStatType.DamageBonus:
                return true;
            case TowerUpgradeBasicStatType.MagicOrbRotationSpeed:
                return towerFamily == TowerFamily.Magic;
            case TowerUpgradeBasicStatType.DroneBurstCooldown:
                return towerFamily == TowerFamily.Drone;
            default:
                return false;
        }
    }

    public bool RequiresWholeNumberAdditiveValue()
    {
        return statType == TowerUpgradeBasicStatType.DamageBonus;
    }

    public bool HasWholeNumberAdditiveValue()
    {
        return Mathf.Approximately(additiveValue, Mathf.Round(additiveValue));
    }

    public bool HasFiniteAdditiveValue()
    {
        return !float.IsNaN(additiveValue) && !float.IsInfinity(additiveValue);
    }

}
