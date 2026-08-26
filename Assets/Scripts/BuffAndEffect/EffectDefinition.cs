using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "EffectDefinition",
    menuName = "Tower Nexus/Effect Definition"
)]
public class EffectDefinition : ScriptableObject
{
    [TitleGroup("Debug")]
    [SerializeField] private string displayName;

    [TitleGroup("Targeting")]
    [MinValue(0f)]
    [SerializeField] private float radius;

    [TitleGroup("Execution Feedback")]
    [SerializeField] private GameObject executionVfxPrefab;

    [TitleGroup("Actions")]
    [SerializeField] private List<EffectAction> actions = new List<EffectAction> { new EffectAction() };

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public float Radius => radius;
    public GameObject ExecutionVfxPrefab => executionVfxPrefab;
    public IReadOnlyList<EffectAction> Actions => actions;

    public bool IsValid()
    {
        return ValidateRecursive(
            requiredDamageMode: null,
            forbidDamage: false,
            new HashSet<EffectDefinition>());
    }

    public bool IsValidForDamageMode(EffectDamageMode requiredDamageMode)
    {
        if (requiredDamageMode != EffectDamageMode.TowerScaled &&
            requiredDamageMode != EffectDamageMode.FixedBuff)
        {
            Debug.LogWarning(
                $"Effect definition '{name}' cannot validate unsupported required Damage Mode '{requiredDamageMode}'.",
                this);
            return false;
        }

        return ValidateRecursive(
            requiredDamageMode,
            forbidDamage: false,
            new HashSet<EffectDefinition>());
    }

    public bool IsValidWithoutDamage()
    {
        return ValidateRecursive(
            requiredDamageMode: null,
            forbidDamage: true,
            new HashSet<EffectDefinition>());
    }

    public bool ContainsDealDamageAction()
    {
        return ContainsDealDamageAction(
            new HashSet<EffectDefinition>());
    }

    public bool IsValidElementalHitReaction(ElementType elementType)
    {
        if (!IsValidForDamageMode(EffectDamageMode.FixedBuff))
        {
            return false;
        }

        switch (elementType)
        {
            case ElementType.Electric:
                return IsApprovedElectricHitReaction();
            case ElementType.Wind:
                return IsApprovedWindHitReaction();
            default:
                return false;
        }
    }

    public bool TryGetElementalHitReactionFixedDamage(
        ElementType elementType,
        out int fixedDamage)
    {
        return TryGetElementalHitReactionDamageSignature(
            elementType,
            out _,
            out _,
            out fixedDamage);
    }

    public bool TryGetElementalHitReactionDamageSignature(
        ElementType elementType,
        out EffectDefinition damageEffectDefinition,
        out int actionOrdinal,
        out int fixedDamage)
    {
        damageEffectDefinition = null;
        actionOrdinal = -1;
        fixedDamage = 0;

        if (!IsValidElementalHitReaction(elementType))
        {
            return false;
        }

        damageEffectDefinition = elementType == ElementType.Electric
            ? this
            : actions[0].MultiTargetEffectDefinition;
        EffectAction damageAction = damageEffectDefinition.Actions[0];
        actionOrdinal = 0;
        fixedDamage = damageAction.FixedDamage;
        return fixedDamage > 0;
    }

    private bool IsApprovedElectricHitReaction()
    {
        return radius <= 0f &&
               actions != null &&
               actions.Count == 1 &&
               actions[0] != null &&
               actions[0].ActionType == EffectActionType.DealDamage &&
               actions[0].DamageMode == EffectDamageMode.FixedBuff;
    }

    private bool IsApprovedWindHitReaction()
    {
        if (radius <= 0f ||
            actions == null ||
            actions.Count != 1 ||
            actions[0] == null ||
            actions[0].ActionType != EffectActionType.ExecuteMultiTargetEffect ||
            actions[0].AuthoredTargetCount != 1 ||
            !actions[0].ExcludeTriggerContextTarget)
        {
            return false;
        }

        EffectDefinition child = actions[0].MultiTargetEffectDefinition;

        return child != null &&
               child.Radius <= 0f &&
               child.Actions != null &&
               child.Actions.Count == 1 &&
               child.Actions[0] != null &&
               child.Actions[0].ActionType == EffectActionType.DealDamage &&
               child.Actions[0].DamageMode == EffectDamageMode.FixedBuff;
    }

    private bool ContainsDealDamageAction(
        HashSet<EffectDefinition> visitedDefinitions)
    {
        if (!visitedDefinitions.Add(this) || actions == null)
        {
            return false;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            EffectAction action = actions[i];

            if (action == null)
            {
                continue;
            }

            if (action.ActionType == EffectActionType.DealDamage)
            {
                return true;
            }

            if (action.ActionType == EffectActionType.ExecuteMultiTargetEffect &&
                action.MultiTargetEffectDefinition != null &&
                action.MultiTargetEffectDefinition.ContainsDealDamageAction(
                    visitedDefinitions))
            {
                return true;
            }
        }

        return false;
    }

    private bool ValidateRecursive(
        EffectDamageMode? requiredDamageMode,
        bool forbidDamage,
        HashSet<EffectDefinition> validationStack)
    {
        if (!validationStack.Add(this))
        {
            Debug.LogWarning(
                $"Effect definition '{name}' is invalid: recursive Effect definitions contain a cycle.",
                this);
            return false;
        }

        bool isValid = true;

        if (radius < 0f)
        {
            Debug.LogWarning($"Effect definition '{name}' is invalid: radius cannot be negative.", this);
            isValid = false;
        }

        if (actions == null || actions.Count == 0)
        {
            Debug.LogWarning($"Effect definition '{name}' is invalid: at least one action is required.", this);
            validationStack.Remove(this);
            return false;
        }

        for (int i = 0; i < actions.Count; i++)
        {
            EffectAction action = actions[i];

            if (action == null)
            {
                Debug.LogWarning($"Effect definition '{name}' is invalid: action at index {i} is missing.", this);
                isValid = false;
                continue;
            }

            if (!action.IsValid())
            {
                Debug.LogWarning($"Effect definition '{name}' is invalid: action at index {i} is invalid.", this);
                isValid = false;
            }

            if (action.ActionType == EffectActionType.DealDamage)
            {
                if (forbidDamage)
                {
                    Debug.LogWarning(
                        $"Effect definition '{name}' is invalid: this owner permits no DealDamage action.",
                        this);
                    isValid = false;
                }
                else if (requiredDamageMode.HasValue &&
                         action.DamageMode != requiredDamageMode.Value)
                {
                    Debug.LogWarning(
                        $"Effect definition '{name}' is invalid: action at index {i} uses '{action.DamageMode}', but its owner requires '{requiredDamageMode.Value}'.",
                        this);
                    isValid = false;
                }
            }

            if (action.ActionType == EffectActionType.ExecuteMultiTargetEffect &&
                action.MultiTargetEffectDefinition != null &&
                !action.MultiTargetEffectDefinition.ValidateRecursive(
                    requiredDamageMode,
                    forbidDamage,
                    validationStack))
            {
                Debug.LogWarning(
                    $"Effect definition '{name}' is invalid: nested Effect at action index {i} failed recursive validation.",
                    this);
                isValid = false;
            }
        }

        validationStack.Remove(this);
        return isValid;
    }
}
