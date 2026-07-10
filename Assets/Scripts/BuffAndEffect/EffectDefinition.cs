using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

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

    [TitleGroup("Visual Feedback")]
    [SerializeField] private GameObject oneShotEffectVfxPrefab;

    [TitleGroup("Actions")]
    [SerializeField] private List<EffectAction> actions = new List<EffectAction> { new EffectAction() };

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public float Radius => radius;
    public GameObject OneShotEffectVfxPrefab => oneShotEffectVfxPrefab;
    public IReadOnlyList<EffectAction> Actions => actions;

    public bool IsValid()
    {
        bool isValid = true;

        if (radius < 0f)
        {
            Debug.LogWarning($"Effect definition '{name}' is invalid: radius cannot be negative.", this);
            isValid = false;
        }

        if (actions == null || actions.Count == 0)
        {
            Debug.LogWarning($"Effect definition '{name}' is invalid: at least one action is required.", this);
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
        }

        return isValid;
    }
}
