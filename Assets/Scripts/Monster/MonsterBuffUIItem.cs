using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterBuffUIItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stackCountText;
    [Header("Protection Pulse")]
    [Min(0.01f)]
    [SerializeField] private float protectionPulseDuration = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float protectionPulseMaxAlpha = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float protectionPulseMinAlpha;
    [SerializeField] private Ease protectionPulseEaseType = Ease.InOutSine;

    private Tween protectionPulseTween;

    private void OnDisable()
    {
        StopProtectionPulse();
    }

    public void Refresh(MonsterBuffStateSnapshot snapshot)
    {
        if (snapshot.Definition == null)
        {
            return;
        }

        bool isProtection = snapshot.Phase == BuffRuntimePhase.Protection;
        Sprite icon = snapshot.Definition.StatusIcon;

        StopProtectionPulse();

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            SetIconAlpha(icon != null
                ? (isProtection ? GetProtectionPulseMaxAlpha() : 1f)
                : 0f);
        }

        bool showStackCount = !isProtection && snapshot.StackCount > 1;

        if (stackCountText != null)
        {
            stackCountText.gameObject.SetActive(showStackCount);

            if (showStackCount)
            {
                stackCountText.text = snapshot.StackCount.ToString();
            }
        }

        if (isProtection && icon != null)
        {
            StartProtectionPulse();
        }
    }

    private void StartProtectionPulse()
    {
        if (iconImage == null)
        {
            return;
        }

        float minAlpha = GetProtectionPulseMinAlpha();
        float maxAlpha = GetProtectionPulseMaxAlpha();

        if (Mathf.Approximately(minAlpha, maxAlpha))
        {
            return;
        }

        protectionPulseTween = iconImage
            .DOFade(minAlpha, GetProtectionPulseDuration() * 0.5f)
            .SetEase(protectionPulseEaseType)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopProtectionPulse()
    {
        if (protectionPulseTween == null)
        {
            return;
        }

        protectionPulseTween.Kill();
        protectionPulseTween = null;
    }

    private void SetIconAlpha(float alpha)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private float GetProtectionPulseDuration()
    {
        return Mathf.Max(0.01f, protectionPulseDuration);
    }

    private float GetProtectionPulseMinAlpha()
    {
        return Mathf.Min(
            Mathf.Clamp01(protectionPulseMinAlpha),
            Mathf.Clamp01(protectionPulseMaxAlpha));
    }

    private float GetProtectionPulseMaxAlpha()
    {
        return Mathf.Max(
            Mathf.Clamp01(protectionPulseMinAlpha),
            Mathf.Clamp01(protectionPulseMaxAlpha));
    }
}
