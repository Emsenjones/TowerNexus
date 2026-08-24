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
    [SerializeField] private float activeDuration = 1f;
    [TitleGroup("Lifecycle")]
    [LabelText("Periodic Tick Interval")]
    [MinValue(0f)]
    [SerializeField] private float periodicTickInterval;

    [TitleGroup("Stacking")]
    [SerializeField] private bool usesStacks;
    [TitleGroup("Stacking")]
    [ShowIf(nameof(UsesStacks))]
    [LabelText("Maximum Stacks")]
    [MinValue(2)]
    [SerializeField] private int maximumStacks = 1;
    [TitleGroup("Stacking")]
    [ShowIf(nameof(UsesStacks))]
    [LabelText("Per-Source Stack Cooldown")]
    [MinValue(0f)]
    [SerializeField] private float sourceApplyCooldown;

    [TitleGroup("Buff Event Bindings")]
    [SerializeField] private List<BuffEventBinding> eventBindings = new List<BuffEventBinding>();

    [TitleGroup("Elemental")]
    [SerializeField] private ElementType elementType;
    [TitleGroup("Elemental")]
    [ShowIf(nameof(UsesStacks))]
    [LabelText("Post-Overload Protection Duration")]
    [MinValue(0f)]
    [SerializeField] private float overloadProtectionDuration;

    [TitleGroup("Visual Feedback")]
    [SerializeField] private Sprite statusIcon;
    [TitleGroup("Visual Feedback")]
    [SerializeField] private GameObject persistentBuffVfxPrefab;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public string Description => description;
    public float ActiveDuration => activeDuration;
    public float PeriodicTickInterval => periodicTickInterval;
    public bool UsesStacks => usesStacks;
    public int MaximumStacks => usesStacks ? Mathf.Max(2, maximumStacks) : 1;
    public float SourceApplyCooldown => usesStacks ? Mathf.Max(0f, sourceApplyCooldown) : 0f;
    public IReadOnlyList<BuffEventBinding> EventBindings => eventBindings;
    public ElementType ElementType => elementType;
    public float OverloadProtectionDuration => usesStacks ? Mathf.Max(0f, overloadProtectionDuration) : 0f;
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

        if (activeDuration <= 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: active duration must be greater than zero.", this);
            isValid = false;
        }

        if (periodicTickInterval < 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: periodic tick interval cannot be negative.", this);
            isValid = false;
        }

        if (usesStacks && maximumStacks < 2)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: stackable buffs require at least two maximum stacks.", this);
            isValid = false;
        }

        if (usesStacks && sourceApplyCooldown < 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: source apply cooldown cannot be negative.", this);
            isValid = false;
        }

        if (usesStacks && overloadProtectionDuration < 0f)
        {
            Debug.LogWarning($"Buff definition '{name}' is invalid: overload protection duration cannot be negative.", this);
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

            if (!eventBinding.IsValidForBuffLifecycle())
            {
                Debug.LogWarning($"Buff definition '{name}' is invalid: buff event binding '{eventBinding.EventType}' must use {eventBinding.RequiredDamageMode} for every nested DealDamage action.", this);
                isValid = false;
            }

            if (!usesStacks && UsesStackingOnlyEvent(eventBinding.EventType))
            {
                Debug.LogWarning($"Buff definition '{name}' is invalid: non-stackable buffs cannot bind '{eventBinding.EventType}'.", this);
                isValid = false;
            }

            if (eventBinding.EventType == BuffEventType.PeriodicTick && periodicTickInterval <= 0f)
            {
                Debug.LogWarning($"Buff definition '{name}' is invalid: PeriodicTick requires a positive periodic tick interval.", this);
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
