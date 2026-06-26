using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EffectConfig",
    menuName = "Tower Nexus/Effect Config"
)]
public class EffectConfig : ScriptableObject
{
    [TitleGroup("Area Damage")]
    [MinValue(0f)]
    [SerializeField] private float radius = 1f;

    public float Radius => radius;

    public bool IsValid()
    {
        if (radius < 0f)
        {
            Debug.LogWarning($"Effect config '{name}' is invalid: radius cannot be negative.", this);
            return false;
        }

        return true;
    }
}
