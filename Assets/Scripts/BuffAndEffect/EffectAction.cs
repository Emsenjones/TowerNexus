using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class EffectAction
{
    [TitleGroup("Effect Action")]
    [SerializeField] private EffectActionType actionType;
    [TitleGroup("Effect Action")]
    [ShowIf(nameof(IsDealDamageAction))]
    [MinValue(0)]
    [SerializeField] private int damageAmount;
    [TitleGroup("Effect Action")]
    [ShowIf(nameof(IsApplyBuffAction))]
    [SerializeField] private BuffDefinition buffDefinition;

    public EffectActionType ActionType => actionType;
    public int DamageAmount => Mathf.Max(0, damageAmount);
    public BuffDefinition BuffDefinition => buffDefinition;

    public bool IsValid()
    {
        if (actionType == EffectActionType.ApplyBuff && buffDefinition == null)
        {
            Debug.LogWarning("Effect action is invalid: ApplyBuff action requires a BuffDefinition.");
            return false;
        }

        if (actionType == EffectActionType.ApplyBuff && !buffDefinition.IsValid())
        {
            return false;
        }

        return true;
    }

    private bool IsApplyBuffAction()
    {
        return actionType == EffectActionType.ApplyBuff;
    }

    private bool IsDealDamageAction()
    {
        return actionType == EffectActionType.DealDamage;
    }
}
