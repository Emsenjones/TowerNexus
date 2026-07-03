using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ProjectileConfig",
    menuName = "Tower Nexus/Projectile Config"
)]
public class ProjectileConfig : ScriptableObject
{
    [TitleGroup("Prefab")]
    [Required]
    [SerializeField] private GameObject projectilePrefab;

    [TitleGroup("Movement")]
    [MinValue(0f)]
    [SerializeField] private float projectileSpeed = 1f;
    [TitleGroup("Movement")]
    [MinValue(0f)]
    [SerializeField] private float hitDistanceThreshold = 0.1f;
    [TitleGroup("Movement")]
    [MinValue(0f)]
    [SerializeField] private float maxLifetime = 10f;

    [TitleGroup("Impact Gameplay")]
    [SerializeField] private EffectDefinition impactEffectDefinition;
    [TitleGroup("Impact VFX")]
    [SerializeField] private GameObject impactVfxPrefab;

    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float HitDistanceThreshold => hitDistanceThreshold;
    public float MaxLifetime => maxLifetime;
    public EffectDefinition ImpactEffectDefinition => impactEffectDefinition;
    public GameObject ImpactVfxPrefab => impactVfxPrefab;

    public bool IsValid()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile config is invalid: projectile prefab is not assigned.", this);
            return false;
        }

        if (projectileSpeed <= 0f)
        {
            Debug.LogWarning("Projectile config is invalid: projectile speed must be greater than zero.", this);
            return false;
        }

        if (maxLifetime <= 0f)
        {
            Debug.LogWarning("Projectile config is invalid: max lifetime must be greater than zero.", this);
            return false;
        }

        return true;
    }
}
