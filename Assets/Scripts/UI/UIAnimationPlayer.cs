using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum UIAnimationTimeMode { Unscaled = 0, Scaled = 1 }

// One sampled timeline avoids tween callback ordering at shared step boundaries.
// DOTween supplies easing; the host supplies updates and cancellation.
[Serializable]
public sealed class UIAnimationPlayer
{
    [SerializeField] private RectTransform animatedRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UIAnimationTimeMode timeMode;
    [SerializeField] private List<UIAnimationStep> animationSteps = new List<UIAnimationStep>();

    private List<UIAnimationStep> playingSteps;
    private RectTransform playingRoot;
    private CanvasGroup playingOpacity;
    private UIAnimationTimeMode playingTimeMode;
    private Action completion;
    private float elapsed;
    private float duration;
    private int startFrame;

    public UIAnimationPlayer() { }
    public UIAnimationPlayer(UIAnimationTimeMode mode) { timeMode = mode; }

    public bool IsPlaying => playingSteps != null;
    public UIAnimationTimeMode TimeMode => timeMode;

    public bool TryValidate(out string failureReason)
    {
        return TryPrepare(out _, out _, out failureReason);
    }

    public bool TryPlay(Action onComplete, out string failureReason)
    {
        Cancel();
        if (!TryPrepare(out List<UIAnimationStep> prepared, out float end, out failureReason))
            return false;

        playingSteps = prepared;
        playingRoot = animatedRoot;
        playingOpacity = canvasGroup;
        playingTimeMode = timeMode;
        duration = end;
        elapsed = 0f;
        startFrame = Time.frameCount;
        completion = onComplete;
        // Only the earliest start per channel is visible before the first update,
        // including a zero-duration channel.
        UIAnimationType? previous = null;
        foreach (UIAnimationStep step in playingSteps)
        {
            if (previous != step.AnimationType) Apply(step, 0f);
            previous = step.AnimationType;
        }
        return true;
    }

    public void Cancel()
    {
        playingSteps = null;
        playingRoot = null;
        playingOpacity = null;
        completion = null;
    }

    // Called once per runtime frame or editor preview update by the owning UI.
    public void Tick(float scaledDeltaTime, float unscaledDeltaTime)
    {
        if (!IsPlaying || (Application.isPlaying && Time.frameCount == startFrame)) return;
        Advance(playingTimeMode == UIAnimationTimeMode.Unscaled
            ? unscaledDeltaTime : scaledDeltaTime);
    }

    private void Advance(float delta)
    {
        if (!IsPlaying || !Finite(delta) || delta <= 0f) return;
        elapsed = (float)Math.Min((double)duration, (double)elapsed + delta);
        // The last started step for each channel wins. Sampling its explicit
        // start/target also handles a frame that crosses multiple boundaries.
        for (int i = 0; i < playingSteps.Count; i++)
        {
            UIAnimationStep step = playingSteps[i];
            if (i + 1 < playingSteps.Count &&
                playingSteps[i + 1].AnimationType == step.AnimationType &&
                playingSteps[i + 1].Delay <= elapsed) continue;

            if ((step.AnimationType == UIAnimationType.Fade && playingOpacity == null) ||
                (step.AnimationType != UIAnimationType.Fade && playingRoot == null))
            {
                Cancel();
                return;
            }
            float progress = elapsed < step.Delay ? 0f : step.Duration == 0f
                ? 1f : Mathf.Clamp01((elapsed - step.Delay) / step.Duration);
            Apply(step, progress);
            // Skip future steps of the channel: its last value holds across gaps.
            while (i + 1 < playingSteps.Count &&
                   playingSteps[i + 1].AnimationType == step.AnimationType) i++;
        }
        if (elapsed < duration) return;
        Action finished = completion;
        Cancel(); // Clear before external code can replay, cancel, or destroy us.
        finished?.Invoke();
    }

    private void Apply(UIAnimationStep step, float progress)
    {
        float t = progress <= 0f ? 0f : progress >= 1f ? 1f
            : DOVirtual.EasedValue(0f, 1f, progress, step.EaseType);
        switch (step.AnimationType)
        {
            case UIAnimationType.Position:
                playingRoot.anchoredPosition = Vector2.LerpUnclamped(
                    step.StartAnchoredPosition, step.TargetAnchoredPosition, t);
                break;
            case UIAnimationType.Scale:
                playingRoot.localScale = Vector3.LerpUnclamped(step.StartScale, step.TargetScale, t);
                break;
            case UIAnimationType.Fade:
                playingOpacity.alpha = Mathf.LerpUnclamped(step.StartAlpha, step.TargetAlpha, t);
                break;
        }
    }

    private bool TryPrepare(out List<UIAnimationStep> steps, out float end, out string reason)
    {
        steps = new List<UIAnimationStep>();
        end = 0f;
        reason = string.Empty;
        if (!Enum.IsDefined(typeof(UIAnimationTimeMode), timeMode))
        {
            reason = "Unknown UI animation time mode.";
            return false;
        }
        if (animationSteps != null)
        for (int i = 0; i < animationSteps.Count; i++)
        {
            UIAnimationStep step = animationSteps[i];
            if (step == null)
            {
                reason = $"Animation step {i} is missing.";
                return false;
            }
            if (!step.Enabled) continue;
            if (!ValidateStep(step, out reason))
            {
                reason = $"Animation step {i}: {reason}";
                return false;
            }
            steps.Add(step.Snapshot());
            end = Mathf.Max(end, step.Delay + step.Duration);
        }
        if (steps.Count == 0)
        {
            reason = "UI animation requires at least one enabled step.";
            return false;
        }
        steps.Sort((a, b) => a.AnimationType != b.AnimationType
            ? a.AnimationType.CompareTo(b.AnimationType) : a.Delay.CompareTo(b.Delay));
        for (int i = 1; i < steps.Count; i++)
        {
            UIAnimationStep a = steps[i - 1], b = steps[i];
            if (a.AnimationType == b.AnimationType &&
                (a.Delay == b.Delay || a.Delay + a.Duration > b.Delay))
            {
                reason = $"Conflicting {a.AnimationType} steps at {a.Delay} and {b.Delay}.";
                return false;
            }
        }
        return true;
    }

    private bool ValidateStep(UIAnimationStep s, out string reason)
    {
        reason = string.Empty;
        if (!Finite(s.Duration) || !Finite(s.Delay) || s.Duration < 0f || s.Delay < 0f ||
            !Finite(s.Delay + s.Duration)) reason = "Duration, delay, and their sum must be finite and non-negative.";
        else if (!Enum.IsDefined(typeof(Ease), s.EaseType) || s.EaseType == Ease.Unset ||
                 s.EaseType.ToString().StartsWith("INTERNAL_")) reason = "Unsupported easing value.";
        else switch (s.AnimationType)
        {
            case UIAnimationType.Position:
                if (animatedRoot == null || !Finite(s.StartAnchoredPosition) || !Finite(s.TargetAnchoredPosition))
                    reason = "Position requires a target and finite start/end values.";
                break;
            case UIAnimationType.Scale:
                if (animatedRoot == null || !Finite(s.StartScale) || !Finite(s.TargetScale))
                    reason = "Scale requires a target and finite start/end values.";
                break;
            case UIAnimationType.Fade:
                if (canvasGroup == null || !Finite(s.StartAlpha) || !Finite(s.TargetAlpha) ||
                    s.StartAlpha < 0f || s.StartAlpha > 1f || s.TargetAlpha < 0f || s.TargetAlpha > 1f)
                    reason = "Fade requires an opacity target and start/end Alpha within [0, 1].";
                break;
            default: reason = "Unknown animation type."; break;
        }
        return reason.Length == 0;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
    private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
}
