using Sirenix.OdinInspector;
using UnityEngine;

public sealed class MagicOrbCombatBehaviour : TowerCombatBehaviour
{
    [TitleGroup("Magic Orb")]
    [Required]
    [SerializeField] private GameObject magicOrbPrefab;

    private MagicOrbGroupRuntime activeMagicOrbGroup;
    private MagicArcaneFieldBehaviour activeMagicArcaneField;
    private MonsterBehaviour pendingMagicTarget;
    private long nextReleaseGroupId = 1;
    private bool hadGroupAtFrameStart;
    private bool hasLoggedMissingMagicOrbPrefab;
    private bool hasLoggedInvalidArcaneDetonationEffect;
    private bool hasLoggedMissingMagicArcaneFieldPrefab;
    private bool hasLoggedInvalidMagicArcaneFieldPrefab;

    public override TowerFamily SupportedTowerFamily => TowerFamily.Magic;

    protected override TowerCombatBaseStats CreateBaseStats()
    {
        float rotationSpeed = 0f;

        if (TryGetMagicOrbPrefabBehaviour(out MagicOrbBehaviour orbBehaviour))
        {
            rotationSpeed = orbBehaviour.BaseRotationSpeed;
        }

        return new TowerCombatBaseStats(
            BaseAttackRange,
            BaseAttackCycleDuration,
            rotationSpeed);
    }

    protected override bool IsSubtypeConfigurationValid()
    {
        if (magicOrbPrefab == null)
        {
            Debug.LogWarning("Magic Orb combat is invalid: Magic Orb prefab is missing.", this);
            return false;
        }

        if (!TryGetMagicOrbPrefabBehaviour(out MagicOrbBehaviour orbBehaviour))
        {
            Debug.LogWarning(
                "Magic Orb combat is invalid: prefab root is missing MagicOrbBehaviour.",
                this);
            return false;
        }

        if (!orbBehaviour.IsAuthoredConfigurationValid())
        {
            Debug.LogWarning(
                "Magic Orb combat is invalid: prefab MagicOrbBehaviour has invalid authored data.",
                this);
            return false;
        }

        if (BaseAttackCycleDuration < orbBehaviour.BaseMaxLifetime)
        {
            Debug.LogWarning(
                "Magic Orb combat is invalid: attack cycle duration cannot be shorter than the Orb max lifetime.",
                this);
            return false;
        }

        return true;
    }

    protected override void OnCombatInitialized()
    {
        nextReleaseGroupId = 1;
        hadGroupAtFrameStart = false;
        pendingMagicTarget = null;
        hasLoggedMissingMagicOrbPrefab = false;
        hasLoggedInvalidArcaneDetonationEffect = false;
        hasLoggedMissingMagicArcaneFieldPrefab = false;
        hasLoggedInvalidMagicArcaneFieldPrefab = false;
        EnsureArcaneFieldExists();
    }

    protected override void OnCombatEnabled()
    {
        EnsureArcaneFieldExists();
    }

    protected override void OnOwnedRuntimeUpdate()
    {
        hadGroupAtFrameStart = activeMagicOrbGroup != null && activeMagicOrbGroup.IsActive;

        if (hadGroupAtFrameStart)
        {
            activeMagicOrbGroup.Tick(Time.deltaTime);
        }
    }

    protected override void OnCombatCleanup()
    {
        ResetPendingAttack();
        CleanupMagicArcaneField();
        ForceCleanupActiveMagicOrbGroup();
        hadGroupAtFrameStart = false;
    }

    protected override void OnResolvedStatsChanged(
        ResolvedTowerCombatStats previousStats,
        ResolvedTowerCombatStats currentStats,
        TowerUpgradeDefinition sourceUpgrade)
    {
        MagicOrbGroupRuntime group = activeMagicOrbGroup;

        if (group == null || !group.IsActive)
        {
            return;
        }

        bool refreshRotationSpeed = UpgradeIncludesBasicStat(
            sourceUpgrade,
            TowerUpgradeBasicStatType.MagicOrbRotationSpeed);
        group.ApplyStatRefresh(new MagicOrbStatRefresh(
            refreshRotationSpeed,
            currentStats.MagicOrbRotationSpeed));
    }

    protected override RequiredUpgradeRefreshResult RefreshBehaviourPackage(
        TowerUpgradeDefinition upgradeDefinition, out string failureReason)
    {
        failureReason = string.Empty;
        RequiredUpgradeRefreshResult result = RequiredUpgradeRefreshResult.NotRequired;
        switch (upgradeDefinition.BehaviourPackageType)
        {
            case TowerBehaviourPackageType.MagicMultiOrbs:
                result = ReconcileActiveMultiOrbs(upgradeDefinition.AdditionalAttackEntities);
                break;
            case TowerBehaviourPackageType.MagicArcaneDetonation:
                if (activeMagicOrbGroup != null)
                {
                    activeMagicOrbGroup.EnableArcaneDetonation(
                        upgradeDefinition, upgradeDefinition.ArcaneDetonationEffect);
                    result = RequiredUpgradeRefreshResult.Applied;
                }
                break;
            case TowerBehaviourPackageType.MagicArcaneField:
                result = EnsureArcaneFieldExists(requireActiveBattle: true);
                break;
        }
        if (result == RequiredUpgradeRefreshResult.TechnicalFailure)
            failureReason = "Required Magic upgrade runtime could not be created or initialized.";
        return result;
    }

    protected override void OnCombatUpdate()
    {
        if (IsWaitingForAnimationRelease)
        {
            return;
        }

        if (hadGroupAtFrameStart || activeMagicOrbGroup != null || !IsAttackCycleReady)
        {
            SetIdle();
            return;
        }

        MonsterBehaviour target = SelectTarget();
        SetCurrentTarget(target);

        if (!IsValidTarget(target))
        {
            SetIdle();
            return;
        }

        pendingMagicTarget = target;
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

    private void ReleasePendingAttack()
    {
        if (!IsWaitingForAnimationRelease)
        {
            return;
        }

        if (activeMagicOrbGroup != null ||
            !IsRegisteredGameplayTarget(pendingMagicTarget) ||
            !IsInAttackRange(pendingMagicTarget))
        {
            ResetPendingAttack();
            return;
        }

        if (magicOrbPrefab == null)
        {
            if (!hasLoggedMissingMagicOrbPrefab)
            {
                hasLoggedMissingMagicOrbPrefab = true;
                Debug.LogWarning("Magic Orb combat cannot spawn: prefab is not assigned.", this);
            }

            ResetPendingAttack();
            return;
        }

        Transform origin = GetAttackOrigin();

        if (origin == null || !TryGetMagicOrbPrefabBehaviour(out MagicOrbBehaviour authoredOrb))
        {
            ResetPendingAttack();
            return;
        }

        ResolvedTowerCombatStats resolvedStats = ResolveCombatStats();
        MagicOrbRuntimeOptions runtimeOptions = CreateRuntimeOptions();
        AdditionalAttackEntityAuthoring additionalAttackEntities =
            GetMultiOrbsAdditionalAttackEntities();
        int memberCount = 1 + (additionalAttackEntities != null
            ? additionalAttackEntities.Count
            : 0);

        if (!TryCreateMagicOrbGroup(
                origin.position,
                authoredOrb,
                memberCount,
                resolvedStats,
                runtimeOptions,
                additionalAttackEntities))
        {
            ResetPendingAttack();
            return;
        }

        StartAttackCycle(resolvedStats.AttackCycleDuration);
        PlayAttackReleaseVfx(Quaternion.identity);
        ResetPendingAttack();
    }

    private bool TryCreateMagicOrbGroup(
        Vector3 orbitCenterPosition,
        MagicOrbBehaviour authoredOrb,
        int memberCount,
        ResolvedTowerCombatStats resolvedStats,
        MagicOrbRuntimeOptions runtimeOptions,
        AdditionalAttackEntityAuthoring additionalAttackEntities)
    {
        if (memberCount <= 0 || activeMagicOrbGroup != null)
        {
            return false;
        }

        MagicOrbGroupRuntime group = new MagicOrbGroupRuntime(
            nextReleaseGroupId++,
            TowerInstance,
            MonsterManager,
            runtimeOptions,
            orbitCenterPosition,
            Random.Range(0f, 360f),
            resolvedStats,
            authoredOrb);

        if (!group.CanInitialize())
        {
            group.ForceCleanup();
            return false;
        }

        for (int i = 0; i < memberCount; i++)
        {
            bool isAdditional = i > 0;
            GameObject memberPrefab = isAdditional
                ? additionalAttackEntities?.Prefab
                : magicOrbPrefab;

            if (memberPrefab == null)
            {
                group.ForceCleanup();
                return false;
            }

            GameObject orbObject = Instantiate(
                memberPrefab,
                orbitCenterPosition,
                Quaternion.identity);
            float memberDamageScale = isAdditional
                ? additionalAttackEntities.DamageScale
                : 1f;

            if (!orbObject.TryGetComponent(out MagicOrbBehaviour member) ||
                !group.TryAddMember(member, i, memberCount, memberDamageScale))
            {
                Debug.LogWarning(
                    "Magic Orb group release failed: every instance root requires a valid MagicOrbBehaviour.",
                    orbObject);
                Destroy(orbObject);
                group.ForceCleanup();
                return false;
            }
        }

        if (!group.Activate(memberCount))
        {
            return false;
        }

        activeMagicOrbGroup = group;
        activeMagicOrbGroup.OnEnded += HandleMagicOrbGroupEnded;
        return true;
    }

    private RequiredUpgradeRefreshResult ReconcileActiveMultiOrbs(
        AdditionalAttackEntityAuthoring additionalAttackEntities)
    {
        MagicOrbGroupRuntime group = activeMagicOrbGroup;
        int desiredMemberCount = 1 + (additionalAttackEntities != null
            ? additionalAttackEntities.Count
            : 0);

        if (group == null ||
            !group.IsActive ||
            group.MemberCount >= desiredMemberCount)
        {
            return RequiredUpgradeRefreshResult.NotRequired;
        }

        if (additionalAttackEntities?.Prefab == null)
            return RequiredUpgradeRefreshResult.TechnicalFailure;

        int missingMemberCount = desiredMemberCount - group.MemberCount;
        System.Collections.Generic.List<MagicOrbBehaviour> stagedMembers =
            new System.Collections.Generic.List<MagicOrbBehaviour>(missingMemberCount);

        bool committed = false;
        GameObject unownedCandidate = null;
        try
        {
            for (int i = 0; i < missingMemberCount; i++)
            {
                unownedCandidate = Instantiate(additionalAttackEntities.Prefab,
                    group.OrbitCenterPosition, Quaternion.identity);
                unownedCandidate.SetActive(false);
                if (!IsBattleActive || !group.IsActive)
                    return RequiredUpgradeRefreshResult.NotRequired;
                if (!unownedCandidate.TryGetComponent(out MagicOrbBehaviour candidate) ||
                    !candidate.IsAuthoredConfigurationValid())
                    return RequiredUpgradeRefreshResult.TechnicalFailure;
                stagedMembers.Add(candidate);
                unownedCandidate = null;
            }
            committed = group.TryCommitStagedMembers(stagedMembers,
                desiredMemberCount, additionalAttackEntities.DamageScale);
            if (!IsBattleActive) return RequiredUpgradeRefreshResult.NotRequired;
            if (!committed || !group.IsActive) return RequiredUpgradeRefreshResult.TechnicalFailure;
            for (int i = 0; i < stagedMembers.Count; i++)
                if (stagedMembers[i] == null || !stagedMembers[i].IsInitialized)
                    return RequiredUpgradeRefreshResult.TechnicalFailure;
            return RequiredUpgradeRefreshResult.Applied;
        }
        finally
        {
            if (unownedCandidate != null) Destroy(unownedCandidate);
            if (!committed) CleanupStagedMembers(stagedMembers);
        }
    }

    private static void CleanupStagedMembers(
        System.Collections.Generic.IReadOnlyList<MagicOrbBehaviour> stagedMembers)
    {
        for (int i = 0; i < stagedMembers.Count; i++)
        {
            MagicOrbBehaviour candidate = stagedMembers[i];

            if (candidate != null)
            {
                Destroy(candidate.gameObject);
            }
        }
    }

    private bool TryGetMagicOrbPrefabBehaviour(out MagicOrbBehaviour orbBehaviour)
    {
        orbBehaviour = null;
        return magicOrbPrefab != null && magicOrbPrefab.TryGetComponent(out orbBehaviour);
    }

    private MagicOrbRuntimeOptions CreateRuntimeOptions()
    {
        TowerUpgradeDefinition sourceUpgrade = null;
        EffectDefinition detonationEffect = null;

        if (HasBehaviourPackage(TowerBehaviourPackageType.MagicArcaneDetonation) &&
            TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.MagicArcaneDetonation,
                out TowerUpgradeDefinition resolvedUpgrade))
        {
            sourceUpgrade = resolvedUpgrade;
            detonationEffect = resolvedUpgrade.ArcaneDetonationEffect;

            if (detonationEffect == null && !hasLoggedInvalidArcaneDetonationEffect)
            {
                hasLoggedInvalidArcaneDetonationEffect = true;
                Debug.LogWarning(
                    "Magic Arcane Detonation is active, but its EffectDefinition is missing.",
                    resolvedUpgrade);
            }
        }

        return new MagicOrbRuntimeOptions(sourceUpgrade, detonationEffect);
    }

    private void HandleMagicOrbGroupEnded(
        MagicOrbGroupRuntime group,
        bool completedNormally)
    {
        if (group == null)
        {
            return;
        }

        group.OnEnded -= HandleMagicOrbGroupEnded;

        if (activeMagicOrbGroup == group)
        {
            activeMagicOrbGroup = null;

            if (!completedNormally)
            {
                ClearAttackCycle();
            }
        }
    }

    private void ForceCleanupActiveMagicOrbGroup()
    {
        MagicOrbGroupRuntime group = activeMagicOrbGroup;
        activeMagicOrbGroup = null;

        if (group == null)
        {
            return;
        }

        group.OnEnded -= HandleMagicOrbGroupEnded;
        group.ForceCleanup();
    }

    private AdditionalAttackEntityAuthoring GetMultiOrbsAdditionalAttackEntities()
    {
        if (!HasBehaviourPackage(TowerBehaviourPackageType.MagicMultiOrbs))
        {
            return null;
        }

        return TryGetBehaviourPackageUpgrade(
            TowerBehaviourPackageType.MagicMultiOrbs,
            out TowerUpgradeDefinition upgradeDefinition)
            ? upgradeDefinition.AdditionalAttackEntities
            : null;
    }

    private void ResetPendingAttack()
    {
        pendingMagicTarget = null;
        SetIdle();
    }

    private RequiredUpgradeRefreshResult EnsureArcaneFieldExists(bool requireActiveBattle = false)
    {
        if (IsCurrentArcaneField(activeMagicArcaneField))
        {
            return RequiredUpgradeRefreshResult.NotRequired;
        }

        if (activeMagicArcaneField != null)
        {
            activeMagicArcaneField.Cleanup();
            activeMagicArcaneField = null;
        }

        MagicArcaneFieldBehaviour existingField = FindAttachedMagicArcaneField();

        if (IsCurrentArcaneField(existingField))
        {
            activeMagicArcaneField = existingField;
            return RequiredUpgradeRefreshResult.NotRequired;
        }

        if (TowerInstance == null ||
            !TowerInstance.TryGetBehaviourPackageUpgrade(
                TowerBehaviourPackageType.MagicArcaneField,
                out TowerUpgradeDefinition arcaneFieldUpgrade))
        {
            if (existingField != null)
            {
                existingField.Cleanup();
            }

            return RequiredUpgradeRefreshResult.NotRequired;
        }

        MagicArcaneFieldBehaviour field = existingField;

        if (field == null && !TryInstantiateMagicArcaneField(arcaneFieldUpgrade.MagicArcaneFieldPrefab, out field))
        {
            return RequiredUpgradeRefreshResult.TechnicalFailure;
        }

        field.Cleanup();
        if (requireActiveBattle && !IsBattleActive)
            return RequiredUpgradeRefreshResult.NotRequired;

        if (!field.Initialize(TowerInstance, MonsterManager, arcaneFieldUpgrade))
        {
            field.Cleanup();
            activeMagicArcaneField = null;
            return RequiredUpgradeRefreshResult.TechnicalFailure;
        }

        if (requireActiveBattle && !IsBattleActive)
        {
            field.Cleanup();
            return RequiredUpgradeRefreshResult.NotRequired;
        }
        if (!IsCurrentArcaneField(field))
        {
            field.Cleanup();
            return RequiredUpgradeRefreshResult.TechnicalFailure;
        }
        activeMagicArcaneField = field;
        return RequiredUpgradeRefreshResult.Applied;
    }

    private bool IsCurrentArcaneField(MagicArcaneFieldBehaviour field)
    {
        return field != null &&
               field.IsInitialized &&
               field.SourceTower == TowerInstance &&
               field.SourceUpgrade != null &&
               field.SourceUpgrade.BehaviourPackageType == TowerBehaviourPackageType.MagicArcaneField &&
               TowerInstance != null &&
               field.transform != transform &&
               field.transform.IsChildOf(transform) &&
               TowerInstance.HasUpgrade(field.SourceUpgrade);
    }

    private void CleanupMagicArcaneField()
    {
        MagicArcaneFieldBehaviour field = activeMagicArcaneField != null
            ? activeMagicArcaneField
            : FindAttachedMagicArcaneField();

        if (field != null)
        {
            field.Cleanup();
        }

        activeMagicArcaneField = null;
    }

    private MagicArcaneFieldBehaviour FindAttachedMagicArcaneField()
    {
        MagicArcaneFieldBehaviour[] fields =
            GetComponentsInChildren<MagicArcaneFieldBehaviour>(true);
        MagicArcaneFieldBehaviour selectedField = null;

        for (int i = 0; i < fields.Length; i++)
        {
            MagicArcaneFieldBehaviour field = fields[i];

            if (field == null || field.transform == transform)
            {
                continue;
            }

            if (selectedField == null)
            {
                selectedField = field;
            }
            else
            {
                field.Cleanup();
            }
        }

        return selectedField;
    }

    private bool TryInstantiateMagicArcaneField(
        GameObject fieldPrefab,
        out MagicArcaneFieldBehaviour field)
    {
        field = null;

        if (fieldPrefab == null)
        {
            if (!hasLoggedMissingMagicArcaneFieldPrefab)
            {
                hasLoggedMissingMagicArcaneFieldPrefab = true;
                Debug.LogWarning(
                    "Magic Arcane Field cannot activate: the applied upgrade has no runtime prefab.",
                    this);
            }

            return false;
        }

        GameObject fieldObject = Instantiate(fieldPrefab, transform);
        fieldObject.transform.localPosition = Vector3.zero;
        fieldObject.transform.localRotation = Quaternion.identity;

        if (fieldObject.TryGetComponent(out field))
        {
            return true;
        }

        if (!hasLoggedInvalidMagicArcaneFieldPrefab)
        {
            hasLoggedInvalidMagicArcaneFieldPrefab = true;
            Debug.LogWarning(
                "Magic Arcane Field runtime prefab root requires MagicArcaneFieldBehaviour.",
                fieldPrefab);
        }

        fieldObject.SetActive(false);
        Destroy(fieldObject);
        return false;
    }
}
