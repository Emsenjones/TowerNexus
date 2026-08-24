using UnityEngine;

public static class ElementalApplication
{
    public static bool TryApplyFromTowerAttack(
        TowerInstance sourceTower,
        MonsterBehaviour targetMonster,
        Vector3 applicationPosition)
    {
        if (sourceTower == null || !EffectTargetResolver.IsValidMonsterTarget(targetMonster))
        {
            return false;
        }

        if (!sourceTower.TryGetElementalUpgrade(out TowerUpgradeDefinition elementalUpgradeDefinition) ||
            elementalUpgradeDefinition == null ||
            elementalUpgradeDefinition.ElementalApplyEffect == null)
        {
            return false;
        }

        EffectExecutor.Execute(
            elementalUpgradeDefinition.ElementalApplyEffect,
            new EffectTriggerContext(
                sourceTower: sourceTower,
                sourceUpgrade: elementalUpgradeDefinition,
                targetMonster: targetMonster,
                hasTriggerPosition: true,
                triggerPosition: applicationPosition,
                allowsElementalApplication: true));
        return true;
    }
}
