using System.Collections.Generic;
using UnityEngine;

public class WindVortexBehaviour : MonoBehaviour
{
    private const float IdleTargetSearchInterval = 0.1f;

    private readonly List<MonsterBehaviour> targetCandidates = new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> damageTargets = new List<MonsterBehaviour>();

    private WindVortexConfig config;
    private MonsterManager monsterManager;
    private TowerInstance sourceTower;
    private TowerUpgradeDefinition sourceUpgrade;
    private MonsterBehaviour currentTarget;
    private MonsterBehaviour lastReachedTarget;
    private float remainingLifetime;
    private float damageTickTimer;
    private float idleTargetSearchTimer;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public MonsterBehaviour CurrentTarget => currentTarget;
    public float RemainingLifetime => Mathf.Max(0f, remainingLifetime);

    public void Initialize(
        WindVortexConfig config,
        MonsterManager monsterManager,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade)
    {
        this.config = config;
        this.monsterManager = monsterManager;
        this.sourceTower = sourceTower;
        this.sourceUpgrade = sourceUpgrade;
        currentTarget = null;
        lastReachedTarget = null;
        remainingLifetime = config != null ? config.Lifetime : 0f;
        damageTickTimer = 0f;
        idleTargetSearchTimer = 0f;
        isInitialized = false;

        if (!CanInitialize())
        {
            Destroy(gameObject);
            return;
        }

        isInitialized = true;

        if (!TryAcquireTarget(null))
        {
            idleTargetSearchTimer = IdleTargetSearchInterval;
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        remainingLifetime -= deltaTime;

        if (remainingLifetime <= 0f)
        {
            Despawn();
            return;
        }

        UpdateTargetAndMovement(deltaTime);
        UpdateDamageTicks(deltaTime);
    }

    private bool CanInitialize()
    {
        if (config == null || !config.IsValid())
        {
            return false;
        }

        if (monsterManager == null)
        {
            Debug.LogWarning("Wind vortex cannot initialize: MonsterManager is null.", this);
            return false;
        }

        return true;
    }

    private void UpdateTargetAndMovement(float deltaTime)
    {
        if (currentTarget == null)
        {
            UpdateIdleTargetSearch(deltaTime);
            return;
        }

        if (!EffectTargetResolver.IsValidMonsterTarget(currentTarget))
        {
            currentTarget = null;
            lastReachedTarget = null;

            if (!TryAcquireTarget(null))
            {
                idleTargetSearchTimer = IdleTargetSearchInterval;
            }

            return;
        }

        Vector3 targetPosition = EffectTargetResolver.GetMonsterHitPosition(currentTarget);
        float arrivalThresholdSqr = config.ArrivalThreshold * config.ArrivalThreshold;

        if ((targetPosition - transform.position).sqrMagnitude <= arrivalThresholdSqr)
        {
            if (currentTarget == lastReachedTarget)
            {
                return;
            }

            MonsterBehaviour reachedTarget = currentTarget;
            lastReachedTarget = reachedTarget;
            currentTarget = null;

            if (!TryAcquireTarget(reachedTarget))
            {
                idleTargetSearchTimer = IdleTargetSearchInterval;
            }

            return;
        }

        lastReachedTarget = null;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            config.MovementSpeed * deltaTime);
    }

    private void UpdateIdleTargetSearch(float deltaTime)
    {
        idleTargetSearchTimer -= deltaTime;

        if (idleTargetSearchTimer > 0f)
        {
            return;
        }

        if (!TryAcquireTarget(null))
        {
            idleTargetSearchTimer = IdleTargetSearchInterval;
        }
    }

    private bool TryAcquireTarget(MonsterBehaviour excludedTarget)
    {
        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        int candidateCount = EffectTargetResolver.CollectValidTargetsInRadius(
            aliveMonsters,
            transform.position,
            config.TargetSearchRadius,
            excludedTarget,
            targetCandidates);

        if (candidateCount == 0 && excludedTarget != null)
        {
            candidateCount = EffectTargetResolver.CollectValidTargetsInRadius(
                aliveMonsters,
                transform.position,
                config.TargetSearchRadius,
                null,
                targetCandidates);
        }

        if (candidateCount == 0)
        {
            return false;
        }

        currentTarget = targetCandidates[Random.Range(0, candidateCount)];
        idleTargetSearchTimer = 0f;
        return true;
    }

    private void UpdateDamageTicks(float deltaTime)
    {
        damageTickTimer += deltaTime;

        while (damageTickTimer >= config.TickInterval)
        {
            damageTickTimer -= config.TickInterval;
            ExecuteDamageTick();
        }
    }

    private void ExecuteDamageTick()
    {
        EffectTargetResolver.CollectValidTargetsInRadius(
            monsterManager.GetAliveMonsters(),
            transform.position,
            config.DamageRadius,
            null,
            damageTargets);

        for (int i = 0; i < damageTargets.Count; i++)
        {
            MonsterBehaviour target = damageTargets[i];

            if (!EffectTargetResolver.IsValidMonsterTarget(target))
            {
                continue;
            }

            EffectExecutor.Execute(
                config.OnTickEffectDefinition,
                new EffectTriggerContext(
                    EffectTriggerType.OnZoneTick,
                    sourceTower,
                    sourceUpgrade,
                    target,
                    true,
                    transform.position,
                    0,
                    false));
        }
    }

    private void Despawn()
    {
        isInitialized = false;
        Destroy(gameObject);
    }
}
