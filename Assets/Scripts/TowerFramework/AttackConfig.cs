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
    [ShowIf(nameof(UsesTargetSelection))]
    [SerializeField] private TargetSelectionType targetSelectionType;
    [TitleGroup("Core")]
    [MinValue(0)]
    [SerializeField] private int damage = 1;

    [TitleGroup("Animation")]
    [SerializeField] private string attackAnimatorTriggerName = "Attack";
    [TitleGroup("Animation")]
    [SerializeField] private string attackingAnimatorBoolName = "IsAttacking";

    [TitleGroup("Projectile")]
    [ShowIf(nameof(UsesProjectile))]
    [Required]
    [SerializeField] private ProjectileConfig projectileConfig;
    [TitleGroup("Projectile")]
    [ShowIf(nameof(IsArcProjectile))]
    [MinValue(0f)]
    [SerializeField] private float arcHeight = 1f;
    [TitleGroup("Projectile VFX")]
    [ShowIf(nameof(UsesProjectile))]
    [SerializeField] private GameObject projectileReleaseVfxPrefab;
    
    [TitleGroup("Channel")]
    [ShowIf(nameof(IsChannelBeam))]
    [MinValue(0.01f)]
    [SerializeField] private float channelDamageInterval = 0.5f;
    [TitleGroup("Channel")]
    [ShowIf(nameof(IsChannelBeam))]
    [MinValue(0f)]
    [SerializeField] private float maxChannelDuration = 5f;
    [TitleGroup("Channel VFX")]
    [ShowIf(nameof(IsChannelBeam))]
    [SerializeField] private GameObject channelBeamVfxPrefab;

    [TitleGroup("Periodic Area VFX")]
    [ShowIf(nameof(IsPeriodicArea))]
    [SerializeField] private GameObject periodicAreaVfxPrefab;

    public string AttackConfigId => attackConfigId;
    public AttackArchetype AttackArchetype => attackArchetype;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;
    public TargetSelectionType TargetSelectionType => targetSelectionType;
    public int Damage => damage;
    public string AttackAnimatorTriggerName => attackAnimatorTriggerName;
    public string AttackingAnimatorBoolName => attackingAnimatorBoolName;
    public ProjectileConfig ProjectileConfig => projectileConfig;
    public float ArcHeight => arcHeight;
    public GameObject ProjectileReleaseVfxPrefab => projectileReleaseVfxPrefab;
    public float ChannelDamageInterval => channelDamageInterval;
    public float MaxChannelDuration => maxChannelDuration;
    public GameObject ChannelBeamVfxPrefab => channelBeamVfxPrefab;
    public GameObject PeriodicAreaVfxPrefab => periodicAreaVfxPrefab;

    private bool UsesProjectile()
    {
        return attackArchetype == AttackArchetype.StraightProjectile ||
               attackArchetype == AttackArchetype.ArcProjectile;
    }

    private bool IsArcProjectile()
    {
        return attackArchetype == AttackArchetype.ArcProjectile;
    }

    private bool UsesTargetSelection()
    {
        return attackArchetype != AttackArchetype.PeriodicArea;
    }


    private bool IsChannelBeam()
    {
        return attackArchetype == AttackArchetype.ChannelBeam;
    }

    private bool IsPeriodicArea()
    {
        return attackArchetype == AttackArchetype.PeriodicArea;
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

        if (UsesProjectile() && projectileConfig == null)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: projectile config is not assigned.", this);
            return false;
        }

        if (arcHeight < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: arc height cannot be negative.", this);
            return false;
        }

        if (channelDamageInterval <= 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: channel damage interval must be greater than zero.", this);
            return false;
        }

        if (maxChannelDuration < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: max channel duration cannot be negative.", this);
            return false;
        }


        return true;
    }
}
