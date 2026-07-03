using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class EffectBinding
{
    [TitleGroup("Effect Binding")]
    [SerializeField] private EffectTriggerType triggerType;
    [TitleGroup("Effect Binding")]
    [SerializeField] private EffectDefinition effectDefinition;

    public EffectTriggerType TriggerType => triggerType;
    public EffectDefinition EffectDefinition => effectDefinition;

    public bool IsValid()
    {
        return effectDefinition != null && effectDefinition.IsValid();
    }
}
