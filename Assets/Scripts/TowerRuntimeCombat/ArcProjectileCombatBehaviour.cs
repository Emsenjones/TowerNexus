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

    private readonly List<Vector3> pendingTargetPositions = new List<Vector3>();
    private MonsterBehaviour pendingPrimaryTarget;
    private bool hasLoggedInvalidExplosiveShellEffect;

    public override TowerFamily SupportedTowerFamily => TowerFamily.Cannon;
    public ProjectileBehaviour ProjectilePrefab => projectilePrefab;
    public float ArcHeight => arcHeight;

    protected override TowerCombatBaseStats CreateBaseStats()
    {
        return new TowerCombatBaseStats(BaseAttackRange, BaseAttackInterval);
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

        if (!IsCooldownReady || projectilePrefab == null)
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

        pendingPrimaryTarget = target;
        CaptureTargetPositions(target);
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

    protected override void OnBehaviourPackageRecorded(TowerUpgradeDefinition upgradeDefinition)
    {
        if (upgradeDefinition == null)
        {
            return;
        }

        List<ProjectileBehaviour> projectileSnapshot = GetOwnedProjectileSnapshot();

        switch (upgradeDefinition.BehaviourPackageType)
        {
            case TowerBehaviourPackageType.CannonExplosiveShell:
                for (int i = 0; i < projectileSnapshot.Count; i++)
                {
                    ProjectileBehaviour projectile = projectileSnapshot[i];

                    if (projectile != null)
                    {
                        projectile.TryRefreshExplosiveShell(
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
                        projectile.TryEnableInitialBouncingShell(
                            upgradeDefinition.BounceSearchRadius,
                            upgradeDefinition.MaxBounceCount,
                            upgradeDefinition.BounceArcHeight,
                            upgradeDefinition.BounceTargetSelectionType);
                    }
                }
                break;
        }
    }

    private void ReleasePendingAttack()
    {
        if (!IsWaitingForAnimationRelease ||
            projectilePrefab == null ||
            pendingTargetPositions.Count == 0)
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
        ProjectileRuntimeOptions runtimeOptions = CreateRuntimeOptions();
        int releasedCount = 0;

        for (int i = 0; i < pendingTargetPositions.Count; i++)
        {
            if (TryReleaseProjectile(
                    projectilePrefab,
                    origin,
                    pendingTargetPositions[i],
                    target: null,
                    resolvedStats.AttackDamage,
                    ProjectileFlightType.Arc,
                    arcHeight,
                    runtimeOptions))
            {
                releasedCount++;
            }
        }

        if (releasedCount == 0)
        {
            ResetPendingAttack();
            return;
        }

        StartAttackCooldown(resolvedStats.AttackInterval);
        PlayAttackReleaseVfx(Quaternion.identity);
        RaiseProjectileReleased(pendingPrimaryTarget);
        ResetPendingAttack();
    }

    private void CaptureTargetPositions(MonsterBehaviour firstTarget)
    {
        pendingTargetPositions.Clear();
        pendingTargetPositions.Add(GetMonsterHitPosition(firstTarget));

        int maximumInitialShellCount = ResolveMaximumInitialShellCount();
        HashSet<MonsterBehaviour> selectedTargets = new HashSet<MonsterBehaviour>
        {
            firstTarget
        };

        while (pendingTargetPositions.Count < maximumInitialShellCount)
        {
            MonsterBehaviour additionalTarget = SelectTarget(selectedTargets);

            if (!IsValidTarget(additionalTarget))
            {
                break;
            }

            selectedTargets.Add(additionalTarget);
            pendingTargetPositions.Add(GetMonsterHitPosition(additionalTarget));
        }
    }

    private ProjectileRuntimeOptions CreateRuntimeOptions()
    {
        TowerUpgradeDefinition explosiveShellSourceUpgrade = null;
        EffectDefinition explosiveShellEffect = null;
        float bounceSearchRadius = 0f;
        int remainingBounceCount = 0;
        float bounceArcHeight = 0f;
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
        }

        return new ProjectileRuntimeOptions(
            canPierce: false,
            maxPierceHitCount: 1,
            explosiveShellSourceUpgrade: explosiveShellSourceUpgrade,
            explosiveShellEffect: explosiveShellEffect,
            bounceSearchRadius: bounceSearchRadius,
            remainingBounceCount: remainingBounceCount,
            bounceArcHeight: bounceArcHeight,
            bounceTargetSelectionType: bounceTargetSelectionType);
    }

    private int ResolveMaximumInitialShellCount()
    {
        if (!HasBehaviourPackage(TowerBehaviourPackageType.CannonMultiShells))
        {
            return 1;
        }

        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.CannonMultiShells,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.MultiShellsMaxInitialShellCount
            : 1;
    }

    private void ResetPendingAttack()
    {
        pendingPrimaryTarget = null;
        pendingTargetPositions.Clear();
        SetIdle();
    }
}
