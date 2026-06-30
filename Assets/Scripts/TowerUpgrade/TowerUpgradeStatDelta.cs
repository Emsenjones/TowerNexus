using System;
using UnityEngine;

[Serializable]
public class TowerUpgradeStatDelta
{
    [SerializeField] private TowerUpgradeStatType statType;
    [SerializeField] private float additiveValue;

    public TowerUpgradeStatType StatType => statType;
    public float AdditiveValue => additiveValue;

    public bool IsCompatibleWithTowerFamily(TowerFamily towerFamily)
    {
        switch (statType)
        {
            case TowerUpgradeStatType.AttackRange:
            case TowerUpgradeStatType.AttackInterval:
            case TowerUpgradeStatType.DamageBonus:
                return true;
            case TowerUpgradeStatType.MagicOrbRotationSpeed:
            case TowerUpgradeStatType.MagicOrbMaxHitCount:
                return towerFamily == TowerFamily.Magic;
            case TowerUpgradeStatType.DroneBatteryDuration:
            case TowerUpgradeStatType.DroneBurstCooldown:
                return towerFamily == TowerFamily.Drone;
            default:
                return false;
        }
    }

    public bool RequiresWholeNumberAdditiveValue()
    {
        return statType == TowerUpgradeStatType.MagicOrbMaxHitCount ||
               statType == TowerUpgradeStatType.DamageBonus;
    }

    public bool HasWholeNumberAdditiveValue()
    {
        return Mathf.Approximately(additiveValue, Mathf.Round(additiveValue));
    }
}
