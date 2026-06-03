using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EffectConfig",
    menuName = "Tower Nexus/Effect Config"
)]
public class EffectConfig : ScriptableObject
{
    [TitleGroup("Identity")]
    [Required]
    [SerializeField] private string effectId;

    [TitleGroup("Area Damage")]
    [MinValue(0f)]
    [SerializeField] private float radius = 1f;

    public string EffectId => effectId;
    public float Radius => radius;

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(effectId))
        {
            Debug.LogWarning("Effect config is invalid: effect id is missing.", this);
            return false;
        }

        if (radius < 0f)
        {
            Debug.LogWarning($"Effect config '{effectId}' is invalid: radius cannot be negative.", this);
            return false;
        }

        return true;
    }
}
