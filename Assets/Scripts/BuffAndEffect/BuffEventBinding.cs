using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class BuffEventBinding
{
    [TitleGroup("Buff Event Binding")]
    [SerializeField] private BuffEventType eventType;
    [TitleGroup("Buff Event Binding")]
    [SerializeField] private EffectDefinition effectDefinition;

    public BuffEventType EventType => eventType;
    public EffectDefinition EffectDefinition => effectDefinition;
    public EffectDamageMode RequiredDamageMode =>
        eventType == BuffEventType.StackApplied
            ? EffectDamageMode.TowerScaled
            : EffectDamageMode.FixedBuff;

    public bool IsValid()
    {
        return effectDefinition != null && effectDefinition.IsValid();
    }

    public bool IsValidForBuffLifecycle()
    {
        return effectDefinition != null &&
               effectDefinition.IsValidForDamageMode(
                   RequiredDamageMode);
    }
}
