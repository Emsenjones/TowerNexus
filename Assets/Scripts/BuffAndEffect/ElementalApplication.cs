using UnityEngine;

public static class ElementalApplication
{
    public static void TryApplyFromTowerAttack(
        TowerInstance sourceTower,
        MonsterBehaviour targetMonster,
        Vector3 applicationPosition,
        EffectTriggerType runtimeTriggerType)
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
                runtimeTriggerType,
                sourceTower,
                elementalUpgradeDefinition,
                targetMonster,
                true,
                applicationPosition,
                0,
                true));
    }
}
