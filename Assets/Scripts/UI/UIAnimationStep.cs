using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class UIAnimationStep
{
    [SerializeField] private bool enabled = true;
    [SerializeField] private UIAnimationType animationType;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float delay;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [ShowIf(nameof(IsPositionStep))]
    [LabelText("Start Anchored Position")]
    [SerializeField] private Vector2 startAnchoredPosition = new Vector2(0f, 80f);
    [ShowIf(nameof(IsPositionStep))]
    [LabelText("Target Anchored Position")]
    [SerializeField] private Vector2 targetAnchoredPosition = new Vector2(0f, 160f);

    [ShowIf(nameof(IsScaleStep))]
    [SerializeField] private Vector3 startScale = Vector3.one;
    [ShowIf(nameof(IsScaleStep))]
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [ShowIf(nameof(IsFadeStep))]
    [SerializeField] private float startAlpha = 1f;
    [ShowIf(nameof(IsFadeStep))]
    [SerializeField] private float targetAlpha;

    internal UIAnimationStep Snapshot() => (UIAnimationStep)MemberwiseClone();

    public bool Enabled => enabled;
    public UIAnimationType AnimationType => animationType;
    public float Duration => duration;
    public float Delay => delay;
    public Ease EaseType => easeType;
    public Vector2 StartAnchoredPosition => startAnchoredPosition;
    public Vector2 TargetAnchoredPosition => targetAnchoredPosition;
    public Vector3 StartScale => startScale;
    public Vector3 TargetScale => targetScale;
    public float StartAlpha => startAlpha;
    public float TargetAlpha => targetAlpha;

    private bool IsPositionStep => animationType == UIAnimationType.Position;
    private bool IsScaleStep => animationType == UIAnimationType.Scale;
    private bool IsFadeStep => animationType == UIAnimationType.Fade;

    public static UIAnimationStep CreatePositionStep(
        Vector2 startAnchoredPosition,
        Vector2 targetAnchoredPosition,
        float duration,
        Ease easeType)
    {
        return new UIAnimationStep
        {
            animationType = UIAnimationType.Position,
            startAnchoredPosition = startAnchoredPosition,
            targetAnchoredPosition = targetAnchoredPosition,
            duration = duration,
            easeType = easeType
        };
    }

    public static UIAnimationStep CreateScaleStep(
        Vector3 startScale,
        Vector3 targetScale,
        float duration,
        float delay,
        Ease easeType)
    {
        return new UIAnimationStep
        {
            animationType = UIAnimationType.Scale,
            startScale = startScale,
            targetScale = targetScale,
            duration = duration,
            delay = delay,
            easeType = easeType
        };
    }

    public static UIAnimationStep CreateFadeStep(
        float startAlpha,
        float targetAlpha,
        float duration,
        float delay,
        Ease easeType)
    {
        return new UIAnimationStep
        {
            animationType = UIAnimationType.Fade,
            startAlpha = startAlpha,
            targetAlpha = targetAlpha,
            duration = duration,
            delay = delay,
            easeType = easeType
        };
    }
}

public enum UIAnimationType
{
    Position = 0,
    Scale = 1,
    Fade = 2
}
