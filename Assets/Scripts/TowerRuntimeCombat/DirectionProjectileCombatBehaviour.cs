using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum ArcherProjectileSlot
{
    Center = 0,
    Left = 1,
    Right = 2
}

public readonly struct ArcherProjectileReleaseIdentity
{
    public ArcherProjectileReleaseIdentity(long releaseGroupId, ArcherProjectileSlot slot)
    {
        ReleaseGroupId = releaseGroupId;
        Slot = slot;
    }

    public long ReleaseGroupId { get; }
    public ArcherProjectileSlot Slot { get; }
    public bool IsValid => ReleaseGroupId > 0;
}

public sealed class DirectionProjectileCombatBehaviour : TowerCombatBehaviour
{
    private const int DefaultPiercingArrowMaxHitCount = 3;
    private const float DefaultScatterArrowAngleOffset = 15f;
    private const int ArcherSlotCount = 3;

    [TitleGroup("Projectile")]
    [Required]
    [SerializeField] private ProjectileBehaviour projectilePrefab;

    private readonly MonsterBehaviour[] pendingCandidateTargets =
        new MonsterBehaviour[ArcherSlotCount];
    private readonly Vector3[] pendingFallbackDirections =
        new Vector3[ArcherSlotCount];
    private MonsterBehaviour pendingProjectileTarget;
    private Vector3 pendingProjectileTargetPosition;
    private long nextReleaseGroupId = 1;
    private long pendingReleaseGroupId;
    private int pendingSlotCount;
    private int cachedPiercingMaximum = 1;
    private bool pendingIsScatter;
    private AdditionalAttackEntityAuthoring pendingAdditionalAttackEntities;

    public override TowerFamily SupportedTowerFamily => TowerFamily.Archer;
    public ProjectileBehaviour ProjectilePrefab => projectilePrefab;

    protected override TowerCombatBaseStats CreateBaseStats()
    {
        return new TowerCombatBaseStats(
            BaseAttackDamage,
            BaseAttackRange,
            BaseAttackCycleDuration);
    }

    protected override bool IsSubtypeConfigurationValid()
    {
        if (projectilePrefab != null && projectilePrefab.IsValid())
        {
            return true;
        }

        Debug.LogWarning(
            "Direction projectile combat is invalid: projectile prefab or behaviour authoring is missing.",
            this);
        return false;
    }

    protected override void OnResolvedBaselineEstablished(ResolvedTowerCombatStats _)
    {
        cachedPiercingMaximum = ResolvePiercingMaximum();
    }

    protected override void OnCombatInitialized()
    {
        nextReleaseGroupId = 1;
        ResetPendingAttack();
    }

    protected override void OnCombatUpdate()
    {
        if (IsWaitingForAnimationRelease)
        {
            if (!IsValidTarget(pendingProjectileTarget) ||
                !IsInAttackRange(pendingProjectileTarget))
            {
                ResetPendingAttack();
            }

            return;
        }

        if (!IsAttackCycleReady)
        {
            return;
        }

        MonsterBehaviour target = SelectTarget();
        SetCurrentTarget(target);

        if (!IsValidTarget(target))
        {
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            return;
        }

        CapturePendingAttack(target, origin);
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

        switch (upgradeDefinition.BehaviourPackageType)
        {
            case TowerBehaviourPackageType.ArcherPiercingArrow:
                RefreshActivePiercing(upgradeDefinition.PiercingMaxHitCount);
                break;
        }
    }

    private void CapturePendingAttack(MonsterBehaviour firstTarget, Transform origin)
    {
        ResetPendingAttack();
        pendingProjectileTarget = firstTarget;
        pendingProjectileTargetPosition = GetMonsterHitPosition(firstTarget);
        pendingReleaseGroupId = nextReleaseGroupId++;
        pendingIsScatter = IsScatterArrowActive();
        pendingAdditionalAttackEntities = pendingIsScatter
            ? GetScatterAdditionalAttackEntities()
            : null;
        pendingSlotCount = pendingAdditionalAttackEntities != null
            ? Mathf.Min(ArcherSlotCount, 1 + pendingAdditionalAttackEntities.Count)
            : 1;

        Vector3 centerDirection = pendingProjectileTargetPosition - origin.position;

        if (centerDirection.sqrMagnitude <= 0.0001f)
        {
            centerDirection = origin.forward;
        }

        centerDirection.Normalize();
        float scatterAngle = pendingIsScatter ? GetScatterArrowAngleOffset() : 0f;
        pendingFallbackDirections[(int)ArcherProjectileSlot.Center] = centerDirection;
        pendingFallbackDirections[(int)ArcherProjectileSlot.Left] =
            Quaternion.AngleAxis(-scatterAngle, Vector3.up) * centerDirection;
        pendingFallbackDirections[(int)ArcherProjectileSlot.Right] =
            Quaternion.AngleAxis(scatterAngle, Vector3.up) * centerDirection;
        pendingCandidateTargets[(int)ArcherProjectileSlot.Center] = firstTarget;

        if (!pendingIsScatter)
        {
            return;
        }

    }

    private void ReleasePendingAttack()
    {
        if (!IsWaitingForAnimationRelease ||
            projectilePrefab == null ||
            pendingReleaseGroupId <= 0 ||
            pendingSlotCount <= 0)
        {
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null)
        {
            ResetPendingAttack();
            return;
        }

        ResolvedTowerCombatStats resolvedStats = ResolveCombatStats();

        if (!IsCapturedCandidateValid(
                pendingCandidateTargets[(int)ArcherProjectileSlot.Center],
                origin.position,
                resolvedStats.AttackRange))
        {
            ResetPendingAttack();
            return;
        }

        int releasedCount = 0;

        for (int slotIndex = 0; slotIndex < pendingSlotCount; slotIndex++)
        {
            if (TryReleasePendingSlot(
                    slotIndex,
                    origin,
                    resolvedStats))
            {
                releasedCount++;
            }
        }

        if (releasedCount <= 0)
        {
            ResetPendingAttack();
            return;
        }

        StartAttackCycle(resolvedStats.AttackCycleDuration);
        PlayAttackReleaseVfx(GetReleaseVfxRotation(origin));
        RaiseProjectileReleased(pendingProjectileTarget);
        ResetPendingAttack();
    }

    private bool TryReleasePendingSlot(
        int slotIndex,
        Transform origin,
        ResolvedTowerCombatStats resolvedStats)
    {
        ArcherProjectileSlot slot = (ArcherProjectileSlot)slotIndex;
        ArcherProjectileReleaseIdentity releaseIdentity =
            new ArcherProjectileReleaseIdentity(pendingReleaseGroupId, slot);
        Vector3 fallbackDirection = pendingFallbackDirections[slotIndex];

        if (fallbackDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        bool isAdditional = slot != ArcherProjectileSlot.Center;
        ProjectileBehaviour releasePrefab = projectilePrefab;

        if (isAdditional &&
            (pendingAdditionalAttackEntities == null ||
             pendingAdditionalAttackEntities.Prefab == null ||
             !pendingAdditionalAttackEntities.Prefab.TryGetComponent(out releasePrefab)))
        {
            return false;
        }

        int releaseDamage = isAdditional
            ? Mathf.Max(0, pendingAdditionalAttackEntities.BasicDamage + resolvedStats.DamageBonus)
            : resolvedStats.AttackDamage;

        return TryReleaseProjectile(
            releasePrefab,
            origin,
            origin.position + fallbackDirection.normalized,
            pendingProjectileTarget,
            releaseDamage,
            ProjectileFlightType.Direction,
            initialArcHeight: 0f,
            runtimeOptions: CreateRuntimeOptions(isAdditional),
            archerReleaseIdentity: releaseIdentity);
    }

    private bool IsCapturedCandidateValid(
        MonsterBehaviour candidate,
        Vector3 rangeOrigin,
        float attackRange)
    {
        return IsRegisteredGameplayTarget(candidate) &&
               candidate.IsGameplayTargetable &&
               IsInRange(rangeOrigin, GetMonsterHitPosition(candidate), attackRange);
    }

    private void RefreshActivePiercing(int newResolvedMaximum)
    {
        int clampedMaximum = Mathf.Max(1, newResolvedMaximum);
        int capacityDelta = clampedMaximum - cachedPiercingMaximum;
        cachedPiercingMaximum = clampedMaximum;

        if (capacityDelta == 0)
        {
            return;
        }

        List<ProjectileBehaviour> projectileSnapshot = GetOwnedProjectileSnapshot();

        for (int i = 0; i < projectileSnapshot.Count; i++)
        {
            ProjectileBehaviour projectile = projectileSnapshot[i];

            if (projectile != null && projectile.ArcherReleaseIdentity.IsValid)
            {
                projectile.TryAddPiercingCapacity(capacityDelta);
            }
        }
    }

    private ProjectileRuntimeOptions CreateRuntimeOptions(bool locksDirectDamage)
    {
        bool canPierce = IsPiercingArrowActive();
        TowerUpgradeDefinition explosiveArrowSourceUpgrade = null;
        EffectDefinition explosiveArrowEffect = null;

        if (HasBehaviourPackage(TowerBehaviourPackageType.ArcherExplosiveArrow) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.ArcherExplosiveArrow,
                out TowerUpgradeDefinition resolvedExplosiveArrowUpgrade))
        {
            explosiveArrowSourceUpgrade = resolvedExplosiveArrowUpgrade;
            explosiveArrowEffect = resolvedExplosiveArrowUpgrade.ExplosiveArrowEffect;
        }

        return new ProjectileRuntimeOptions(
            canPierce,
            canPierce ? cachedPiercingMaximum : 1,
            locksDirectDamage: locksDirectDamage,
            explosiveArrowSourceUpgrade: explosiveArrowSourceUpgrade,
            explosiveArrowEffect: explosiveArrowEffect);
    }

    private bool IsPiercingArrowActive()
    {
        return HasBehaviourPackage(TowerBehaviourPackageType.ArcherPiercingArrow);
    }

    private bool IsScatterArrowActive()
    {
        return HasBehaviourPackage(TowerBehaviourPackageType.ArcherScatterArrow);
    }

    private int ResolvePiercingMaximum()
    {
        if (!IsPiercingArrowActive())
        {
            return 1;
        }

        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.ArcherPiercingArrow,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.PiercingMaxHitCount
            : DefaultPiercingArrowMaxHitCount;
    }

    private float GetScatterArrowAngleOffset()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.ArcherScatterArrow,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.ScatterAngleOffset
            : DefaultScatterArrowAngleOffset;
    }

    private AdditionalAttackEntityAuthoring GetScatterAdditionalAttackEntities()
    {
        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.ArcherScatterArrow,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.AdditionalAttackEntities
            : null;
    }

    private Quaternion GetReleaseVfxRotation(Transform origin)
    {
        Vector3 direction = pendingProjectileTargetPosition - origin.position;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
    }

    private void ResetPendingAttack()
    {
        pendingProjectileTarget = null;
        pendingProjectileTargetPosition = Vector3.zero;
        pendingReleaseGroupId = 0;
        pendingSlotCount = 0;
        pendingIsScatter = false;
        pendingAdditionalAttackEntities = null;
        Array.Clear(pendingCandidateTargets, 0, pendingCandidateTargets.Length);
        Array.Clear(pendingFallbackDirections, 0, pendingFallbackDirections.Length);
        SetIdle();
    }
}
