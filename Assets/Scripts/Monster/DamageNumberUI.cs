using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class DamageNumberUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private UIAnimationPlayer animationPlayer = new UIAnimationPlayer(UIAnimationTimeMode.Scaled);
    [SerializeField] private int previewDamageValue = 999;
    [SerializeField] private Vector2 randomOffsetRangeX = new Vector2(-1f, 1f);
    [SerializeField] private Vector2 randomOffsetRangeY = new Vector2(-1f, 1f);

    // Cancellation is not completion. The instance owner still needs to remove
    // its registration if this component/object disappears early.
    public event Action<DamageNumberUI> Unavailable;

    public bool TryPlay(int damage, Action<DamageNumberUI> onComplete, out string failureReason)
    {
        Cancel();
        if (!isActiveAndEnabled || damageText == null || animationPlayer == null ||
            animationPlayer.TimeMode != UIAnimationTimeMode.Scaled || !(transform is RectTransform root))
        {
            failureReason = "Damage number requires active presentation, text, RectTransform, and a scaled animation player.";
            return false;
        }
        if (!animationPlayer.TryValidate(out failureReason)) return false;
        damageText.text = damage.ToString();
        // The manager establishes the spawn location before each runtime play.
        // Shared animation touches the child, never this world-positioned root.
        root.anchoredPosition += new Vector2(
            UnityEngine.Random.Range(randomOffsetRangeX.x, randomOffsetRangeX.y),
            UnityEngine.Random.Range(randomOffsetRangeY.x, randomOffsetRangeY.y));
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

    [Button("Preview")]
    [ContextMenu("Preview")]
    public void Preview()
    {
        Cancel();
        if (!isActiveAndEnabled || damageText == null || animationPlayer == null)
        {
            Debug.LogWarning("Damage number preview requires text and animation player.", this);
            return;
        }
        damageText.text = previewDamageValue.ToString();
        if (animationPlayer.TryPlay(null, out string reason)) BeginEditorPreview();
        else Debug.LogWarning(reason, this);
    }

    private void OnDisable() => NotifyUnavailable();
    private void OnDestroy() => NotifyUnavailable();

    private void NotifyUnavailable()
    {
        Cancel();
        Action<DamageNumberUI> unavailable = Unavailable;
        Unavailable = null;
        unavailable?.Invoke(this);
    }
}
