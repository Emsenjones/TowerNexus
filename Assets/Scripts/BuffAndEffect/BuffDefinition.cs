using System.Collections.Generic;
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
    [SerializeField] private bool usesStacks;
    [TitleGroup("Stacking")]
    [ShowIf(nameof(UsesStacks))]
    [MinValue(2)]
    [SerializeField] private int maxStacks = 1;
    [TitleGroup("Stacking")]
    [ShowIf(nameof(UsesStacks))]
    [MinValue(0f)]
    [SerializeField] private float buffApplyCooldown;

    [TitleGroup("Buff Event Bindings")]
    [SerializeField] private List<BuffEventBinding> eventBindings = new List<BuffEventBinding>();

    [TitleGroup("Elemental")]
    [SerializeField] private ElementType elementType;
    [TitleGroup("Elemental")]
    [ShowIf(nameof(UsesStacks))]
    [MinValue(0f)]
    [SerializeField] private float protectionDuration;

    [TitleGroup("Visual Feedback")]
    [SerializeField] private Sprite statusIcon;
    [TitleGroup("Visual Feedback")]
    [SerializeField] private GameObject persistentBuffVfxPrefab;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public float Duration => duration;
    public float TickInterval => tickInterval;
    public bool UsesStacks => usesStacks;
    public int MaxStacks => usesStacks ? Mathf.Max(2, maxStacks) : 1;
    public float BuffApplyCooldown => usesStacks ? Mathf.Max(0f, buffApplyCooldown) : 0f;
    public IReadOnlyList<BuffEventBinding> EventBindings => eventBindings;
    public ElementType ElementType => elementType;
    public float ProtectionDuration => usesStacks ? Mathf.Max(0f, protectionDuration) : 0f;
    public Sprite StatusIcon => statusIcon;
    public GameObject PersistentBuffVfxPrefab => persistentBuffVfxPrefab;

    public EffectDefinition GetEffectDefinition(BuffEventType eventType)
    {
        if (eventBindings == null)
        {
            return null;
        }

        for (int i = 0; i < eventBindings.Count; i++)
        {
            BuffEventBinding eventBinding = eventBindings[i];

            if (eventBinding != null && eventBinding.EventType == eventType)
            {
                return eventBinding.EffectDefinition;
            }
        }

        return null;
    }

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

        if (usesStacks && maxStacks < 2)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: stackable buffs require at least two max stacks.", this);
            isValid = false;
        }

        if (usesStacks && buffApplyCooldown < 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: buff apply cooldown cannot be negative.", this);
            isValid = false;
        }

        if (usesStacks && protectionDuration < 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: protection duration cannot be negative.", this);
            isValid = false;
        }

        isValid &= AreEventBindingsValid();

        return isValid;
    }

    private bool AreEventBindingsValid()
    {
        if (eventBindings == null || eventBindings.Count == 0)
        {
            return true;
        }

        bool isValid = true;
        HashSet<BuffEventType> authoredEventTypes = new HashSet<BuffEventType>();

        for (int i = 0; i < eventBindings.Count; i++)
        {
            BuffEventBinding eventBinding = eventBindings[i];

            if (eventBinding == null)
            {
                Debug.LogWarning($"Buff definition '{name}' is invalid: buff event binding at index {i} is missing.", this);
                isValid = false;
                continue;
            }

            if (!authoredEventTypes.Add(eventBinding.EventType))
            {
                Debug.LogWarning($"Buff definition '{name}' is invalid: duplicate buff event binding for '{eventBinding.EventType}'.", this);
                isValid = false;
            }

            if (!eventBinding.IsValid())
            {
                Debug.LogWarning($"Buff definition '{name}' is invalid: buff event binding '{eventBinding.EventType}' failed validation.", this);
                isValid = false;
            }

            if (!usesStacks && UsesStackingOnlyEvent(eventBinding.EventType))
            {
                Debug.LogWarning($"Buff definition '{name}' is invalid: non-stackable buffs cannot bind '{eventBinding.EventType}'.", this);
                isValid = false;
            }

            if (eventBinding.EventType == BuffEventType.PeriodicTick && tickInterval <= 0f)
            {
                Debug.LogWarning($"Buff definition '{name}' is invalid: PeriodicTick requires a positive tick interval.", this);
                isValid = false;
            }
        }

        return isValid;
    }

    private static bool UsesStackingOnlyEvent(BuffEventType eventType)
    {
        return eventType == BuffEventType.StackApplied ||
               eventType == BuffEventType.Overload ||
               eventType == BuffEventType.EnteredProtection;
    }
}
