using UnityEngine;

public static class ElementalStackApplication
{
    public static void TryApplyFromTowerAttack(
        TowerInstance sourceTower,
        MonsterBehaviour targetMonster,
        bool hasTriggerPosition,
        Vector3 triggerPosition,
        int resolvedDamage,
        bool allowElementalStackApplication)
    {
        if (!allowElementalStackApplication ||
            sourceTower == null ||
            targetMonster == null ||
            targetMonster.IsDead())
        {
            return;
        }

        if (!sourceTower.TryGetElementalUpgrade(out TowerUpgradeDefinition elementalUpgradeDefinition) ||
            elementalUpgradeDefinition == null)
        {
            return;
        }

        EffectBindingExecutor.ExecuteBindings(
            elementalUpgradeDefinition.EffectBindings,
            new EffectTriggerContext(
                EffectTriggerType.OnHit,
                sourceTower,
                elementalUpgradeDefinition,
                targetMonster,
                hasTriggerPosition,
                triggerPosition,
                resolvedDamage,
                true)
        );
    }
}
