using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WindVortexConfig",
    menuName = "Tower Nexus/Wind Vortex Config"
)]
public class WindVortexConfig : ScriptableObject
{
    [TitleGroup("Runtime Prefab")]
    [Required]
    [SerializeField] private GameObject windVortexPrefab;

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

    public GameObject WindVortexPrefab => windVortexPrefab;
    public float Lifetime => lifetime;
    public float MovementSpeed => movementSpeed;
    public float TargetSearchRadius => targetSearchRadius;
    public float ArrivalThreshold => arrivalThreshold;
    public float DamageRadius => damageRadius;
    public float TickInterval => tickInterval;
    public EffectDefinition OnTickEffectDefinition => onTickEffectDefinition;

    public bool IsValid()
    {
        bool isValid = true;

        if (windVortexPrefab == null || !windVortexPrefab.TryGetComponent(out WindVortexBehaviour _))
        {
            Debug.LogWarning($"Wind vortex config '{name}' is invalid: runtime prefab requires WindVortexBehaviour on its root.", this);
            isValid = false;
        }

        if (lifetime <= 0f || movementSpeed <= 0f || arrivalThreshold <= 0f || tickInterval <= 0f ||
            targetSearchRadius < 0f || damageRadius < 0f)
        {
            Debug.LogWarning($"Wind vortex config '{name}' is invalid: lifetime, movement speed, arrival threshold, and tick interval must be positive; radii cannot be negative.", this);
            isValid = false;
        }

        if (onTickEffectDefinition == null)
        {
            Debug.LogWarning($"Wind vortex config '{name}' is invalid: on-tick EffectDefinition is required.", this);
            return false;
        }

        if (onTickEffectDefinition.Radius > 0f)
        {
            Debug.LogWarning($"Wind vortex config '{name}' is invalid: on-tick EffectDefinition must be single-target.", this);
            isValid = false;
        }

        if (!onTickEffectDefinition.IsValid())
        {
            isValid = false;
        }

        return isValid;
    }
}
