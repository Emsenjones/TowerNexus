using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public class MonsterBuffInstance
{
    private readonly BuffDefinition definition;
    private readonly MonsterBehaviour owner;
    private TowerInstance sourceTower;
    private TowerUpgradeDefinition sourceUpgrade;
    private readonly Dictionary<TowerInstance, float> nextAllowedApplyTimesBySourceTower =
        new Dictionary<TowerInstance, float>();
    private float remainingPhaseDuration;
    private float periodicTickTimer;
    private int stackCount;
    private float nextAllowedUnattributedApplyTime;
    private float nextAllowedElementalHitReactionTime;
    private BuffRuntimePhase phase;
    private readonly int stackingCycleIdentity;

    public MonsterBuffInstance(
        BuffApplyRequest request,
        MonsterBehaviour owner,
        int stackingCycleIdentity)
    {
        definition = request.BuffDefinition;
        this.owner = owner;
        sourceTower = request.SourceTower;
        sourceUpgrade = request.SourceUpgrade;
        remainingPhaseDuration = definition.ActiveDuration;
        periodicTickTimer = 0f;
        stackCount = definition.UsesStacks
            ? Mathf.Min(request.RequestedStackUnits, definition.MaximumStacks)
            : 1;
        phase = BuffRuntimePhase.Stacking;
        this.stackingCycleIdentity = stackingCycleIdentity;
        RecordSourceApplyCooldown(request.SourceTower);
    }

    public BuffDefinition Definition => definition;
    public MonsterBehaviour Owner => owner;
    public TowerInstance SourceTower => sourceTower;
    public TowerUpgradeDefinition SourceUpgrade => sourceUpgrade;
    public float RemainingPhaseDuration => remainingPhaseDuration;
    public float PeriodicTickTimer => periodicTickTimer;
    public int StackCount => stackCount;
    public BuffRuntimePhase Phase => phase;
    public bool IsInProtectionPhase => phase == BuffRuntimePhase.Protection;
    public int StackingCycleIdentity => stackingCycleIdentity;
    public bool IsElementalHitReactionCooldownActive =>
        definition != null &&
        definition.TowerHitReactionCooldown > 0f &&
               Time.time < nextAllowedElementalHitReactionTime;

    internal string CapturePlacementFingerprint()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(definition != null ? definition.GetInstanceID() : 0)
            .Append('|')
            .Append(sourceTower != null ? sourceTower.GetInstanceID() : 0)
            .Append('|')
            .Append(sourceUpgrade != null ? sourceUpgrade.GetInstanceID() : 0)
            .Append('|')
            .Append(stackCount)
            .Append('|')
            .Append((int)phase)
            .Append('|')
            .Append(remainingPhaseDuration.ToString("R", CultureInfo.InvariantCulture))
            .Append('|')
            .Append(periodicTickTimer.ToString("R", CultureInfo.InvariantCulture))
            .Append('|')
            .Append(stackingCycleIdentity)
            .Append('|')
            .Append(Mathf.Max(0f, nextAllowedUnattributedApplyTime - Time.time)
                .ToString("R", CultureInfo.InvariantCulture))
            .Append('|')
            .Append(Mathf.Max(0f, nextAllowedElementalHitReactionTime - Time.time)
                .ToString("R", CultureInfo.InvariantCulture));

        List<KeyValuePair<TowerInstance, float>> sourceCooldowns =
            new List<KeyValuePair<TowerInstance, float>>(
                nextAllowedApplyTimesBySourceTower);
        sourceCooldowns.Sort((left, right) =>
            (left.Key != null ? left.Key.GetInstanceID() : 0).CompareTo(
                right.Key != null ? right.Key.GetInstanceID() : 0));

        for (int i = 0; i < sourceCooldowns.Count; i++)
        {
            KeyValuePair<TowerInstance, float> cooldown = sourceCooldowns[i];
            builder.Append(';')
                .Append(cooldown.Key != null
                    ? cooldown.Key.GetInstanceID()
                    : 0)
                .Append(':')
                .Append(Mathf.Max(0f, cooldown.Value - Time.time)
                    .ToString("R", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    public void RecordElementalHitReactionCooldown()
    {
        if (definition == null || definition.TowerHitReactionCooldown <= 0f)
        {
            return;
        }

        nextAllowedElementalHitReactionTime =
            Time.time + definition.TowerHitReactionCooldown;
    }

    public BuffApplyResult TryReapply(
        BuffApplyRequest request,
        out int eligibleRequestedStackUnits,
        out int appliedStackUnits,
        out int discardedStackUnits)
    {
        eligibleRequestedStackUnits = 0;
        appliedStackUnits = 0;
        discardedStackUnits = 0;

        if (definition == null || !definition.IsValid())
        {
            return BuffApplyResult.Invalid;
        }

        if (definition.UsesStacks && request.RequestedStackUnits <= 0)
        {
            return BuffApplyResult.Invalid;
        }

        if (definition.UsesStacks && phase == BuffRuntimePhase.Protection)
        {
            return BuffApplyResult.BlockedByProtectionPhase;
        }

        if (definition.UsesStacks && IsSourceApplyCooldownActive(request.SourceTower))
        {
            return BuffApplyResult.BlockedBySourceApplyCooldown;
        }

        sourceTower = request.SourceTower;
        sourceUpgrade = request.SourceUpgrade;
        remainingPhaseDuration = definition.ActiveDuration;
        RecordSourceApplyCooldown(request.SourceTower);

        if (!definition.UsesStacks)
        {
            return BuffApplyResult.Refreshed;
        }

        eligibleRequestedStackUnits = request.RequestedStackUnits;
        int remainingCapacity = Mathf.Max(
            0,
            definition.MaximumStacks - stackCount);
        appliedStackUnits = Mathf.Min(
            eligibleRequestedStackUnits,
            remainingCapacity);
        discardedStackUnits = eligibleRequestedStackUnits - appliedStackUnits;

        if (appliedStackUnits > 0)
        {
            stackCount += appliedStackUnits;
            return BuffApplyResult.Stacked;
        }

        return BuffApplyResult.Refreshed;
    }

    public bool TryEnterProtectionPhase()
    {
        if (definition == null || !definition.UsesStacks || definition.OverloadProtectionDuration <= 0f)
        {
            return false;
        }

        phase = BuffRuntimePhase.Protection;
        remainingPhaseDuration = definition.OverloadProtectionDuration;
        periodicTickTimer = 0f;
        stackCount = 0;
        nextAllowedApplyTimesBySourceTower.Clear();
        nextAllowedUnattributedApplyTime = 0f;
        nextAllowedElementalHitReactionTime = 0f;
        return true;
    }

    public bool Tick(float deltaTime, out int periodicTickCount)
    {
        periodicTickCount = 0;

        if (definition == null || owner == null)
        {
            return false;
        }

        remainingPhaseDuration -= deltaTime;

        if (phase == BuffRuntimePhase.Protection)
        {
            return remainingPhaseDuration > 0f;
        }

        if (definition.PeriodicTickInterval > 0f)
        {
            periodicTickTimer += deltaTime;

            while (periodicTickTimer >= definition.PeriodicTickInterval)
            {
                periodicTickTimer -= definition.PeriodicTickInterval;
                periodicTickCount++;
            }
        }

        return remainingPhaseDuration > 0f;
    }

    private bool IsSourceApplyCooldownActive(TowerInstance applyingSourceTower)
    {
        if (!definition.UsesStacks)
        {
            return false;
        }

        if (applyingSourceTower == null)
        {
            return Time.time < nextAllowedUnattributedApplyTime;
        }

        return nextAllowedApplyTimesBySourceTower.TryGetValue(
                   applyingSourceTower,
                   out float nextAllowedApplyTime) &&
               Time.time < nextAllowedApplyTime;
    }

    private void RecordSourceApplyCooldown(TowerInstance applyingSourceTower)
    {
        if (!definition.UsesStacks)
        {
            return;
        }

        float nextAllowedApplyTime = Time.time + definition.SourceApplyCooldown;

        if (applyingSourceTower == null)
        {
            nextAllowedUnattributedApplyTime = nextAllowedApplyTime;
            return;
        }

        nextAllowedApplyTimesBySourceTower[applyingSourceTower] = nextAllowedApplyTime;
    }
}
