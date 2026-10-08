using System;
using UnityEngine;

// Explicitly attached to DraftUI's authored root, including when it is a child.
public sealed class DraftPresentationLifetime : MonoBehaviour
{
    private Action cancelled;
    internal void Arm(Action callback) { cancelled = callback; }
    internal void Disarm() { cancelled = null; }
    private void OnDisable()
    {
        Action callback = cancelled;
        cancelled = null;
        callback?.Invoke();
    }
}
