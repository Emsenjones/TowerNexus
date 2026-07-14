using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

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
    [TitleGroup("Effect Action")]
    [ShowIf(nameof(IsSetMoveSpeedMultiplierAction))]
    [Range(0.01f, 0.99f)]
    [FormerlySerializedAs("slowMultiplier")]
    [SerializeField] private float moveSpeedMultiplier = 0.5f;
    [TitleGroup("Effect Action")]
    [ShowIf(nameof(IsSetMovementLockAction))]
    [SerializeField] private bool isMovementLocked;
    [TitleGroup("Multi-Target Effect")]
    [ShowIf(nameof(IsExecuteMultiTargetEffectAction))]
    [FormerlySerializedAs("lightningStrikeEffect")]
    [FormerlySerializedAs("sequentialEffectDefinition")]
    [SerializeField] private EffectDefinition multiTargetEffectDefinition;
    [TitleGroup("Multi-Target Effect")]
    [ShowIf(nameof(IsExecuteMultiTargetEffectAction))]
    [FormerlySerializedAs("overchargedStrikeCount")]
    [MinValue(1)]
    [SerializeField] private int targetCount = 1;
    [FormerlySerializedAs("excludeTriggerContextTarget")]
    [TitleGroup("Multi-Target Effect")]
    [ShowIf(nameof(IsExecuteMultiTargetEffectAction))]
    [SerializeField] private bool excludeTriggerTarget;
    [TitleGroup("Wind Vortex")]
    [ShowIf(nameof(IsSpawnWindVortexAction))]
    [SerializeField] private WindVortexConfig windVortexConfig;

    public EffectActionType ActionType => actionType;
    public int DamageAmount => Mathf.Max(0, damageAmount);
    public BuffDefinition BuffDefinition => buffDefinition;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public bool IsMovementLocked => isMovementLocked;
    public EffectDefinition MultiTargetEffectDefinition => multiTargetEffectDefinition;
    public int TargetCount => Mathf.Max(1, targetCount);
    public bool ExcludeTriggerContextTarget => excludeTriggerTarget;
    public WindVortexConfig WindVortexConfig => windVortexConfig;

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

        if (actionType == EffectActionType.SetMoveSpeedMultiplier &&
            (float.IsNaN(moveSpeedMultiplier) || float.IsInfinity(moveSpeedMultiplier) || moveSpeedMultiplier <= 0f || moveSpeedMultiplier >= 1f))
        {
            Debug.LogWarning("Effect action is invalid: SetMoveSpeedMultiplier requires a multiplier greater than zero and less than one.");
            return false;
        }

        if (actionType == EffectActionType.ExecuteMultiTargetEffect)
        {
            if (multiTargetEffectDefinition == null)
            {
                Debug.LogWarning("Effect action is invalid: ExecuteMultiTargetEffect requires an EffectDefinition.");
                return false;
            }

            if (multiTargetEffectDefinition.Radius > 0f)
            {
                Debug.LogWarning("Effect action is invalid: ExecuteMultiTargetEffect requires a single-target EffectDefinition.");
                return false;
            }

            if (!multiTargetEffectDefinition.IsValid())
            {
                return false;
            }
        }

        if (actionType == EffectActionType.SpawnWindVortex)
        {
            if (windVortexConfig == null)
            {
                Debug.LogWarning("Effect action is invalid: SpawnWindVortex requires a WindVortexConfig.");
                return false;
            }

            if (!windVortexConfig.IsValid())
            {
                return false;
            }
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

    private bool IsSetMoveSpeedMultiplierAction()
    {
        return actionType == EffectActionType.SetMoveSpeedMultiplier;
    }

    private bool IsSetMovementLockAction()
    {
        return actionType == EffectActionType.SetMovementLock;
    }

    private bool IsExecuteMultiTargetEffectAction()
    {
        return actionType == EffectActionType.ExecuteMultiTargetEffect;
    }

    private bool IsSpawnWindVortexAction()
    {
        return actionType == EffectActionType.SpawnWindVortex;
    }
}
