using UnityEngine;

public class MonsterBuffInstance
{
    private readonly BuffDefinition definition;
    private readonly MonsterBehaviour owner;
    private TowerInstance sourceTower;
    private TowerUpgradeDefinition sourceUpgrade;
    private float remainingDuration;
    private float tickTimer;
    private int stackCount;
    private float nextAllowedApplyTime;
    private BuffRuntimePhase phase;

    public MonsterBuffInstance(BuffApplyRequest request, MonsterBehaviour owner)
    {
        definition = request.BuffDefinition;
        this.owner = owner;
        sourceTower = request.SourceTower;
        sourceUpgrade = request.SourceUpgrade;
        remainingDuration = definition.Duration;
        tickTimer = 0f;
        stackCount = 1;
        nextAllowedApplyTime = Time.time + definition.BuffApplyCooldown;
        phase = BuffRuntimePhase.Stacking;
    }

    public BuffDefinition Definition => definition;
    public MonsterBehaviour Owner => owner;
    public TowerInstance SourceTower => sourceTower;
    public TowerUpgradeDefinition SourceUpgrade => sourceUpgrade;
    public float RemainingDuration => remainingDuration;
    public float TickTimer => tickTimer;
    public int StackCount => stackCount;
    public float NextAllowedApplyTime => nextAllowedApplyTime;
    public BuffRuntimePhase Phase => phase;
    public bool IsInProtectionPhase => phase == BuffRuntimePhase.Protection;

    public BuffApplyResult TryReapply(BuffApplyRequest request)
    {
        if (definition == null || !definition.IsValid())
        {
            return BuffApplyResult.Invalid;
        }

        if (phase == BuffRuntimePhase.Protection)
        {
            return BuffApplyResult.BlockedByProtectionPhase;
        }

        if (Time.time < nextAllowedApplyTime)
        {
            return BuffApplyResult.BlockedByBuffApplyCooldown;
        }

        sourceTower = request.SourceTower;
        sourceUpgrade = request.SourceUpgrade;
        remainingDuration = definition.Duration;
        nextAllowedApplyTime = Time.time + definition.BuffApplyCooldown;

        if (stackCount < definition.MaxStacks)
        {
            stackCount++;
            return BuffApplyResult.Stacked;
        }

        return BuffApplyResult.Refreshed;
    }

    public bool TryEnterProtectionPhase()
    {
        if (definition == null || definition.ProtectionDuration <= 0f)
        {
            return false;
        }

        phase = BuffRuntimePhase.Protection;
        remainingDuration = definition.ProtectionDuration;
        tickTimer = 0f;
        stackCount = 0;
        nextAllowedApplyTime = 0f;
        return true;
    }

    public bool Tick(float deltaTime)
    {
        if (definition == null || owner == null)
        {
            return false;
        }

        remainingDuration -= deltaTime;

        if (phase == BuffRuntimePhase.Protection)
        {
            return remainingDuration > 0f;
        }

        if (definition.TickInterval > 0f)
        {
            tickTimer += deltaTime;

            while (tickTimer >= definition.TickInterval)
            {
                tickTimer -= definition.TickInterval;
                ExecuteTickEffect();
            }
        }

        return remainingDuration > 0f;
    }

    private void ExecuteTickEffect()
    {
        EffectDefinition periodicEffect = definition.GetEffectDefinition(BuffEventType.PeriodicTick);

        if (periodicEffect == null)
        {
            return;
        }

        Transform hitAnchor = owner.HitAnchor;
        Vector3 triggerPosition = hitAnchor != null ? hitAnchor.position : owner.transform.position;

        EffectExecutor.Execute(
            periodicEffect,
            new EffectTriggerContext(
                EffectTriggerType.OnBuffTick,
                sourceTower,
                sourceUpgrade,
                owner,
                true,
                triggerPosition,
                0,
                false)
        );
    }
}
