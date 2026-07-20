using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MagicArcaneFieldBehaviour : MonoBehaviour
{
    private readonly List<MonsterBehaviour> tickTargets = new List<MonsterBehaviour>();

    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private MagicArcaneFieldRuntimeOptions runtimeOptions;
    private float tickTimer;
    private bool isInitialized;
    private bool isCleaningUp;

    public TowerInstance SourceTower => sourceTower;
    public TowerUpgradeDefinition SourceUpgrade => runtimeOptions.SourceUpgrade;
    public bool IsInitialized => isInitialized;

    public bool Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        MagicArcaneFieldRuntimeOptions runtimeOptions)
    {
        ClearRuntimeState();

        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.runtimeOptions = runtimeOptions;
        tickTimer = 0f;

        if (!CanInitialize())
        {
            Cleanup();
            return false;
        }

        isInitialized = true;
        ApplyRadiusScale();
        enabled = true;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        return true;
    }

    public void Cleanup()
    {
        if (isCleaningUp)
        {
            return;
        }

        isCleaningUp = true;
        ClearRuntimeState();
        enabled = false;

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }

        isCleaningUp = false;
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        ClearRuntimeState();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (!HasValidSource())
        {
            Cleanup();
            return;
        }

        tickTimer += Time.deltaTime;

        while (isInitialized && tickTimer >= runtimeOptions.TickInterval)
        {
            tickTimer -= runtimeOptions.TickInterval;
            ExecuteTick();
        }
    }

    private bool CanInitialize()
    {
        if (sourceTower == null ||
            monsterManager == null ||
            runtimeOptions.SourceUpgrade == null ||
            runtimeOptions.Radius <= 0f ||
            runtimeOptions.TickInterval <= 0f ||
            runtimeOptions.TickEffect == null ||
            runtimeOptions.VfxPrefab == null)
        {
            return false;
        }

        if (runtimeOptions.TickEffect.Radius > 0f || !runtimeOptions.TickEffect.IsValid())
        {
            return false;
        }

        return HasValidSource();
    }

    private bool HasValidSource()
    {
        return sourceTower != null &&
               sourceTower.isActiveAndEnabled &&
               sourceTower.gameObject.activeInHierarchy &&
               transform.IsChildOf(sourceTower.transform) &&
               monsterManager != null &&
               runtimeOptions.SourceUpgrade != null &&
               sourceTower.HasUpgrade(runtimeOptions.SourceUpgrade);
    }

    private void ExecuteTick()
    {
        Vector3 fieldCenter = transform.position;

        EffectTargetResolver.CollectValidTargetsInRadius(
            monsterManager.GetAliveMonsters(),
            fieldCenter,
            runtimeOptions.Radius,
            null,
            tickTargets);

        for (int i = 0; i < tickTargets.Count; i++)
        {
            MonsterBehaviour target = tickTargets[i];

            if (!EffectTargetResolver.IsValidMonsterTarget(target))
            {
                continue;
            }

            EffectExecutor.Execute(
                runtimeOptions.TickEffect,
                new EffectTriggerContext(
                    sourceTower: sourceTower,
                    sourceUpgrade: runtimeOptions.SourceUpgrade,
                    targetMonster: target,
                    hasTriggerPosition: true,
                    triggerPosition: fieldCenter,
                    resolvedDamage: 0,
                    allowsElementalApplication: false));

            if (!EffectTargetResolver.IsValidMonsterTarget(target))
            {
                continue;
            }

            ElementalApplication.TryApplyFromTowerAttack(
                sourceTower,
                target,
                fieldCenter);
        }
    }

    private void ApplyRadiusScale()
    {
        Vector3 localScale = transform.localScale;
        localScale.x = runtimeOptions.Radius;
        localScale.z = runtimeOptions.Radius;
        transform.localScale = localScale;
    }

    private void ClearRuntimeState()
    {
        isInitialized = false;
        tickTimer = 0f;
        tickTargets.Clear();
        sourceTower = null;
        monsterManager = null;
        runtimeOptions = default;
    }
}
