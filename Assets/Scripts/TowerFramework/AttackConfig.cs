using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AttackConfig",
    menuName = "Tower Nexus/Attack Config"
)]
public class AttackConfig : ScriptableObject
{
    [TitleGroup("Identity")]
    [Required]
    [SerializeField] private string attackConfigId;

    [TitleGroup("Core")]
    [SerializeField] private AttackArchetype attackArchetype;
    [TitleGroup("Core")]
    [MinValue(0f)]
    [SerializeField] private float attackRange = 1f;
    [TitleGroup("Core")]
    [MinValue(0f)]
    [SerializeField] private float attackInterval = 1f;
    [TitleGroup("Core")]
    [SerializeField] private TargetSelectionType targetSelectionType;
    [TitleGroup("Core")]
    [MinValue(0f)]
    [SerializeField] private float damage = 1f;

    [TitleGroup("Projectile")]
    [ShowIf(nameof(UsesProjectile))]
    [SerializeField] private string projectileConfigId;

    [TitleGroup("Effect Reference")]
    [ShowIf(nameof(UsesEffectReference))]
    [SerializeField] private string effectConfigId;

    [TitleGroup("Area")]
    [ShowIf(nameof(UsesExplosionRadius))]
    [MinValue(0f)]
    [SerializeField] private float explosionRadius;
    [TitleGroup("Area")]
    [ShowIf(nameof(UsesAreaTickInterval))]
    [MinValue(0f)]
    [SerializeField] private float areaTickInterval = 1f;

    [TitleGroup("Channel")]
    [ShowIf(nameof(IsChannelBeam))]
    [MinValue(0f)]
    [SerializeField] private float damagePerSecond;
    [TitleGroup("Channel")]
    [ShowIf(nameof(IsChannelBeam))]
    [MinValue(0f)]
    [SerializeField] private float maxChannelDuration = 1f;

    public string AttackConfigId => attackConfigId;
    public AttackArchetype AttackArchetype => attackArchetype;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;
    public TargetSelectionType TargetSelectionType => targetSelectionType;
    public float Damage => damage;
    public string ProjectileConfigId => projectileConfigId;
    public string EffectConfigId => effectConfigId;
    public float ExplosionRadius => explosionRadius;
    public float DamagePerSecond => damagePerSecond;
    public float MaxChannelDuration => maxChannelDuration;
    public float AreaTickInterval => areaTickInterval;

    private bool UsesProjectile()
    {
        return attackArchetype == AttackArchetype.StraightProjectile ||
               attackArchetype == AttackArchetype.ArcProjectile;
    }

    private bool UsesEffectReference()
    {
        return attackArchetype == AttackArchetype.ArcProjectile;
    }

    private bool UsesExplosionRadius()
    {
        return attackArchetype == AttackArchetype.ArcProjectile ||
               attackArchetype == AttackArchetype.PeriodicArea;
    }

    private bool UsesAreaTickInterval()
    {
        return attackArchetype == AttackArchetype.PeriodicArea;
    }

    private bool IsChannelBeam()
    {
        return attackArchetype == AttackArchetype.ChannelBeam;
    }

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(attackConfigId))
        {
            Debug.LogWarning("Attack config is invalid: attack config id is missing.", this);
            return false;
        }

        if (attackRange < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: attack range cannot be negative.", this);
            return false;
        }

        if (attackInterval < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: attack interval cannot be negative.", this);
            return false;
        }

        if (damage < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: damage cannot be negative.", this);
            return false;
        }

        if (explosionRadius < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: explosion radius cannot be negative.", this);
            return false;
        }

        if (damagePerSecond < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: damage per second cannot be negative.", this);
            return false;
        }

        if (maxChannelDuration < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: max channel duration cannot be negative.", this);
            return false;
        }

        if (areaTickInterval < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: area tick interval cannot be negative.", this);
            return false;
        }

        return true;
    }
}
