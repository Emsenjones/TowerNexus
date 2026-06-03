using System;
using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    private TowerInstance sourceTower;
    private ProjectileConfig projectileConfig;
    private AttackConfig attackConfig;
    private MonsterBehaviour targetMonster;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float elapsedLifetime;
    private float arcTravelTime;
    private bool isInitialized;
    private bool hasImpacted;

    public event Action<ProjectileImpactContext> OnImpact;

    public TowerInstance SourceTower => sourceTower;
    public ProjectileConfig ProjectileConfig => projectileConfig;
    public AttackConfig AttackConfig => attackConfig;
    public MonsterBehaviour TargetMonster => targetMonster;
    public Vector3 TargetPosition => targetPosition;
    public bool IsInitialized => isInitialized;

    public void Initialize(
        TowerInstance sourceTower,
        ProjectileConfig projectileConfig,
        AttackConfig attackConfig,
        MonsterBehaviour targetMonster,
        Vector3 targetPosition)
    {
        this.sourceTower = sourceTower;
        this.projectileConfig = projectileConfig;
        this.attackConfig = attackConfig;
        this.targetMonster = targetMonster;
        this.targetPosition = targetPosition;

        startPosition = transform.position;
        elapsedLifetime = 0f;
        hasImpacted = false;

        if (!CanInitialize())
        {
            DestroyProjectile();
            return;
        }

        if (attackConfig.AttackArchetype == AttackArchetype.StraightProjectile)
        {
            this.targetPosition = targetMonster.transform.position;
        }

        arcTravelTime = CalculateArcTravelTime();
        isInitialized = true;
    }

    private bool CanInitialize()
    {
        if (projectileConfig == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: projectile config is null.", this);
            return false;
        }

        if (attackConfig == null)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: attack config is null.", this);
            return false;
        }

        if (projectileConfig.ProjectileSpeed <= 0f)
        {
            Debug.LogWarning("Projectile behaviour cannot initialize: projectile speed must be greater than zero.", projectileConfig);
            return false;
        }

        if (attackConfig.AttackArchetype == AttackArchetype.StraightProjectile)
        {
            if (!IsValidTarget(targetMonster))
            {
                Debug.LogWarning("Projectile behaviour cannot initialize straight projectile: target monster is invalid.", this);
                return false;
            }

            return true;
        }

        if (attackConfig.AttackArchetype == AttackArchetype.ArcProjectile)
        {
            return true;
        }

        Debug.LogWarning($"Projectile behaviour cannot initialize: unsupported attack archetype '{attackConfig.AttackArchetype}'.", this);
        return false;
    }

    private void Update()
    {
        if (!isInitialized || hasImpacted)
        {
            return;
        }

        elapsedLifetime += Time.deltaTime;

        if (elapsedLifetime >= projectileConfig.MaxLifetime)
        {
            DestroyProjectile();
            return;
        }

        switch (attackConfig.AttackArchetype)
        {
            case AttackArchetype.StraightProjectile:
                UpdateStraightProjectile();
                break;
            case AttackArchetype.ArcProjectile:
                UpdateArcProjectile();
                break;
            default:
                DestroyProjectile();
                break;
        }
    }

    private void UpdateStraightProjectile()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            projectileConfig.ProjectileSpeed * Time.deltaTime
        );

        FaceMoveDirection(targetPosition - transform.position);

        if (Vector3.Distance(transform.position, targetPosition) > projectileConfig.HitDistanceThreshold)
        {
            return;
        }

        if (IsValidTarget(targetMonster) &&
            Vector3.Distance(targetMonster.transform.position, transform.position) <= projectileConfig.HitDistanceThreshold)
        {
            ImpactStraightProjectile(targetMonster);
            return;
        }

        DestroyProjectile();
    }

    private void UpdateArcProjectile()
    {
        float progress = Mathf.Clamp01(elapsedLifetime / arcTravelTime);
        Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        nextPosition.y += Mathf.Sin(progress * Mathf.PI) * attackConfig.ArcHeight;
        transform.position = nextPosition;

        FaceMoveDirection(targetPosition - transform.position);

        if (progress < 1f && Vector3.Distance(transform.position, targetPosition) > projectileConfig.HitDistanceThreshold)
        {
            return;
        }

        ImpactArcProjectile();
    }

    private float CalculateArcTravelTime()
    {
        float distance = Vector3.Distance(startPosition, targetPosition);
        return Mathf.Max(distance / projectileConfig.ProjectileSpeed, 0.01f);
    }

    private void FaceMoveDirection(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.FromToRotation(Vector3.down, moveDirection.normalized);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleMonsterCollision(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        TryHandleMonsterCollision(collision.collider);
    }

    private void TryHandleMonsterCollision(Collider hitCollider)
    {
        if (!isInitialized ||
            hasImpacted ||
            attackConfig.AttackArchetype != AttackArchetype.StraightProjectile ||
            hitCollider == null)
        {
            return;
        }

        MonsterBehaviour hitMonster = hitCollider.GetComponentInParent<MonsterBehaviour>();

        if (!IsValidTarget(hitMonster))
        {
            return;
        }

        ImpactStraightProjectile(hitMonster);
    }

    private void ImpactStraightProjectile(MonsterBehaviour hitMonster)
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        hitMonster.TakeDamage(attackConfig.Damage);
        RaiseImpact(hitMonster, transform.position);
        DestroyProjectile();
    }

    private void ImpactArcProjectile()
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        RaiseImpact(null, targetPosition);
        DestroyProjectile();
    }

    private void RaiseImpact(MonsterBehaviour hitMonster, Vector3 impactPosition)
    {
        ProjectileImpactContext impactContext = new ProjectileImpactContext(
            sourceTower,
            projectileConfig,
            attackConfig,
            hitMonster,
            impactPosition,
            attackConfig.Damage,
            projectileConfig.ImpactEffectConfig
        );

        OnImpact?.Invoke(impactContext);
        AreaDamageEffectExecutor.Execute(impactContext);
    }

    private static bool IsValidTarget(MonsterBehaviour monster)
    {
        return monster != null &&
               monster.gameObject.activeInHierarchy &&
               !monster.IsDead();
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
