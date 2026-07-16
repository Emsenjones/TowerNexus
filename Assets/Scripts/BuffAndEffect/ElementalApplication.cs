using UnityEngine;

public static class ElementalApplication
{
    public static void TryApplyFromTowerAttack(
        TowerInstance sourceTower,
        MonsterBehaviour targetMonster,
        Vector3 applicationPosition)
    {
        if (sourceTower == null || !EffectTargetResolver.IsValidMonsterTarget(targetMonster))
        {
            return;
        }

        if (!sourceTower.TryGetElementalUpgrade(out TowerUpgradeDefinition elementalUpgradeDefinition) ||
            elementalUpgradeDefinition == null ||
            elementalUpgradeDefinition.ElementalApplyEffect == null)
        {
            return;
        }

        EffectExecutor.Execute(
            elementalUpgradeDefinition.ElementalApplyEffect,
            new EffectTriggerContext(
                sourceTower: sourceTower,
                sourceUpgrade: elementalUpgradeDefinition,
                targetMonster: targetMonster,
                hasTriggerPosition: true,
                triggerPosition: applicationPosition,
                resolvedDamage: 0,
                allowsElementalApplication: true));
    }
}
