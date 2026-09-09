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
    private BattleCombatBinding battleBinding;
    private TowerUpgradeDefinition sourceUpgrade;
    private float tickTimer;
    private int tickOrdinal;
    private bool isInitialized;
    private bool isCleaningUp;

    public TowerInstance SourceTower => sourceTower;
    public TowerUpgradeDefinition SourceUpgrade => sourceUpgrade;
    public bool IsInitialized => isInitialized;

    public bool Initialize(
        TowerInstance sourceTower,
        BattleCombatBinding battleBinding,
        TowerUpgradeDefinition sourceUpgrade)
    {
        ClearRuntimeState();

        this.sourceTower = sourceTower;
        this.battleBinding = battleBinding;
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
#if UNITY_EDITOR
        using (CombatDiagnosticScope.Enter(battleBinding))
#endif
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
    }

    private bool CanInitialize()
    {
        if (sourceTower == null ||
            (battleBinding == null || !battleBinding.IsUsable) ||
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
               tickEffect.IsValidForDamageMode(
                   EffectDamageMode.TowerScaled) &&
               tickEffect.ContainsDealDamageAction();
    }

    private bool HasValidSource()
    {
        return sourceTower != null &&
               sourceTower.isActiveAndEnabled &&
               sourceTower.gameObject.activeInHierarchy &&
               transform.IsChildOf(sourceTower.transform) &&
               battleBinding != null && battleBinding.IsUsable &&
               sourceUpgrade != null &&
               sourceTower.HasUpgrade(sourceUpgrade);
    }

    private void ExecuteTick()
    {
        Vector3 fieldCenter = transform.position;
        int currentTickOrdinal = tickOrdinal++;

        EffectTargetResolver.CollectValidTargetsInRadius(
            battleBinding.GetAliveMonsters(),
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
                    battleBinding: battleBinding,
                    sourceTower: sourceTower,
                    sourceUpgrade: sourceUpgrade,
                    targetMonster: target,
                    hasTriggerPosition: true,
                    triggerPosition: fieldCenter,
                    allowsElementalApplication: false,
                    elementalOpportunityDiagnostics:
                        new ElementalOpportunityDiagnosticContext(
                            ElementalOpportunityProvenance.MagicArcaneField,
                            ElementalOpportunityMemberIdentity.NotApplicable,
                            ElementalOpportunityResultRole.PersistentTick,
                            currentTickOrdinal,
                            topologyAuthorized: false,
                            observeResolvedTargetsAsCandidates: true)));
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
        tickOrdinal = 0;
        tickTargets.Clear();
        sourceTower = null;
        battleBinding = null;
        sourceUpgrade = null;
    }
}
