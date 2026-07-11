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
    [MinValue(1)]
    [SerializeField] private int maxStacks = 1;
    [TitleGroup("Stacking")]
    [MinValue(0f)]
    [SerializeField] private float buffApplyCooldown;

    [TitleGroup("Buff Event Bindings")]
    [SerializeField] private List<BuffEventBinding> eventBindings = new List<BuffEventBinding>();

    [TitleGroup("Elemental")]
    [SerializeField] private ElementType elementType;
    [TitleGroup("Elemental")]
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
    public int MaxStacks => Mathf.Max(1, maxStacks);
    public float BuffApplyCooldown => Mathf.Max(0f, buffApplyCooldown);
    public IReadOnlyList<BuffEventBinding> EventBindings => eventBindings;
    public ElementType ElementType => elementType;
    public float ProtectionDuration => Mathf.Max(0f, protectionDuration);
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

        if (protectionDuration < 0f)
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
        }

        return isValid;
    }
}
