using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class DamageNumberAnimationStep
{
    [SerializeField] private bool enabled = true;
    [SerializeField] private DamageNumberAnimationType animationType;
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

    public bool Enabled => enabled;
    public DamageNumberAnimationType AnimationType => animationType;
    public float Duration => Mathf.Max(0f, duration);
    public float Delay => Mathf.Max(0f, delay);
    public Ease EaseType => easeType;
    public Vector2 StartAnchoredPosition => startAnchoredPosition;
    public Vector2 TargetAnchoredPosition => targetAnchoredPosition;
    public Vector3 StartScale => startScale;
    public Vector3 TargetScale => targetScale;
    public float StartAlpha => Mathf.Clamp01(startAlpha);
    public float TargetAlpha => Mathf.Clamp01(targetAlpha);

    private bool IsPositionStep => animationType == DamageNumberAnimationType.Position;
    private bool IsScaleStep => animationType == DamageNumberAnimationType.Scale;
    private bool IsFadeStep => animationType == DamageNumberAnimationType.Fade;

    public static DamageNumberAnimationStep CreatePositionStep(
        Vector2 startAnchoredPosition,
        Vector2 targetAnchoredPosition,
        float duration,
        Ease easeType)
    {
        return new DamageNumberAnimationStep
        {
            animationType = DamageNumberAnimationType.Position,
            startAnchoredPosition = startAnchoredPosition,
            targetAnchoredPosition = targetAnchoredPosition,
            duration = duration,
            easeType = easeType
        };
    }

    public static DamageNumberAnimationStep CreateScaleStep(
        Vector3 startScale,
        Vector3 targetScale,
        float duration,
        float delay,
        Ease easeType)
    {
        return new DamageNumberAnimationStep
        {
            animationType = DamageNumberAnimationType.Scale,
            startScale = startScale,
            targetScale = targetScale,
            duration = duration,
            delay = delay,
            easeType = easeType
        };
    }

    public static DamageNumberAnimationStep CreateFadeStep(
        float startAlpha,
        float targetAlpha,
        float duration,
        float delay,
        Ease easeType)
    {
        return new DamageNumberAnimationStep
        {
            animationType = DamageNumberAnimationType.Fade,
            startAlpha = startAlpha,
            targetAlpha = targetAlpha,
            duration = duration,
            delay = delay,
            easeType = easeType
        };
    }
}

public enum DamageNumberAnimationType
{
    Position,
    Scale,
    Fade
}
