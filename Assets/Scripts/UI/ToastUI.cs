using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ToastUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private UIAnimationPlayer animationPlayer = new UIAnimationPlayer();
    [SerializeField] private string previewMessage = "No other draft choices available.";

    public bool TryPlay(string message, Action<ToastUI> onComplete, out string failureReason)
    {
        Cancel();
        if (!isActiveAndEnabled || messageText == null || animationPlayer == null ||
            animationPlayer.TimeMode != UIAnimationTimeMode.Unscaled)
        {
            failureReason = "Toast requires active presentation, text, and an unscaled animation player.";
            return false;
        }
        // Include backgrounds added by UI authoring. Lifetime belongs to caller.
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        foreach (CanvasGroup group in GetComponentsInChildren<CanvasGroup>(true))
        {
            group.blocksRaycasts = false;
            group.interactable = false;
        }
        messageText.text = message ?? string.Empty;
        bool started = animationPlayer.TryPlay(() => onComplete?.Invoke(this), out failureReason);
        if (started) BeginEditorPreview();
        return started;
    }

    public void Cancel()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
        animationPlayer?.Cancel();
    }

    private void Update()
    {
        if (Application.isPlaying) animationPlayer?.Tick(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void BeginEditorPreview()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        editorTime = UnityEditor.EditorApplication.timeSinceStartup;
        UnityEditor.EditorApplication.update -= EditorUpdate;
        UnityEditor.EditorApplication.update += EditorUpdate;
#endif
    }

#if UNITY_EDITOR
    private double editorTime;
    private void EditorUpdate()
    {
        if (this == null || !isActiveAndEnabled || Application.isPlaying)
        {
            Cancel();
            return;
        }
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        float delta = (float)(now - editorTime);
        editorTime = now;
        animationPlayer?.Tick(delta * Time.timeScale, delta);
        if (animationPlayer == null || !animationPlayer.IsPlaying)
            UnityEditor.EditorApplication.update -= EditorUpdate;
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }
#endif
    private void OnDisable() => Cancel();
    private void OnDestroy() => Cancel();

    [Button("Preview")]
    [ContextMenu("Preview")]
    public void Preview()
    {
        if (!TryPlay(previewMessage, null, out string reason)) Debug.LogWarning(reason, this);
    }
}
