using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class AnimationEventController : MonoBehaviour
{
    [InfoBox("Cannot add multiple AnimationEventController components in the same gameObject.")]
    public event Action OnTriggerEvent01;
    void OnTriggerAnimationEvent01()
    {
        OnTriggerEvent01?.Invoke();
    }
    void OnDisable()
    {
        OnTriggerEvent01 = null;
    }
}
