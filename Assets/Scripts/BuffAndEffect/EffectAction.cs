using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public enum EffectDamageMode
{
    None = 0,
    TowerScaled = 1,
    FixedBuff = 2
}

[Serializable]
public class EffectAction
{
    [TitleGroup("Effect Action")]
    [SerializeField] private EffectActionType actionType;
    [TitleGroup("Effect Action")]
    [ShowIf(nameof(IsDealDamageAction))]
    [SerializeField] private EffectDamageMode damageMode;
    [TitleGroup("Effect Action")]
    [ShowIf(nameof(IsTowerScaledDamageAction))]
    [MinValue(0.01f)]
    [SerializeField] private float damageScale;
    [TitleGroup("Effect Action")]
    [ShowIf(nameof(IsFixedBuffDamageAction))]
    [MinValue(1)]
    [SerializeField] private int fixedDamage;
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
    [Required]
    [SerializeField] private GameObject windVortexPrefab;

    public EffectActionType ActionType => actionType;
    public EffectDamageMode DamageMode => damageMode;
    public float DamageScale => damageScale;
    public int FixedDamage => fixedDamage;
    public BuffDefinition BuffDefinition => buffDefinition;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public bool IsMovementLocked => isMovementLocked;
    public EffectDefinition MultiTargetEffectDefinition => multiTargetEffectDefinition;
    public int TargetCount => Mathf.Max(1, targetCount);
    public int AuthoredTargetCount => targetCount;
    public bool ExcludeTriggerContextTarget => excludeTriggerTarget;
    public GameObject WindVortexPrefab => windVortexPrefab;

    public bool IsValid()
    {
        if (!IsDamageAuthoringValid())
        {
            return false;
        }

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

        }

        if (actionType == EffectActionType.SpawnWindVortex)
        {
            if (windVortexPrefab == null)
            {
                Debug.LogWarning("Effect action is invalid: SpawnWindVortex requires a runtime prefab.");
                return false;
            }

            if (!windVortexPrefab.TryGetComponent(out WindVortexBehaviour windVortexBehaviour))
            {
                Debug.LogWarning("Effect action is invalid: SpawnWindVortex requires WindVortexBehaviour on the runtime prefab root.", windVortexPrefab);
                return false;
            }

            if (!windVortexBehaviour.IsValid())
            {
                return false;
            }
        }

        return true;
    }

    private bool IsDamageAuthoringValid()
    {
        if (actionType != EffectActionType.DealDamage)
        {
            if (damageMode != EffectDamageMode.None ||
                damageScale != 0f ||
                fixedDamage != 0)
            {
                Debug.LogWarning(
                    "Effect action is invalid: non-damage actions cannot author Damage Mode, Damage Scale, or Fixed Damage.");
                return false;
            }

            return true;
        }

        switch (damageMode)
        {
            case EffectDamageMode.TowerScaled:
                if (float.IsNaN(damageScale) ||
                    float.IsInfinity(damageScale) ||
                    damageScale <= 0f ||
                    fixedDamage != 0)
                {
                    Debug.LogWarning(
                        "Effect action is invalid: TowerScaled damage requires a positive finite Damage Scale and no Fixed Damage.");
                    return false;
                }

                return true;
            case EffectDamageMode.FixedBuff:
                if (fixedDamage <= 0 || damageScale != 0f)
                {
                    Debug.LogWarning(
                        "Effect action is invalid: FixedBuff damage requires positive Fixed Damage and no Damage Scale.");
                    return false;
                }

                return true;
            case EffectDamageMode.None:
            default:
                Debug.LogWarning(
                    "Effect action is invalid: DealDamage requires TowerScaled or FixedBuff Damage Mode.");
                return false;
        }
    }

    private bool IsApplyBuffAction()
    {
        return actionType == EffectActionType.ApplyBuff;
    }

    private bool IsDealDamageAction()
    {
        return actionType == EffectActionType.DealDamage;
    }

    private bool IsTowerScaledDamageAction()
    {
        return IsDealDamageAction() &&
               damageMode == EffectDamageMode.TowerScaled;
    }

    private bool IsFixedBuffDamageAction()
    {
        return IsDealDamageAction() &&
               damageMode == EffectDamageMode.FixedBuff;
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
