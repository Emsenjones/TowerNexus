using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class WindVortexBehaviour : MonoBehaviour
{
    private const float IdleTargetSearchInterval = 0.1f;

    private readonly List<MonsterBehaviour> targetCandidates = new List<MonsterBehaviour>();
    private readonly List<MonsterBehaviour> damageTargets = new List<MonsterBehaviour>();

    [TitleGroup("Lifetime")]
    [MinValue(0.01f)]
    [SerializeField] private float lifetime = 5f;

    [TitleGroup("Movement")]
    [MinValue(0.01f)]
    [SerializeField] private float movementSpeed = 1f;
    [TitleGroup("Movement")]
    [MinValue(0f)]
    [SerializeField] private float targetSearchRadius = 3f;
    [TitleGroup("Movement")]
    [MinValue(0.01f)]
    [SerializeField] private float arrivalThreshold = 0.1f;

    [TitleGroup("Damage")]
    [MinValue(0f)]
    [SerializeField] private float damageRadius = 1f;
    [TitleGroup("Damage")]
    [MinValue(0.01f)]
    [SerializeField] private float tickInterval = 1f;
    [TitleGroup("Damage")]
    [Required]
    [SerializeField] private EffectDefinition onTickEffectDefinition;

    private BattleCombatBinding battleBinding;
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
        BattleCombatBinding battleBinding,
        TowerInstance sourceTower,
        TowerUpgradeDefinition sourceUpgrade)
    {
        this.battleBinding = battleBinding;
        this.sourceTower = sourceTower;
        this.sourceUpgrade = sourceUpgrade;
        currentTarget = null;
        lastReachedTarget = null;
        remainingLifetime = lifetime;
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
        if (isInitialized && (battleBinding == null || !battleBinding.IsUsable))
        {
            Despawn();
            return;
        }
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
        if (!IsValid())
        {
            return false;
        }

        if ((battleBinding == null || !battleBinding.IsUsable))
        {
            Debug.LogWarning("Wind vortex cannot initialize: BattleCombatBinding is null.", this);
            return false;
        }

        return true;
    }

    public bool IsValid()
    {
        bool isValid = true;

        if (lifetime <= 0f || movementSpeed <= 0f || arrivalThreshold <= 0f || tickInterval <= 0f ||
            targetSearchRadius < 0f || damageRadius < 0f)
        {
            Debug.LogWarning($"Wind vortex prefab '{name}' is invalid: lifetime, movement speed, arrival threshold, and tick interval must be positive; radii cannot be negative.", this);
            isValid = false;
        }

        if (onTickEffectDefinition == null)
        {
            Debug.LogWarning($"Wind vortex prefab '{name}' is invalid: on-tick EffectDefinition is required.", this);
            return false;
        }

        if (onTickEffectDefinition.Radius > 0f)
        {
            Debug.LogWarning($"Wind vortex prefab '{name}' is invalid: on-tick EffectDefinition must be single-target.", this);
            isValid = false;
        }

        if (!onTickEffectDefinition.IsValidForDamageMode(
                EffectDamageMode.FixedBuff))
        {
            Debug.LogWarning(
                $"Wind vortex prefab '{name}' is invalid: on-tick damage must use FixedBuff mode.",
                this);
            isValid = false;
        }

        if (!onTickEffectDefinition.ContainsDealDamageAction())
        {
            Debug.LogWarning(
                $"Wind vortex prefab '{name}' is invalid: on-tick EffectDefinition requires DealDamage.",
                this);
            isValid = false;
        }

        return isValid;
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

        Vector3 targetPosition = currentTarget.transform.position;
        float arrivalThresholdSqr = arrivalThreshold * arrivalThreshold;

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
            movementSpeed * deltaTime);
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
        IReadOnlyList<MonsterBehaviour> aliveMonsters = battleBinding.GetAliveMonsters();
        int candidateCount = EffectTargetResolver.CollectValidTargetsInRadius(
            aliveMonsters,
            transform.position,
            targetSearchRadius,
            excludedTarget,
            targetCandidates);

        if (candidateCount == 0 && excludedTarget != null)
        {
            candidateCount = EffectTargetResolver.CollectValidTargetsInRadius(
                aliveMonsters,
                transform.position,
                targetSearchRadius,
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

        while (damageTickTimer >= tickInterval)
        {
            damageTickTimer -= tickInterval;
            ExecuteDamageTick();
        }
    }

    private void ExecuteDamageTick()
    {
        EffectTargetResolver.CollectValidTargetsInRadius(
            battleBinding.GetAliveMonsters(),
            transform.position,
            damageRadius,
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
                onTickEffectDefinition,
                new EffectTriggerContext(
                    battleBinding: battleBinding,
                    sourceTower: sourceTower,
                    sourceUpgrade: sourceUpgrade,
                    targetMonster: target,
                    hasTriggerPosition: true,
                    triggerPosition: transform.position,
                    allowsElementalApplication: false));
        }
    }

    private void Despawn()
    {
        isInitialized = false;
        Destroy(gameObject);
    }
}
