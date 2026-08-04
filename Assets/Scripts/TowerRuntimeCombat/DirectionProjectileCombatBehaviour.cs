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
    private readonly List<MonsterBehaviour> huntingCandidates =
        new List<MonsterBehaviour>();

    private MonsterBehaviour pendingProjectileTarget;
    private Vector3 pendingProjectileTargetPosition;
    private long nextReleaseGroupId = 1;
    private long pendingReleaseGroupId;
    private int pendingSlotCount;
    private int cachedPiercingMaximum = 1;
    private bool pendingIsScatter;

    public override TowerFamily SupportedTowerFamily => TowerFamily.Archer;
    public ProjectileBehaviour ProjectilePrefab => projectilePrefab;

    protected override TowerCombatBaseStats CreateBaseStats()
    {
        return new TowerCombatBaseStats(BaseAttackRange, BaseAttackInterval);
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

        if (!IsCooldownReady)
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
            case TowerBehaviourPackageType.ArcherHuntingArrow:
                ReconcileActiveHuntingGroups();
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
        pendingSlotCount = pendingIsScatter ? ArcherSlotCount : 1;

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

        HashSet<MonsterBehaviour> selectedTargets = new HashSet<MonsterBehaviour>
        {
            firstTarget
        };

        for (int slotIndex = 1; slotIndex < pendingSlotCount; slotIndex++)
        {
            MonsterBehaviour candidate = SelectTarget(selectedTargets);

            if (!IsValidTarget(candidate))
            {
                break;
            }

            pendingCandidateTargets[slotIndex] = candidate;
            selectedTargets.Add(candidate);
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

        bool isHunting = IsHuntingArrowActive();
        ProjectileRuntimeOptions runtimeOptions = CreateRuntimeOptions(
            origin.position,
            resolvedStats.AttackRange);
        int releasedCount = 0;

        for (int slotIndex = 0; slotIndex < pendingSlotCount; slotIndex++)
        {
            if (TryReleasePendingSlot(
                    slotIndex,
                    origin,
                    resolvedStats,
                    runtimeOptions,
                    isHunting))
            {
                releasedCount++;
            }
        }

        if (releasedCount <= 0)
        {
            ResetPendingAttack();
            return;
        }

        StartAttackCooldown(resolvedStats.AttackInterval);
        PlayAttackReleaseVfx(GetReleaseVfxRotation(origin));
        RaiseProjectileReleased(pendingProjectileTarget);
        ResetPendingAttack();
    }

    private bool TryReleasePendingSlot(
        int slotIndex,
        Transform origin,
        ResolvedTowerCombatStats resolvedStats,
        ProjectileRuntimeOptions runtimeOptions,
        bool isHunting)
    {
        ArcherProjectileSlot slot = (ArcherProjectileSlot)slotIndex;
        ArcherProjectileReleaseIdentity releaseIdentity =
            new ArcherProjectileReleaseIdentity(pendingReleaseGroupId, slot);
        MonsterBehaviour candidate = pendingCandidateTargets[slotIndex];

        if (isHunting &&
            IsCapturedCandidateValid(candidate, origin.position, resolvedStats.AttackRange))
        {
            return TryReleaseProjectile(
                projectilePrefab,
                origin,
                GetMonsterHitPosition(candidate),
                candidate,
                resolvedStats.AttackDamage,
                ProjectileFlightType.Tracking,
                initialArcHeight: 0f,
                runtimeOptions: runtimeOptions,
                archerReleaseIdentity: releaseIdentity,
                huntingOpportunityConsumed: true);
        }

        Vector3 fallbackDirection = pendingFallbackDirections[slotIndex];

        if (fallbackDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        return TryReleaseProjectile(
            projectilePrefab,
            origin,
            origin.position + fallbackDirection.normalized,
            pendingProjectileTarget,
            resolvedStats.AttackDamage,
            ProjectileFlightType.Direction,
            initialArcHeight: 0f,
            runtimeOptions: runtimeOptions,
            archerReleaseIdentity: releaseIdentity,
            huntingOpportunityConsumed: isHunting);
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

    private void ReconcileActiveHuntingGroups()
    {
        List<ProjectileBehaviour> projectileSnapshot = GetOwnedProjectileSnapshot();
        SortedDictionary<long, List<ProjectileBehaviour>> groups =
            new SortedDictionary<long, List<ProjectileBehaviour>>();

        for (int i = 0; i < projectileSnapshot.Count; i++)
        {
            ProjectileBehaviour projectile = projectileSnapshot[i];

            if (projectile == null ||
                !projectile.IsActiveForRefresh ||
                !projectile.ArcherReleaseIdentity.IsValid)
            {
                continue;
            }

            long releaseGroupId = projectile.ArcherReleaseIdentity.ReleaseGroupId;

            if (!groups.TryGetValue(releaseGroupId, out List<ProjectileBehaviour> members))
            {
                members = new List<ProjectileBehaviour>();
                groups.Add(releaseGroupId, members);
            }

            members.Add(projectile);
        }

        float trackingRange = ResolveCombatStats().AttackRange;

        foreach (KeyValuePair<long, List<ProjectileBehaviour>> pair in groups)
        {
            List<ProjectileBehaviour> members = pair.Value;
            members.Sort((left, right) =>
                left.ArcherReleaseIdentity.Slot.CompareTo(right.ArcherReleaseIdentity.Slot));

            Vector3 trackingOrigin = members[0].TrackingRangeOrigin;
            CollectTargetCandidates(huntingCandidates, trackingOrigin, trackingRange);
            HashSet<MonsterBehaviour> assignedTargets = new HashSet<MonsterBehaviour>();

            for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
            {
                ProjectileBehaviour member = members[memberIndex];

                if (!member.TryConsumeHuntingOpportunity())
                {
                    continue;
                }

                MonsterBehaviour target = SelectTargetFromCandidates(
                    huntingCandidates,
                    trackingOrigin,
                    assignedTargets);

                if (target == null)
                {
                    continue;
                }

                if (member.TryConvertToTracking(target, trackingOrigin, trackingRange))
                {
                    assignedTargets.Add(target);
                }
            }
        }
    }

    private ProjectileRuntimeOptions CreateRuntimeOptions(
        Vector3 trackingRangeOrigin,
        float trackingRange)
    {
        bool canPierce = IsPiercingArrowActive();
        return new ProjectileRuntimeOptions(
            canPierce,
            canPierce ? cachedPiercingMaximum : 1,
            trackingRangeOrigin: trackingRangeOrigin,
            trackingRange: trackingRange);
    }

    private bool IsPiercingArrowActive()
    {
        return HasBehaviourPackage(TowerBehaviourPackageType.ArcherPiercingArrow);
    }

    private bool IsScatterArrowActive()
    {
        return HasBehaviourPackage(TowerBehaviourPackageType.ArcherScatterArrow);
    }

    private bool IsHuntingArrowActive()
    {
        return HasBehaviourPackage(TowerBehaviourPackageType.ArcherHuntingArrow);
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
        Array.Clear(pendingCandidateTargets, 0, pendingCandidateTargets.Length);
        Array.Clear(pendingFallbackDirections, 0, pendingFallbackDirections.Length);
        SetIdle();
    }
}
