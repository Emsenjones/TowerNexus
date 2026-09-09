using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public readonly struct MagicOrbStatRefresh
{
    public MagicOrbStatRefresh(
        bool refreshRotationSpeed,
        float newRotationSpeed)
    {
        RefreshRotationSpeed = refreshRotationSpeed;
        NewRotationSpeed = newRotationSpeed;
    }

    public bool RefreshRotationSpeed { get; }
    public float NewRotationSpeed { get; }
    public bool HasAnyChange => RefreshRotationSpeed;
}

public class MagicOrbBehaviour : MonoBehaviour
{
    [TitleGroup("Orbit")]
    [MinValue(0f)]
    [SerializeField] private float rotationSpeed = 180f;
    [TitleGroup("Orbit")]
    [MinValue(0f)]
    [SerializeField] private float orbitRadius = 0.75f;

    [TitleGroup("Hits")]
    [MinValue(0f)]
    [SerializeField] private float contactDistance = 0.25f;
    [TitleGroup("Hits")]
    [MinValue(0f)]
    [SerializeField] private float sameTargetHitCooldown = 0.5f;

    [TitleGroup("Lifetime")]
    [MinValue(0.01f)]
    [SerializeField] private float maxLifetime = 5f;

    private readonly Dictionary<MonsterBehaviour, float> monsterHitCooldownEnds =
        new Dictionary<MonsterBehaviour, float>();

    private MagicOrbGroupRuntime ownerGroup;
    private int memberSlot;
    private float angleOffset;
    private float damageScale;
    private TowerDamageSourceIdentity damageSourceIdentity;
    private int contactResultOrdinal;
    private bool isInitialized;
    private bool hasEnded;

    public TowerInstance SourceTower => ownerGroup != null ? ownerGroup.SourceTower : null;
    public Vector3 OrbitCenterPosition => ownerGroup != null
        ? ownerGroup.OrbitCenterPosition
        : transform.position;
    public bool IsInitialized => isInitialized;
    public int MemberSlot => memberSlot;
    public float AngleOffset => angleOffset;
    public float BaseRotationSpeed => rotationSpeed;
    public float BaseOrbitRadius => orbitRadius;
    public float BaseContactDistance => contactDistance;
    public float BaseMaxLifetime => maxLifetime;
    public float BaseSameTargetHitCooldown => sameTargetHitCooldown;

    public bool IsAuthoredConfigurationValid()
    {
        return rotationSpeed >= 0f &&
               orbitRadius >= 0f &&
               contactDistance >= 0f &&
               maxLifetime > 0f &&
               sameTargetHitCooldown >= 0f;
    }

    internal bool Initialize(
        MagicOrbGroupRuntime initializedOwnerGroup,
        int initializedMemberSlot,
        float initializedAngleOffset,
        float initializedDamageScale,
        TowerDamageSourceIdentity initializedDamageSourceIdentity)
    {
        ownerGroup = initializedOwnerGroup;
        memberSlot = initializedMemberSlot;
        angleOffset = initializedAngleOffset;
        damageScale = initializedDamageScale;
        damageSourceIdentity = initializedDamageSourceIdentity;
        monsterHitCooldownEnds.Clear();
        contactResultOrdinal = 0;
        hasEnded = false;
        isInitialized = ownerGroup != null && memberSlot >= 0;

        if (isInitialized)
        {
            return true;
        }

        ForceCleanupFromGroup();
        return false;
    }

    internal bool CanAttachAsStagedMember()
    {
        return ownerGroup == null &&
               !isInitialized &&
               !hasEnded &&
               !gameObject.activeSelf;
    }

    internal void AttachCommittedMember(
        MagicOrbGroupRuntime initializedOwnerGroup,
        int initializedMemberSlot,
        float initializedAngleOffset,
        float initializedDamageScale,
        TowerDamageSourceIdentity initializedDamageSourceIdentity)
    {
        ownerGroup = initializedOwnerGroup;
        memberSlot = initializedMemberSlot;
        angleOffset = initializedAngleOffset;
        damageScale = initializedDamageScale;
        damageSourceIdentity = initializedDamageSourceIdentity;
        monsterHitCooldownEnds.Clear();
        contactResultOrdinal = 0;
        hasEnded = false;
        isInitialized = ownerGroup != null && memberSlot >= 0;
    }

    internal void ActivateCommittedMember()
    {
        if (isInitialized && !hasEnded)
        {
            gameObject.SetActive(true);
        }
    }

    internal bool TryResolveDamage(
        out TowerOwnedDamageResolution damageResolution)
    {
        damageResolution = default;
        return isInitialized &&
               !hasEnded &&
               TowerRuntimeStatResolver.TryResolveTowerOwnedDamage(
                   SourceTower,
                   damageSourceIdentity,
                   damageScale,
                   out damageResolution);
    }

    internal bool IsTargetOnCooldown(MonsterBehaviour monster, float currentTime)
    {
        return monsterHitCooldownEnds.TryGetValue(monster, out float cooldownEndTime) &&
               currentTime < cooldownEndTime;
    }

    internal void RecordContact(MonsterBehaviour monster, float cooldownEndTime)
    {
        monsterHitCooldownEnds[monster] = cooldownEndTime;
    }

    internal int ConsumeContactResultOrdinal()
    {
        return contactResultOrdinal++;
    }

    internal void SetGroupPosition(Vector3 worldPosition)
    {
        if (isInitialized && !hasEnded)
        {
            transform.position = worldPosition;
        }
    }

    internal void CompleteFromGroup()
    {
        EndFromGroup();
    }

    internal void ForceCleanupFromGroup()
    {
        EndFromGroup();
    }

    private void OnDisable()
    {
        NotifyUnexpectedInvalidation();
    }

    private void OnDestroy()
    {
        NotifyUnexpectedInvalidation();
    }

    private void NotifyUnexpectedInvalidation()
    {
        if (!isInitialized || hasEnded || ownerGroup == null)
        {
            return;
        }

        MagicOrbGroupRuntime group = ownerGroup;
        hasEnded = true;
        isInitialized = false;
        ownerGroup = null;
        monsterHitCooldownEnds.Clear();
        group.HandleUnexpectedMemberInvalidation(this);
    }

    private void EndFromGroup()
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        isInitialized = false;
        ownerGroup = null;
        monsterHitCooldownEnds.Clear();
        Destroy(gameObject);
    }
}

internal sealed class MagicOrbGroupRuntime
{
    private readonly List<MagicOrbBehaviour> members = new List<MagicOrbBehaviour>();
    private readonly List<Vector3> completionPositions = new List<Vector3>();
    private readonly List<MonsterBehaviour> resolvedArcaneDetonationTargets =
        new List<MonsterBehaviour>();

    private readonly TowerInstance sourceTower;
    private readonly BattleCombatBinding battleBinding;
    private readonly Vector3 orbitCenterPosition;
    private readonly float orbitRadius;
    private readonly float contactDistance;
    private readonly float maxLifetime;
    private readonly float sameTargetHitCooldown;

    private TowerUpgradeDefinition arcaneDetonationSourceUpgrade;
    private EffectDefinition arcaneDetonationEffect;
    private float rotationSpeed;
    private float orbitPhase;
    private float elapsedLifetime;
    private bool isActive;
    private bool hasEnded;

    public event Action<MagicOrbGroupRuntime, bool> OnEnded;

    public MagicOrbGroupRuntime(
        long releaseGroupId,
        TowerInstance sourceTower,
        BattleCombatBinding battleBinding,
        MagicOrbRuntimeOptions runtimeOptions,
        Vector3 orbitCenterPosition,
        float initialOrbitPhase,
        ResolvedTowerCombatStats resolvedStats,
        MagicOrbBehaviour authoredOrb)
    {
        ReleaseGroupId = releaseGroupId;
        this.sourceTower = sourceTower;
        this.battleBinding = battleBinding;
        this.orbitCenterPosition = orbitCenterPosition;
        rotationSpeed = resolvedStats.MagicOrbRotationSpeed;
        arcaneDetonationSourceUpgrade = runtimeOptions.ArcaneDetonationSourceUpgrade;
        arcaneDetonationEffect = runtimeOptions.ArcaneDetonationEffect;
        orbitRadius = authoredOrb != null ? authoredOrb.BaseOrbitRadius : -1f;
        contactDistance = authoredOrb != null ? authoredOrb.BaseContactDistance : -1f;
        maxLifetime = authoredOrb != null ? authoredOrb.BaseMaxLifetime : 0f;
        sameTargetHitCooldown = authoredOrb != null
            ? authoredOrb.BaseSameTargetHitCooldown
            : -1f;
        orbitPhase = initialOrbitPhase;
    }

    public long ReleaseGroupId { get; }
    public TowerInstance SourceTower => sourceTower;
    public Vector3 OrbitCenterPosition => orbitCenterPosition;
    public int MemberCount => members.Count;
    public bool IsActive => isActive && !hasEnded;

    public bool CanInitialize()
    {
        return sourceTower != null &&
               battleBinding != null && battleBinding.IsUsable &&
               rotationSpeed >= 0f &&
               orbitRadius >= 0f &&
               contactDistance >= 0f &&
               maxLifetime > 0f &&
               sameTargetHitCooldown >= 0f;
    }

    public bool TryAddMember(
        MagicOrbBehaviour member,
        int memberSlot,
        int desiredMemberCount,
        float damageScale)
    {
        if (hasEnded ||
            isActive ||
            member == null ||
            memberSlot != members.Count ||
            desiredMemberCount <= 0)
        {
            return false;
        }

        float angleOffset = 360f / desiredMemberCount * memberSlot;

        if (!member.Initialize(
                this,
                memberSlot,
                angleOffset,
                damageScale,
                memberSlot == 0
                    ? TowerDamageSourceIdentity.PrimaryDirect
                    : TowerDamageSourceIdentity.AdditionalDirect))
        {
            return false;
        }

        members.Add(member);
        SetMemberPosition(member);
        return true;
    }

    public bool Activate(int expectedMemberCount)
    {
        if (hasEnded ||
            !CanInitialize() ||
            expectedMemberCount <= 0 ||
            members.Count != expectedMemberCount)
        {
            ForceCleanup();
            return false;
        }

        isActive = true;
        UpdateMemberPositions();
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive)
        {
            return;
        }

        if (sourceTower == null || (battleBinding == null || !battleBinding.IsUsable))
        {
            ForceCleanup();
            return;
        }

        elapsedLifetime += Mathf.Max(0f, deltaTime);

        if (elapsedLifetime >= maxLifetime)
        {
            CompleteNormally();
            return;
        }

        orbitPhase += rotationSpeed * Mathf.Max(0f, deltaTime);
        UpdateMemberPositions();
        ResolveContactsInStableOrder();
    }

    public void ApplyStatRefresh(MagicOrbStatRefresh refresh)
    {
        if (!IsActive || !refresh.HasAnyChange)
        {
            return;
        }

        if (refresh.RefreshRotationSpeed)
        {
            rotationSpeed = Mathf.Max(0f, refresh.NewRotationSpeed);
        }

    }

    public void EnableArcaneDetonation(
        TowerUpgradeDefinition sourceUpgrade,
        EffectDefinition effectDefinition)
    {
        if (!IsActive)
        {
            return;
        }

        arcaneDetonationSourceUpgrade = sourceUpgrade;
        arcaneDetonationEffect = effectDefinition;
    }

    public bool TryCommitStagedMembers(
        IReadOnlyList<MagicOrbBehaviour> stagedMembers,
        int desiredMemberCount,
        float damageScale)
    {
        if (!IsActive ||
            stagedMembers == null ||
            desiredMemberCount <= members.Count ||
            stagedMembers.Count != desiredMemberCount - members.Count)
        {
            return false;
        }

        for (int i = 0; i < stagedMembers.Count; i++)
        {
            MagicOrbBehaviour candidate = stagedMembers[i];

            if (candidate == null || !candidate.CanAttachAsStagedMember())
            {
                return false;
            }
        }

        int firstNewSlot = members.Count;

        for (int i = 0; i < stagedMembers.Count; i++)
        {
            MagicOrbBehaviour candidate = stagedMembers[i];
            int slot = firstNewSlot + i;
            float angleOffset = 360f / desiredMemberCount * slot;
            candidate.AttachCommittedMember(
                this,
                slot,
                angleOffset,
                damageScale,
                TowerDamageSourceIdentity.AdditionalDirect);
            members.Add(candidate);
            SetMemberPosition(candidate);
        }

        for (int i = 0; i < stagedMembers.Count; i++)
        {
            stagedMembers[i].ActivateCommittedMember();
        }

        return true;
    }

    public void ForceCleanup()
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        isActive = false;
        TeardownMembers(technicalCleanup: true);
        OnEnded?.Invoke(this, false);
    }

    public void HandleUnexpectedMemberInvalidation(MagicOrbBehaviour member)
    {
        if (hasEnded || member == null || !members.Contains(member))
        {
            return;
        }

        ForceCleanup();
    }

    private void ResolveContactsInStableOrder()
    {
        IReadOnlyList<MonsterBehaviour> aliveMonsters = battleBinding.GetAliveMonsters();

        for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
        {
            MagicOrbBehaviour member = members[memberIndex];

            if (member == null || !member.IsInitialized)
            {
                ForceCleanup();
                return;
            }

            for (int monsterIndex = 0; monsterIndex < aliveMonsters.Count; monsterIndex++)
            {
                MonsterBehaviour monster = aliveMonsters[monsterIndex];

                if (!IsCandidateContact(member, monster))
                {
                    continue;
                }

                if (TryResolveMemberContact(member, monster) && !IsActive)
                {
                    return;
                }
            }
        }
    }

    private bool IsCandidateContact(MagicOrbBehaviour member, MonsterBehaviour monster)
    {
        if (!EffectTargetResolver.IsValidMonsterTarget(monster) ||
            member.IsTargetOnCooldown(monster, Time.time))
        {
            return false;
        }

        Vector3 monsterPosition = EffectTargetResolver.GetMonsterHitPosition(monster);
        return IsWithinPlanarContactDistance(
            member.transform.position,
            monsterPosition,
            contactDistance);
    }

    private bool TryResolveMemberContact(MagicOrbBehaviour member, MonsterBehaviour monster)
    {
        if (!IsActive ||
            member == null ||
            !member.IsInitialized ||
            !EffectTargetResolver.IsValidMonsterTarget(monster) ||
            member.IsTargetOnCooldown(monster, Time.time))
        {
            return false;
        }

        Vector3 hitPosition = EffectTargetResolver.GetMonsterHitPosition(monster);

        if (!IsWithinPlanarContactDistance(
                member.transform.position,
                hitPosition,
                contactDistance))
        {
            return false;
        }

        bool isPrimaryMember = member.MemberSlot == 0;
        ElementalOpportunityDiagnosticContext contactDiagnostics =
            new ElementalOpportunityDiagnosticContext(
                ElementalOpportunityProvenance.MagicOrb,
                isPrimaryMember
                    ? ElementalOpportunityMemberIdentity.Primary
                    : ElementalOpportunityMemberIdentity.Additional,
                ElementalOpportunityResultRole.InitialDirect,
                member.ConsumeContactResultOrdinal(),
                topologyAuthorized: isPrimaryMember);
        ElementalApplication.ObserveCandidate(
            sourceTower,
            monster,
            contactDiagnostics);

        if (!member.TryResolveDamage(
                out TowerOwnedDamageResolution damageResolution))
        {
            return false;
        }

        TowerOwnedHitTransaction.ApplyDamage(
                battleBinding: battleBinding,
            monster,
            damageResolution,
            hitPosition,
            contactDiagnostics,
            allowsElementalApplication: isPrimaryMember);
        member.RecordContact(monster, Time.time + sameTargetHitCooldown);

        return true;
    }

    private static bool IsWithinPlanarContactDistance(
        Vector3 orbPosition,
        Vector3 monsterHitPosition,
        float maximumDistance)
    {
        float deltaX = monsterHitPosition.x - orbPosition.x;
        float deltaZ = monsterHitPosition.z - orbPosition.z;
        return deltaX * deltaX + deltaZ * deltaZ <=
               maximumDistance * maximumDistance;
    }

    private void CompleteNormally()
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        isActive = false;
        CaptureCompletionPositions();
        ExecuteArcaneDetonations();
        TeardownMembers(technicalCleanup: false);
        OnEnded?.Invoke(this, true);
    }

    private void CaptureCompletionPositions()
    {
        completionPositions.Clear();

        for (int i = 0; i < members.Count; i++)
        {
            MagicOrbBehaviour member = members[i];
            completionPositions.Add(member != null ? member.transform.position : orbitCenterPosition);
        }
    }

    private void ExecuteArcaneDetonations()
    {
        if (arcaneDetonationEffect == null || sourceTower == null)
        {
            return;
        }

        for (int i = 0; i < completionPositions.Count; i++)
        {
            Vector3 detonationPosition = completionPositions[i];
            resolvedArcaneDetonationTargets.Clear();

            if (members.Count <= i || members[i] == null)
            {
                continue;
            }

            EffectExecutor.ExecuteWithResolvedTargets(
                arcaneDetonationEffect,
                new EffectTriggerContext(
                    battleBinding: battleBinding,
                    sourceTower: sourceTower,
                    sourceUpgrade: arcaneDetonationSourceUpgrade,
                    targetMonster: null,
                    hasTriggerPosition: true,
                    triggerPosition: detonationPosition,
                    allowsElementalApplication: false,
                    elementalOpportunityDiagnostics:
                        new ElementalOpportunityDiagnosticContext(
                            ElementalOpportunityProvenance.MagicArcaneDetonation,
                            i == 0
                                ? ElementalOpportunityMemberIdentity.Primary
                                : ElementalOpportunityMemberIdentity.Additional,
                            ElementalOpportunityResultRole.CompletionResult,
                            i,
                            topologyAuthorized: false,
                            observeResolvedTargetsAsCandidates: true)),
                resolvedArcaneDetonationTargets);
        }
    }

    private void UpdateMemberPositions()
    {
        for (int i = 0; i < members.Count; i++)
        {
            SetMemberPosition(members[i]);
        }
    }

    private void SetMemberPosition(MagicOrbBehaviour member)
    {
        float angleRadians = (orbitPhase + member.AngleOffset) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angleRadians),
            0f,
            Mathf.Sin(angleRadians)) * orbitRadius;
        member.SetGroupPosition(orbitCenterPosition + offset);
    }

    private void TeardownMembers(bool technicalCleanup)
    {
        List<MagicOrbBehaviour> snapshot = new List<MagicOrbBehaviour>(members);
        members.Clear();

        for (int i = 0; i < snapshot.Count; i++)
        {
            MagicOrbBehaviour member = snapshot[i];

            if (member != null)
            {
                if (technicalCleanup)
                {
                    member.ForceCleanupFromGroup();
                }
                else
                {
                    member.CompleteFromGroup();
                }
            }
        }
    }
}
