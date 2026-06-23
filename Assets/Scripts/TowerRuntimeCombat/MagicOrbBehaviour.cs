using System;
using System.Collections.Generic;
using UnityEngine;

public class MagicOrbBehaviour : MonoBehaviour
{
    private TowerInstance sourceTower;
    private MonsterManager monsterManager;
    private AttackConfig attackConfig;
    private Transform orbitCenter;
    private int remainingHitCount;
    private float orbitAngle;
    private bool isInitialized;
    private bool hasEnded;

    private readonly Dictionary<MonsterBehaviour, float> monsterHitCooldownEnds = new Dictionary<MonsterBehaviour, float>();

    public event Action<MagicOrbBehaviour> OnEnded;

    public TowerInstance SourceTower => sourceTower;
    public AttackConfig AttackConfig => attackConfig;
    public Transform OrbitCenter => orbitCenter;
    public int RemainingHitCount => remainingHitCount;
    public bool IsInitialized => isInitialized;

    public void Initialize(
        TowerInstance sourceTower,
        MonsterManager monsterManager,
        AttackConfig attackConfig,
        Transform orbitCenter)
    {
        this.sourceTower = sourceTower;
        this.monsterManager = monsterManager;
        this.attackConfig = attackConfig;
        this.orbitCenter = orbitCenter != null ? orbitCenter : transform.parent;

        remainingHitCount = attackConfig != null ? attackConfig.MagicOrbMaxHitCount : 0;
        orbitAngle = 0f;
        hasEnded = false;
        monsterHitCooldownEnds.Clear();

        if (!CanInitialize())
        {
            Destroy(gameObject);
            return;
        }

        isInitialized = true;
        UpdateOrbitPosition();
    }

    private bool CanInitialize()
    {
        if (monsterManager == null)
        {
            Debug.LogWarning("Magic orb cannot initialize: monster manager is null.", this);
            return false;
        }

        if (attackConfig == null)
        {
            Debug.LogWarning("Magic orb cannot initialize: attack config is null.", this);
            return false;
        }

        if (orbitCenter == null)
        {
            Debug.LogWarning("Magic orb cannot initialize: orbit center is null.", this);
            return false;
        }

        if (remainingHitCount <= 0)
        {
            Debug.LogWarning("Magic orb cannot initialize: max hit count must be greater than zero.", this);
            return false;
        }

        return true;
    }

    private void Update()
    {
        if (!isInitialized || hasEnded)
        {
            return;
        }

        UpdateOrbitPosition();
        TryHitMonsters();
    }

    private void UpdateOrbitPosition()
    {
        if (orbitCenter == null)
        {
            EndOrb();
            return;
        }

        orbitAngle += attackConfig.MagicOrbRotationSpeed * Time.deltaTime;
        float angleRadians = orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angleRadians), 0f, Mathf.Sin(angleRadians)) * attackConfig.MagicOrbOrbitRadius;
        transform.position = orbitCenter.position + offset;
    }

    private void TryHitMonsters()
    {
        IReadOnlyList<MonsterBehaviour> aliveMonsters = monsterManager.GetAliveMonsters();
        float contactDistanceSqr = attackConfig.MagicOrbContactDistance * attackConfig.MagicOrbContactDistance;

        for (int i = 0; i < aliveMonsters.Count; i++)
        {
            MonsterBehaviour monster = aliveMonsters[i];

            if (!IsValidTarget(monster))
            {
                continue;
            }

            if (IsTargetOnCooldown(monster))
            {
                continue;
            }

            if ((GetMonsterHitPosition(monster) - transform.position).sqrMagnitude > contactDistanceSqr)
            {
                continue;
            }

            HitMonster(monster);

            if (hasEnded)
            {
                return;
            }
        }
    }

    private void HitMonster(MonsterBehaviour monster)
    {
        monster.TakeDamage(attackConfig.Damage);
        monsterHitCooldownEnds[monster] = Time.time + attackConfig.MagicOrbSameTargetHitCooldown;
        remainingHitCount--;

        if (remainingHitCount <= 0)
        {
            EndOrb();
        }
    }

    private bool IsTargetOnCooldown(MonsterBehaviour monster)
    {
        return monsterHitCooldownEnds.TryGetValue(monster, out float cooldownEndTime) &&
               Time.time < cooldownEndTime;
    }

    private static Vector3 GetMonsterHitPosition(MonsterBehaviour monster)
    {
        Transform hitAnchor = monster.HitAnchor;
        return hitAnchor != null ? hitAnchor.position : monster.transform.position;
    }

    private static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }

    private void EndOrb()
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        isInitialized = false;
        OnEnded?.Invoke(this);
        Destroy(gameObject);
    }
}
