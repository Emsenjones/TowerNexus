using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BuffDefinition",
    menuName = "Tower Nexus/Buff Definition"
)]
public class BuffDefinition : ScriptableObject
{
    [TitleGroup("Debug")]
    [SerializeField] private string displayName;
    [TitleGroup("Debug")]
    [TextArea]
    [SerializeField] private string description;

    [TitleGroup("Lifecycle")]
    [MinValue(0f)]
    [SerializeField] private float duration = 1f;
    [TitleGroup("Lifecycle")]
    [MinValue(0f)]
    [SerializeField] private float tickInterval;

    [TitleGroup("Stacking")]
    [MinValue(1)]
    [SerializeField] private int maxStacks = 1;
    [TitleGroup("Stacking")]
    [MinValue(0f)]
    [SerializeField] private float buffApplyCooldown;

    [TitleGroup("Tick Effect")]
    [SerializeField] private EffectDefinition tickEffectDefinition;

    [TitleGroup("Elemental")]
    [SerializeField] private ElementType elementType;
    [TitleGroup("Elemental")]
    [SerializeField] private bool isElementalStackImmunity;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public float Duration => duration;
    public float TickInterval => tickInterval;
    public int MaxStacks => Mathf.Max(1, maxStacks);
    public float BuffApplyCooldown => Mathf.Max(0f, buffApplyCooldown);
    public EffectDefinition TickEffectDefinition => tickEffectDefinition;
    public ElementType ElementType => elementType;
    public bool IsElementalStackImmunity => isElementalStackImmunity;

    public bool IsValid()
    {
        bool isValid = true;

        if (duration <= 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: duration must be greater than zero.", this);
            isValid = false;
        }

        if (tickInterval < 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: tick interval cannot be negative.", this);
            isValid = false;
        }

        if (maxStacks < 1)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: max stacks must be at least 1.", this);
            isValid = false;
        }

        if (buffApplyCooldown < 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: buff apply cooldown cannot be negative.", this);
            isValid = false;
        }

        if (isElementalStackImmunity && elementType == ElementType.None)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: elemental stack immunity requires an element type.", this);
            isValid = false;
        }

        return isValid;
    }
}
