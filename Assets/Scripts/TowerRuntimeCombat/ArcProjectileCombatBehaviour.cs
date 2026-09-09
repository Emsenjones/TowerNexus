using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class ArcProjectileCombatBehaviour : TowerCombatBehaviour
{
    [TitleGroup("Projectile")]
    [Required]
    [SerializeField] private ProjectileBehaviour projectilePrefab;
    [TitleGroup("Projectile")]
    [MinValue(0f)]
    [SerializeField] private float arcHeight = 1f;

    private readonly List<MonsterBehaviour> releaseCandidates =
        new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> pendingTargets =
        new List<MonsterBehaviour>();
    private readonly List<Vector3> pendingTargetPositions = new List<Vector3>();
    private MonsterBehaviour pendingPrimaryTarget;
    private AdditionalAttackEntityAuthoring pendingAdditionalAttackEntities;
    private int pendingInitialShellCount;
    private float pendingWindupAdmissionTime;
    private bool hasLoggedInvalidExplosiveShellEffect;

    public override TowerFamily SupportedTowerFamily => TowerFamily.Cannon;
    public ProjectileBehaviour ProjectilePrefab => projectilePrefab;
    public float ArcHeight => arcHeight;

    protected override TowerCombatBaseStats CreateBaseStats()
    {
        return new TowerCombatBaseStats(
            BaseAttackRange,
            BaseAttackCycleDuration);
    }

    protected override bool IsSubtypeConfigurationValid()
    {
        if (projectilePrefab == null || !projectilePrefab.IsValid())
        {
            Debug.LogWarning(
                "Arc projectile combat is invalid: projectile prefab or behaviour authoring is missing.",
                this);
            return false;
        }

        if (projectilePrefab.HitDistanceThreshold <= 0f)
        {
            Debug.LogWarning(
                "Arc projectile combat is invalid: hit distance threshold must be greater than zero.",
                projectilePrefab);
            return false;
        }

        if (arcHeight < 0f)
        {
            Debug.LogWarning("Arc projectile combat is invalid: arc height cannot be negative.", this);
            return false;
        }

        return true;
    }

    protected override void OnCombatInitialized()
    {
        hasLoggedInvalidExplosiveShellEffect = false;
    }

    protected override void OnCombatUpdate()
    {
        if (IsWaitingForAnimationRelease)
        {
            return;
        }

        if (!IsAttackCycleReady || projectilePrefab == null)
        {
            return;
        }

        if (projectilePrefab.HitDistanceThreshold <= 0f)
        {
            return;
        }

        MonsterBehaviour target = SelectTarget();
        SetCurrentTarget(target);

        if (!IsValidTarget(target))
        {
            return;
        }

        CapturePendingAttackTopology();
        SetWaitingForAnimationRelease();

        if (!SetAttackAnimatorTrigger())
        {
            ReleasePendingAttack();
        }
    }

    protected override void OnAnimationRelease()
    {
        ReleasePendingAttack();
    }

    protected override void OnCombatCleanup()
    {
        ResetPendingAttack();
    }

    protected override RequiredUpgradeRefreshResult RefreshBehaviourPackage(
        TowerUpgradeDefinition upgradeDefinition, out string failureReason)
    {
        failureReason = string.Empty;
        if (upgradeDefinition == null)
        {
            return RequiredUpgradeRefreshResult.NotRequired;
        }

        bool refreshed = false;
        List<ProjectileBehaviour> projectileSnapshot = GetOwnedProjectileSnapshot();

        switch (upgradeDefinition.BehaviourPackageType)
        {
            case TowerBehaviourPackageType.CannonExplosiveShell:
                for (int i = 0; i < projectileSnapshot.Count; i++)
                {
                    ProjectileBehaviour projectile = projectileSnapshot[i];

                    if (projectile != null)
                    {
                        refreshed |= projectile.TryRefreshExplosiveShell(
                            upgradeDefinition,
                            upgradeDefinition.ExplosiveShellEffect);
                    }
                }
                break;
            case TowerBehaviourPackageType.CannonBouncingShell:
                for (int i = 0; i < projectileSnapshot.Count; i++)
                {
                    ProjectileBehaviour projectile = projectileSnapshot[i];

                    if (projectile != null)
                    {
                        refreshed |= projectile.TryEnableInitialBouncingShell(
                            upgradeDefinition.BounceSearchRadius,
                            upgradeDefinition.MaxBounceCount,
                            upgradeDefinition.BounceArcHeight,
                            upgradeDefinition.BounceTargetSelectionType,
                            upgradeDefinition.BounceDamageScale);
                    }
                }
                break;
        }
        return refreshed ? RequiredUpgradeRefreshResult.Applied : RequiredUpgradeRefreshResult.NotRequired;
    }

    private void ReleasePendingAttack()
    {
        if (!IsWaitingForAnimationRelease || projectilePrefab == null)
        {
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null || projectilePrefab.HitDistanceThreshold <= 0f)
        {
            ResetPendingAttack();
            return;
        }

        ResolvedTowerCombatStats resolvedStats = ResolveCombatStats();
        CollectTargetCandidates(
            releaseCandidates,
            origin.position,
            resolvedStats.AttackRange);
        MonsterBehaviour releaseTarget = SelectTargetFromCandidates(
            releaseCandidates,
            origin.position);

        if (!IsValidTarget(releaseTarget))
        {
            ResetPendingAttack();
            return;
        }

        pendingPrimaryTarget = releaseTarget;
        SetCurrentTarget(releaseTarget);
        CaptureReleaseTargetsAndPositions(releaseTarget, origin.position);

        if (pendingTargetPositions.Count == 0 ||
            pendingTargets.Count != pendingTargetPositions.Count)
        {
            ResetPendingAttack();
            return;
        }

        int releasedCount = 0;

        for (int i = 0; i < pendingTargetPositions.Count; i++)
        {
            bool isAdditional = i > 0;
            ProjectileBehaviour releasePrefab = projectilePrefab;

            if (isAdditional &&
                (pendingAdditionalAttackEntities == null ||
                 pendingAdditionalAttackEntities.Prefab == null ||
                 !pendingAdditionalAttackEntities.Prefab.TryGetComponent(out releasePrefab)))
            {
                continue;
            }

            float damageScale = isAdditional
                ? pendingAdditionalAttackEntities.DamageScale
                : 1f;
            TowerDamageSourceIdentity damageSourceIdentity = isAdditional
                ? TowerDamageSourceIdentity.AdditionalDirect
                : TowerDamageSourceIdentity.PrimaryDirect;
            ProjectileRuntimeOptions runtimeOptions =
                CreateRuntimeOptions(isAdditional);

            if (TryReleaseProjectile(
                    releasePrefab,
                    origin,
                    pendingTargetPositions[i],
                    pendingTargets[i],
                    damageScale,
                    damageSourceIdentity,
                    ProjectileFlightType.Arc,
                    arcHeight,
                    runtimeOptions,
                    out ProjectileBehaviour releasedProjectile))
            {
#if UNITY_EDITOR
                releasedProjectile.ConfigureArcRuntimeObservation(
                    isAdditional
                        ? ProjectileArcMemberType.AdditionalInitial
                        : ProjectileArcMemberType.PrimaryInitial,
                    Time.time - pendingWindupAdmissionTime);
#endif
                releasedCount++;
            }
        }

        if (releasedCount == 0)
        {
            ResetPendingAttack();
            return;
        }

        StartAttackCycle(resolvedStats.AttackCycleDuration);
        PlayAttackReleaseVfx(Quaternion.identity);
        RaiseProjectileReleased(pendingPrimaryTarget);
        ResetPendingAttack();
    }

    private void CapturePendingAttackTopology()
    {
        ResetPendingAttack();
        pendingWindupAdmissionTime = Time.time;
        pendingAdditionalAttackEntities = GetMultiShellsAdditionalAttackEntities();
        pendingInitialShellCount = pendingAdditionalAttackEntities != null
            ? 1 + pendingAdditionalAttackEntities.Count
            : 1;
    }

    private void CaptureReleaseTargetsAndPositions(
        MonsterBehaviour firstTarget,
        Vector3 selectionOrigin)
    {
        pendingTargets.Clear();
        pendingTargetPositions.Clear();
        pendingTargets.Add(firstTarget);
        pendingTargetPositions.Add(GetMonsterHitPosition(firstTarget));
        HashSet<MonsterBehaviour> selectedTargets = new HashSet<MonsterBehaviour>
        {
            firstTarget
        };

        while (pendingTargetPositions.Count < pendingInitialShellCount)
        {
            MonsterBehaviour additionalTarget = SelectTargetFromCandidates(
                releaseCandidates,
                selectionOrigin,
                selectedTargets);

            if (!IsValidTarget(additionalTarget))
            {
                break;
            }

            selectedTargets.Add(additionalTarget);
            pendingTargets.Add(additionalTarget);
            pendingTargetPositions.Add(GetMonsterHitPosition(additionalTarget));
        }
    }

    private ProjectileRuntimeOptions CreateRuntimeOptions(bool isAdditional)
    {
        TowerUpgradeDefinition explosiveShellSourceUpgrade = null;
        EffectDefinition explosiveShellEffect = null;
        float bounceSearchRadius = 0f;
        int remainingBounceCount = 0;
        float bounceArcHeight = 0f;
        float bounceDamageScale = 0f;
        TargetSelectionType bounceTargetSelectionType = TargetSelectionType.Nearest;

        if (HasBehaviourPackage(TowerBehaviourPackageType.CannonExplosiveShell) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.CannonExplosiveShell,
                out TowerUpgradeDefinition resolvedExplosiveShellUpgrade))
        {
            explosiveShellSourceUpgrade = resolvedExplosiveShellUpgrade;
            explosiveShellEffect = resolvedExplosiveShellUpgrade.ExplosiveShellEffect;

            if (explosiveShellEffect == null && !hasLoggedInvalidExplosiveShellEffect)
            {
                hasLoggedInvalidExplosiveShellEffect = true;
                Debug.LogWarning(
                    "Cannon Explosive Shell is active, but its EffectDefinition is missing. Explosion gameplay is disabled for this release.",
                    resolvedExplosiveShellUpgrade);
            }
        }

        if (HasBehaviourPackage(TowerBehaviourPackageType.CannonBouncingShell) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.CannonBouncingShell,
                out TowerUpgradeDefinition resolvedBouncingShellUpgrade))
        {
            bounceSearchRadius = resolvedBouncingShellUpgrade.BounceSearchRadius;
            remainingBounceCount = resolvedBouncingShellUpgrade.MaxBounceCount;
            bounceArcHeight = resolvedBouncingShellUpgrade.BounceArcHeight;
            bounceTargetSelectionType = resolvedBouncingShellUpgrade.BounceTargetSelectionType;
            bounceDamageScale = resolvedBouncingShellUpgrade.BounceDamageScale;
        }

        return new ProjectileRuntimeOptions(
            allowsElementalApplication: !isAdditional,
            canPierce: false,
            maxPierceHitCount: 1,
            explosiveShellSourceUpgrade: explosiveShellSourceUpgrade,
            explosiveShellEffect: explosiveShellEffect,
            bounceSearchRadius: bounceSearchRadius,
            remainingBounceCount: remainingBounceCount,
            bounceArcHeight: bounceArcHeight,
            bounceTargetSelectionType: bounceTargetSelectionType,
            bounceDamageScale: bounceDamageScale,
            elementalOpportunityProvenance:
                ElementalOpportunityProvenance.CannonShell,
            elementalOpportunityMemberIdentity: isAdditional
                ? ElementalOpportunityMemberIdentity.Additional
                : ElementalOpportunityMemberIdentity.Primary);
    }

    private AdditionalAttackEntityAuthoring GetMultiShellsAdditionalAttackEntities()
    {
        if (!HasBehaviourPackage(TowerBehaviourPackageType.CannonMultiShells))
        {
            return null;
        }

        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.CannonMultiShells,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.AdditionalAttackEntities
            : null;
    }

    private void ResetPendingAttack()
    {
        pendingPrimaryTarget = null;
        pendingAdditionalAttackEntities = null;
        pendingInitialShellCount = 0;
        pendingWindupAdmissionTime = 0f;
        releaseCandidates.Clear();
        pendingTargets.Clear();
        pendingTargetPositions.Clear();
        SetIdle();
    }
}
