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

    private readonly List<MonsterBehaviour> releaseCandidates =
        new List<MonsterBehaviour>();
    private readonly Vector3[] pendingFallbackDirections =
        new Vector3[ArcherSlotCount];
    private MonsterBehaviour pendingProjectileTarget;
    private Vector3 pendingProjectileTargetPosition;
    private long nextReleaseGroupId = 1;
    private long pendingReleaseGroupId;
    private int pendingSlotCount;
    private int cachedPiercingMaximum = 1;
    private float pendingScatterAngleOffset;
    private AdditionalAttackEntityAuthoring pendingAdditionalAttackEntities;

    public override TowerFamily SupportedTowerFamily => TowerFamily.Archer;
    public ProjectileBehaviour ProjectilePrefab => projectilePrefab;

    protected override TowerCombatBaseStats CreateBaseStats()
    {
        return new TowerCombatBaseStats(
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

    private void CapturePendingAttackTopology()
    {
        ResetPendingAttack();
        pendingReleaseGroupId = nextReleaseGroupId++;
        bool isScatter = IsScatterArrowActive();
        pendingAdditionalAttackEntities = isScatter
            ? GetScatterAdditionalAttackEntities()
            : null;
        pendingScatterAngleOffset = isScatter
            ? GetScatterArrowAngleOffset()
            : 0f;
        pendingSlotCount = pendingAdditionalAttackEntities != null
            ? Mathf.Min(ArcherSlotCount, 1 + pendingAdditionalAttackEntities.Count)
            : 1;
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

        pendingProjectileTarget = releaseTarget;
        pendingProjectileTargetPosition = GetMonsterHitPosition(releaseTarget);
        SetCurrentTarget(releaseTarget);
        CaptureReleaseDirections(origin);

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

    private void CaptureReleaseDirections(Transform origin)
    {
        Vector3 centerDirection = pendingProjectileTargetPosition - origin.position;

        if (centerDirection.sqrMagnitude <= 0.0001f)
        {
            centerDirection = origin.forward;
        }

        centerDirection.Normalize();
        pendingFallbackDirections[(int)ArcherProjectileSlot.Center] = centerDirection;
        pendingFallbackDirections[(int)ArcherProjectileSlot.Left] =
            Quaternion.AngleAxis(-pendingScatterAngleOffset, Vector3.up) * centerDirection;
        pendingFallbackDirections[(int)ArcherProjectileSlot.Right] =
            Quaternion.AngleAxis(pendingScatterAngleOffset, Vector3.up) * centerDirection;
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

        float damageScale = isAdditional
            ? pendingAdditionalAttackEntities.DamageScale
            : 1f;
        TowerDamageSourceIdentity damageSourceIdentity = isAdditional
            ? TowerDamageSourceIdentity.AdditionalDirect
            : TowerDamageSourceIdentity.PrimaryDirect;

        return TryReleaseProjectile(
            releasePrefab,
            origin,
            origin.position + fallbackDirection.normalized,
            pendingProjectileTarget,
            damageScale,
            damageSourceIdentity,
            ProjectileFlightType.Direction,
            initialArcHeight: 0f,
            runtimeOptions: CreateRuntimeOptions(),
            archerReleaseIdentity: releaseIdentity);
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

    private ProjectileRuntimeOptions CreateRuntimeOptions()
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
        pendingScatterAngleOffset = 0f;
        pendingAdditionalAttackEntities = null;
        releaseCandidates.Clear();
        System.Array.Clear(pendingFallbackDirections, 0, pendingFallbackDirections.Length);
        SetIdle();
    }
}
