using System;
using UnityEngine;

#if UNITY_EDITOR
public enum ElementalOpportunityObservationType
{
    Candidate = 0,
    Dispatched = 1
}

public readonly struct ElementalOpportunityObservation
{
    public ElementalOpportunityObservation(
        ElementalOpportunityObservationType observationType,
        TowerInstance sourceTower,
        TowerUpgradeDefinition elementalUpgrade,
        MonsterBehaviour targetMonster,
        ElementalOpportunityDiagnosticContext diagnostics)
    {
        ObservationType = observationType;
        SourceTower = sourceTower;
        ElementalUpgrade = elementalUpgrade;
        TargetMonster = targetMonster;
        Diagnostics = diagnostics;
    }

    public ElementalOpportunityObservationType ObservationType { get; }
    public TowerInstance SourceTower { get; }
    public TowerUpgradeDefinition ElementalUpgrade { get; }
    public MonsterBehaviour TargetMonster { get; }
    public ElementalOpportunityDiagnosticContext Diagnostics { get; }
}
#endif

public sealed class ElementalApplicationTransaction
{
    private readonly System.Collections.Generic.List<PendingBuffOverload>
        pendingOverloads =
            new System.Collections.Generic.List<PendingBuffOverload>();
    private bool isCompleted;

    internal void RecordOutcome(BuffApplyOutcome outcome)
    {
        if (isCompleted ||
            outcome.PendingOverload == null ||
            !outcome.PendingOverload.IsPending)
        {
            return;
        }

        pendingOverloads.Add(outcome.PendingOverload);
    }

    internal void FinalizePendingOverloads()
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;

        for (int i = 0; i < pendingOverloads.Count; i++)
        {
            PendingBuffOverload pendingOverload = pendingOverloads[i];

            if (pendingOverload != null && pendingOverload.Runtime != null)
            {
                pendingOverload.Runtime.FinalizePendingOverload(
                    pendingOverload);
            }
        }

        pendingOverloads.Clear();
    }
}

public enum ElementalHitReactionResult
{
    Triggered = 0,
    CooldownBlocked = 1,
    NoValidTarget = 2,
    Invalidated = 3
}

public enum ElementalHitReactionInvalidationReason
{
    None = 0,
    OwnerResolvedByEarlierElementalReaction = 1,
    BuffInstanceOrCycleChanged = 2,
    ReactionTargetInvalidBeforeCommit = 3,
    ReactionCommitRejected = 4
}

public readonly struct ElementalHitReactionObservation
{
    internal ElementalHitReactionObservation(
        float observationTime,
        MonsterBehaviour ownerMonster,
        BuffDefinition buffDefinition,
        MonsterBuffInstance buffInstance,
        int stackingCycleIdentity,
        TowerInstance triggeringTower,
        TowerUpgradeDefinition triggeringElementalUpgrade,
        TowerDamageSourceIdentity damageSourceIdentity,
        ElementalOpportunityDiagnosticContext diagnostics,
        ElementalHitReactionResult result,
        ElementalHitReactionInvalidationReason invalidationReason,
        EffectDefinition reactionEffect,
        int successfulDamageTargetCount,
        int committedFixedDamage)
    {
        ObservationTime = observationTime;
        OwnerMonster = ownerMonster;
        BuffDefinition = buffDefinition;
        BuffInstance = buffInstance;
        StackingCycleIdentity = stackingCycleIdentity;
        TriggeringTower = triggeringTower;
        TriggeringElementalUpgrade = triggeringElementalUpgrade;
        DamageSourceIdentity = damageSourceIdentity;
        Diagnostics = diagnostics;
        Result = result;
        InvalidationReason = invalidationReason;
        ReactionEffect = reactionEffect;
        SuccessfulDamageTargetCount = successfulDamageTargetCount;
        CommittedFixedDamage = committedFixedDamage;
    }

    public float ObservationTime { get; }
    public MonsterBehaviour OwnerMonster { get; }
    public BuffDefinition BuffDefinition { get; }
    internal MonsterBuffInstance BuffInstance { get; }
    public int StackingCycleIdentity { get; }
    public TowerInstance TriggeringTower { get; }
    public TowerUpgradeDefinition TriggeringElementalUpgrade { get; }
    public TowerDamageSourceIdentity DamageSourceIdentity { get; }
    public ElementalOpportunityDiagnosticContext Diagnostics { get; }
    public ElementalHitReactionResult Result { get; }
    public ElementalHitReactionInvalidationReason InvalidationReason { get; }
    public EffectDefinition ReactionEffect { get; }
    public int SuccessfulDamageTargetCount { get; }
    public int CommittedFixedDamage { get; }
}

public static class TowerOwnedHitTransaction
{
    public static bool ApplyDamage(
        BattleCombatBinding battleBinding,
        MonsterBehaviour target,
        TowerOwnedDamageResolution damageResolution,
        Vector3 hitPosition,
        ElementalOpportunityDiagnosticContext diagnostics,
        bool allowsElementalApplication,
        bool publishDamageApplication = true)
    {
#if UNITY_EDITOR
        using (CombatDiagnosticScope.Enter(battleBinding))
#endif
        {
            if (battleBinding == null || !battleBinding.CanTarget(target) ||
                damageResolution.FinalDamage <= 0 ||
                damageResolution.SourceTower == null)
            {
                return false;
            }

#if UNITY_EDITOR
            CombatDiagnosticScope.CaptureTarget(target);
#endif
            ElementalApplicationTransaction applicationTransaction = null;
            bool damageApplied = false;
            int healthBeforeDamage = target.CurrentHealth;
            int healthAfterDirectDamage = healthBeforeDamage;
            target.BeginTowerOwnedHitTransaction();

            try
            {
                target.TakeDamage(damageResolution.FinalDamage);
                damageApplied = true;
                healthAfterDirectDamage = target.CurrentHealth;

                if (battleBinding.CanTarget(target) && target.CurrentHealth > 0 &&
                    target.IsGameplayTargetable &&
                    allowsElementalApplication)
                {
                    applicationTransaction =
                        ElementalApplication.TryApplyFromTowerAttack(
                            battleBinding,
                            damageResolution.SourceTower,
                            target,
                            hitPosition,
                            diagnostics);
                }

                if (battleBinding.CanTarget(target) && target.CurrentHealth > 0 && target.IsGameplayTargetable)
                {
                    target.ResolveElementalHitReactions(
                        damageResolution.SourceTower,
                        damageResolution.DamageSourceIdentity,
                        diagnostics);
                }

                applicationTransaction?.FinalizePendingOverloads();
            }
            finally
            {
                // TakeDamage can commit health before a user/native callback throws.
                if (!damageApplied && target.CurrentHealth < healthBeforeDamage)
                {
                    damageApplied = true;
                    healthAfterDirectDamage = target.CurrentHealth;
                }
                try { applicationTransaction?.FinalizePendingOverloads(); }
                finally
                {
                    try { target.EndTowerOwnedHitTransaction(); }
                    finally
                    {
                        if (damageApplied && publishDamageApplication)
                        {
                            TowerRuntimeStatResolver.PublishTowerOwnedDamageApplication(
                                damageResolution,
                                1);
                        }

                        int appliedDamage = Mathf.Max(
                            0,
                            healthBeforeDamage - healthAfterDirectDamage);
                        TowerRuntimeStatResolver.PublishTowerOwnedTargetDamage(
                            damageResolution,
                            target,
                            appliedDamage,
                            healthBeforeDamage > 0 && healthAfterDirectDamage <= 0);

                    }
                }
            }

            return damageApplied;

        }
    }
}

public static class ElementalApplication
{
#if UNITY_EDITOR
    public static event Action<ElementalOpportunityObservation>
        OnOpportunityObserved;
#endif

    public static ElementalApplicationTransaction TryApplyFromTowerAttack(
        BattleCombatBinding battleBinding,
        TowerInstance sourceTower,
        MonsterBehaviour targetMonster,
        Vector3 applicationPosition,
        ElementalOpportunityDiagnosticContext diagnostics)
    {
        if (battleBinding == null || !battleBinding.CanTarget(targetMonster) || sourceTower == null)
        {
            return null;
        }

        if (!sourceTower.TryGetElementalUpgrade(out TowerUpgradeDefinition elementalUpgradeDefinition) ||
            elementalUpgradeDefinition == null ||
            elementalUpgradeDefinition.ElementalApplyEffect == null ||
            elementalUpgradeDefinition.ElementalStackContribution <= 0)
        {
            return null;
        }

        ElementalApplicationTransaction applicationTransaction =
            new ElementalApplicationTransaction();

        EffectExecutor.Execute(
            elementalUpgradeDefinition.ElementalApplyEffect,
            new EffectTriggerContext(
                battleBinding: battleBinding,
                sourceTower: sourceTower,
                sourceUpgrade: elementalUpgradeDefinition,
                targetMonster: targetMonster,
                hasTriggerPosition: true,
                triggerPosition: applicationPosition,
                allowsElementalApplication: true,
                requestedStackUnits:
                    elementalUpgradeDefinition.ElementalStackContribution,
                elementalOpportunityDiagnostics: diagnostics,
                elementalApplicationTransaction: applicationTransaction));
        return applicationTransaction;
    }

    public static void ObserveCandidate(
        TowerInstance sourceTower,
        MonsterBehaviour targetMonster,
        ElementalOpportunityDiagnosticContext diagnostics)
    {
#if UNITY_EDITOR
        if (sourceTower == null ||
            targetMonster == null ||
            !diagnostics.IsValid ||
            !sourceTower.TryGetElementalUpgrade(
                out TowerUpgradeDefinition elementalUpgrade) ||
            elementalUpgrade == null)
        {
            return;
        }

        PublishObservation(new ElementalOpportunityObservation(
            ElementalOpportunityObservationType.Candidate,
            sourceTower,
            elementalUpgrade,
            targetMonster,
            diagnostics));
#endif
    }

    public static void ObserveDispatched(
        EffectTriggerContext triggerContext,
        MonsterBehaviour targetMonster)
    {
#if UNITY_EDITOR
        ElementalOpportunityDiagnosticContext diagnostics =
            triggerContext.ElementalOpportunityDiagnostics;

        if (!diagnostics.IsValid ||
            !diagnostics.TopologyAuthorized ||
            triggerContext.SourceTower == null ||
            triggerContext.SourceUpgrade == null ||
            targetMonster == null)
        {
            return;
        }

        PublishObservation(new ElementalOpportunityObservation(
            ElementalOpportunityObservationType.Dispatched,
            triggerContext.SourceTower,
            triggerContext.SourceUpgrade,
            targetMonster,
            diagnostics));
#endif
    }

#if UNITY_EDITOR
    private static void PublishObservation(
        ElementalOpportunityObservation observation)
    {
        Action<ElementalOpportunityObservation> handlers =
            OnOpportunityObserved;

        if (handlers == null)
        {
            return;
        }

        Delegate[] invocationList = handlers.GetInvocationList();

        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((Action<ElementalOpportunityObservation>)invocationList[i])
                    .Invoke(observation);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Elemental opportunity observation subscriber failed: " +
                    exception.Message);
            }
        }
    }
#endif
}
