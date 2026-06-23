using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
    [ShowIf(nameof(UsesProjectileConfig))]
    [SerializeField] private ProjectileConfig projectileConfig;
    [TitleGroup("Projectile")]
    [ShowIf(nameof(IsArcProjectile))]
    [MinValue(0f)]
    [SerializeField] private float arcHeight = 1f;
    [TitleGroup("Attack VFX")]
    [ShowIf(nameof(UsesAttackReleaseVfx))]
    [FormerlySerializedAs("projectileReleaseVfxPrefab")]
    [SerializeField] private GameObject attackReleaseVfxPrefab;

    [TitleGroup("Magic Orb")]
    [ShowIf(nameof(IsMagicOrb))]
    [MinValue(0f)]
    [SerializeField] private float magicOrbRotationSpeed = 180f;
    [TitleGroup("Magic Orb")]
    [ShowIf(nameof(IsMagicOrb))]
    [MinValue(0f)]
    [SerializeField] private float magicOrbOrbitRadius = 0.75f;
    [TitleGroup("Magic Orb")]
    [ShowIf(nameof(IsMagicOrb))]
    [MinValue(0f)]
    [SerializeField] private float magicOrbContactDistance = 0.25f;
    [TitleGroup("Magic Orb")]
    [ShowIf(nameof(IsMagicOrb))]
    [MinValue(1)]
    [SerializeField] private int magicOrbMaxHitCount = 3;
    [TitleGroup("Magic Orb")]
    [ShowIf(nameof(IsMagicOrb))]
    [MinValue(0f)]
    [SerializeField] private float magicOrbSameTargetHitCooldown = 0.5f;
    [TitleGroup("Magic Orb")]
    [ShowIf(nameof(IsMagicOrb))]
    [SerializeField] private GameObject magicOrbPrefab;

    [TitleGroup("Drone")]
    [ShowIf(nameof(IsDrone))]
    [MinValue(0.01f)]
    [SerializeField] private float droneBatteryDuration = 5f;
    [TitleGroup("Drone")]
    [ShowIf(nameof(IsDrone))]
    [MinValue(0f)]
    [SerializeField] private float droneRechargeDuration = 2f;
    [TitleGroup("Drone")]
    [ShowIf(nameof(IsDrone))]
    [MinValue(0f)]
    [SerializeField] private float droneHoverDistance = 1.5f;
    [TitleGroup("Drone")]
    [ShowIf(nameof(IsDrone))]
    [MinValue(0.01f)]
    [SerializeField] private float droneMoveSpeed = 3f;
    [TitleGroup("Drone")]
    [ShowIf(nameof(IsDrone))]
    [MinValue(0f)]
    [SerializeField] private float droneLaunchHeight = 1f;
    [TitleGroup("Drone")]
    [ShowIf(nameof(IsDrone))]
    [SerializeField] private ProjectileConfig droneProjectileConfig;
    [TitleGroup("Drone")]
    [ShowIf(nameof(IsDrone))]
    [SerializeField] private GameObject dronePrefab;

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
    public GameObject AttackReleaseVfxPrefab => attackReleaseVfxPrefab;
    public float MagicOrbRotationSpeed => magicOrbRotationSpeed;
    public float MagicOrbOrbitRadius => magicOrbOrbitRadius;
    public float MagicOrbContactDistance => magicOrbContactDistance;
    public int MagicOrbMaxHitCount => magicOrbMaxHitCount;
    public float MagicOrbSameTargetHitCooldown => magicOrbSameTargetHitCooldown;
    public GameObject MagicOrbPrefab => magicOrbPrefab;
    public float DroneBatteryDuration => droneBatteryDuration;
    public float DroneRechargeDuration => droneRechargeDuration;
    public float DroneHoverDistance => droneHoverDistance;
    public float DroneMoveSpeed => droneMoveSpeed;
    public float DroneLaunchHeight => droneLaunchHeight;
    public ProjectileConfig DroneProjectileConfig => droneProjectileConfig;
    public GameObject DronePrefab => dronePrefab;

    private bool UsesProjectileConfig()
    {
        return attackArchetype == AttackArchetype.DirectionProjectile ||
               attackArchetype == AttackArchetype.ArcProjectile ||
               attackArchetype == AttackArchetype.TrackingProjectile;
    }

    private bool UsesAttackReleaseVfx()
    {
        return UsesProjectileConfig() ||
               attackArchetype == AttackArchetype.MagicOrb ||
               attackArchetype == AttackArchetype.Drone;
    }

    private bool RequiresProjectileConfig()
    {
        return attackArchetype == AttackArchetype.DirectionProjectile ||
               attackArchetype == AttackArchetype.ArcProjectile;
    }

    private bool IsArcProjectile()
    {
        return attackArchetype == AttackArchetype.ArcProjectile;
    }

    private bool UsesTargetSelection()
    {
        return attackArchetype == AttackArchetype.DirectionProjectile ||
               attackArchetype == AttackArchetype.ArcProjectile ||
               attackArchetype == AttackArchetype.TrackingProjectile ||
               attackArchetype == AttackArchetype.Drone;
    }

    private bool IsMagicOrb()
    {
        return attackArchetype == AttackArchetype.MagicOrb;
    }

    private bool IsDrone()
    {
        return attackArchetype == AttackArchetype.Drone;
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

        if (damage < 0)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: damage cannot be negative.", this);
            return false;
        }

        if (RequiresProjectileConfig() && projectileConfig == null)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: projectile config is not assigned.", this);
            return false;
        }

        if (arcHeight < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: arc height cannot be negative.", this);
            return false;
        }

        if (magicOrbRotationSpeed < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: magic orb rotation speed cannot be negative.", this);
            return false;
        }

        if (magicOrbOrbitRadius < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: magic orb orbit radius cannot be negative.", this);
            return false;
        }

        if (magicOrbContactDistance < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: magic orb contact distance cannot be negative.", this);
            return false;
        }

        if (magicOrbMaxHitCount <= 0)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: magic orb max hit count must be greater than zero.", this);
            return false;
        }

        if (magicOrbSameTargetHitCooldown < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: magic orb same target hit cooldown cannot be negative.", this);
            return false;
        }

        if (droneBatteryDuration <= 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: drone battery duration must be greater than zero.", this);
            return false;
        }

        if (droneRechargeDuration < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: drone recharge duration cannot be negative.", this);
            return false;
        }

        if (droneHoverDistance < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: drone hover distance cannot be negative.", this);
            return false;
        }

        if (droneMoveSpeed <= 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: drone move speed must be greater than zero.", this);
            return false;
        }

        if (droneLaunchHeight < 0f)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: drone launch height cannot be negative.", this);
            return false;
        }

        if (attackArchetype == AttackArchetype.Drone && droneProjectileConfig == null)
        {
            Debug.LogWarning($"Attack config '{attackConfigId}' is invalid: drone projectile config is not assigned.", this);
            return false;
        }

        return true;
    }
}
