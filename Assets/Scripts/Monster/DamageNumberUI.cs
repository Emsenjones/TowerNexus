using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class DamageNumberUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform animatedRoot;
    [SerializeField] private int previewDamageValue = 999;
    // Small random offset to reduce overlap when multiple damage numbers appear simultaneously.
    [SerializeField] private Vector2 randomOffsetRangeX = new Vector2(-1f, 1f);
    [SerializeField] private Vector2 randomOffsetRangeY = new Vector2(-1f, 1f);
    [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true)]
    [SerializeField] private List<DamageNumberAnimationStep> animationSteps = new List<DamageNumberAnimationStep>
    {
        DamageNumberAnimationStep.CreatePositionStep(new Vector2(0f, 80f), new Vector2(0f, 80f), 0.6f, Ease.OutQuad),
        DamageNumberAnimationStep.CreateScaleStep(new Vector3(0.8f, 0.8f, 0.8f), new Vector3(1.2f, 1.2f, 1.2f), 0.12f, 0f, Ease.OutBack),
        DamageNumberAnimationStep.CreateScaleStep(new Vector3(1.2f, 1.2f, 1.2f), Vector3.one, 0.12f, 0.12f, Ease.OutQuad),
        DamageNumberAnimationStep.CreateFadeStep(1f, 0f, 0.4f, 0.2f, Ease.InQuad)
    };

    private RectTransform rectTransform;
    private Sequence activeSequence;
    private Action<DamageNumberUI> onAnimationComplete;

    public void Play(int damage, Action<DamageNumberUI> onComplete = null)
    {
        onAnimationComplete = onComplete;
        ApplyRandomSpawnOffset();
        PrepareForPlayback(damage);
        activeSequence = BuildSequence(true);
    }

    [Button("Preview")]
    [ContextMenu("Preview")]
    public void Preview()
    {
        PrepareForPlayback(previewDamageValue);
        activeSequence = BuildSequence(false);
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnDestroy()
    {
        KillActiveSequence();
    }

    private void CacheReferences()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (animatedRoot == null)
        {
            animatedRoot = rectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void ApplyRandomSpawnOffset()
    {
        CacheReferences();

        if (rectTransform == null)
        {
            return;
        }

        float randomX = UnityEngine.Random.Range(randomOffsetRangeX.x, randomOffsetRangeX.y);
        float randomY = UnityEngine.Random.Range(randomOffsetRangeY.x, randomOffsetRangeY.y);

        rectTransform.anchoredPosition += new Vector2(randomX, randomY);
    }

    private void PrepareForPlayback(int damage)
    {
        CacheReferences();
        KillActiveSequence();

        if (damageText == null)
        {
            Debug.LogWarning("Damage number UI cannot display damage: damage text is not assigned.", this);
        }
        else
        {
            damageText.text = damage.ToString();
        }

        if (animatedRoot == null)
        {
            Debug.LogWarning("Damage number UI cannot animate: animated root is not assigned.", this);
            return;
        }

        ApplyStartValues();
    }

    private void ApplyStartValues()
    {
        if (animatedRoot == null)
        {
            return;
        }

        for (int i = 0; i < animationSteps.Count; i++)
        {
            DamageNumberAnimationStep step = animationSteps[i];

            if (step == null || !step.Enabled)
            {
                continue;
            }

            switch (step.AnimationType)
            {
                case DamageNumberAnimationType.Position:
                    animatedRoot.anchoredPosition = step.StartAnchoredPosition;
                    break;
                case DamageNumberAnimationType.Scale:
                    animatedRoot.localScale = step.StartScale;
                    break;
                case DamageNumberAnimationType.Fade:
                    if (canvasGroup == null)
                    {
                        Debug.LogWarning("Damage number UI cannot apply fade start value: CanvasGroup is not assigned.", this);
                        break;
                    }

                    canvasGroup.alpha = step.StartAlpha;
                    break;
            }
        }
    }

    private Sequence BuildSequence(bool destroyWhenComplete)
    {
        if (animatedRoot == null)
        {
            CompletePlayback(destroyWhenComplete);
            return null;
        }

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(this);
        bool hasTween = false;

        for (int i = 0; i < animationSteps.Count; i++)
        {
            DamageNumberAnimationStep step = animationSteps[i];

            if (step == null || !step.Enabled)
            {
                continue;
            }

            Tween tween = CreateTween(step);

            if (tween == null)
            {
                continue;
            }

            sequence.Insert(step.Delay, tween);
            hasTween = true;
        }

        if (!hasTween)
        {
            sequence.Kill();
            CompletePlayback(destroyWhenComplete);
            return null;
        }

        sequence.OnComplete(() => CompletePlayback(destroyWhenComplete));
        return sequence;
    }

    private Tween CreateTween(DamageNumberAnimationStep step)
    {
        switch (step.AnimationType)
        {
            case DamageNumberAnimationType.Position:
                return animatedRoot.DOAnchorPos(step.TargetAnchoredPosition, step.Duration)
                    .SetEase(step.EaseType);
            case DamageNumberAnimationType.Scale:
                return animatedRoot.DOScale(step.TargetScale, step.Duration)
                    .SetEase(step.EaseType);
            case DamageNumberAnimationType.Fade:
                if (canvasGroup == null)
                {
                    Debug.LogWarning("Damage number UI cannot play fade tween: CanvasGroup is not assigned.", this);
                    return null;
                }

                return canvasGroup.DOFade(step.TargetAlpha, step.Duration)
                    .SetEase(step.EaseType);
            default:
                return null;
        }
    }

    private void CompletePlayback(bool destroyWhenComplete)
    {
        activeSequence = null;

        if (destroyWhenComplete)
        {
            onAnimationComplete?.Invoke(this);
        }
    }

    private void KillActiveSequence()
    {
        if (activeSequence == null)
        {
            return;
        }

        activeSequence.Kill();
        activeSequence = null;
    }
}
