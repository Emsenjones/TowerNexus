using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[DisallowMultipleComponent]
public class MagicArcaneFieldBehaviour : MonoBehaviour
{
    private readonly List<MonsterBehaviour> tickTargets = new List<MonsterBehaviour>();

    [TitleGroup("Field")]
    [MinValue(0.01f)]
    [SerializeField] private float radius = 1f;

    [TitleGroup("Tick")]
    [MinValue(0.01f)]
    [SerializeField] private float tickInterval = 1f;
    [TitleGroup("Tick")]
    [Required]
    [SerializeField] private EffectDefinition tickEffect;

    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private TowerUpgradeDefinition sourceUpgrade;
    private float tickTimer;
    private bool isInitialized;
    private bool isCleaningUp;

    public TowerInstance SourceTower => sourceTower;
    public TowerUpgradeDefinition SourceUpgrade => sourceUpgrade;
    public bool IsInitialized => isInitialized;

    public bool Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        TowerUpgradeDefinition sourceUpgrade)
    {
        ClearRuntimeState();

        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.sourceUpgrade = sourceUpgrade;
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

        while (isInitialized && tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            ExecuteTick();
        }
    }

    private bool CanInitialize()
    {
        if (sourceTower == null ||
            monsterManager == null ||
            sourceUpgrade == null ||
            !IsAuthoredConfigurationValid())
        {
            return false;
        }

        return HasValidSource();
    }

    public bool IsAuthoredConfigurationValid()
    {
        return radius > 0f &&
               tickInterval > 0f &&
               tickEffect != null &&
               tickEffect.Radius == 0f &&
               tickEffect.IsValid();
    }

    private bool HasValidSource()
    {
        return sourceTower != null &&
               sourceTower.isActiveAndEnabled &&
               sourceTower.gameObject.activeInHierarchy &&
               transform.IsChildOf(sourceTower.transform) &&
               monsterManager != null &&
               sourceUpgrade != null &&
               sourceTower.HasUpgrade(sourceUpgrade);
    }

    private void ExecuteTick()
    {
        Vector3 fieldCenter = transform.position;

        EffectTargetResolver.CollectValidTargetsInRadius(
            monsterManager.GetAliveMonsters(),
            fieldCenter,
            radius,
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
                tickEffect,
                new EffectTriggerContext(
                    sourceTower: sourceTower,
                    sourceUpgrade: sourceUpgrade,
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
        localScale.x = radius;
        localScale.z = radius;
        transform.localScale = localScale;
    }

    private void ClearRuntimeState()
    {
        isInitialized = false;
        tickTimer = 0f;
        tickTargets.Clear();
        sourceTower = null;
        monsterManager = null;
        sourceUpgrade = null;
    }
}
