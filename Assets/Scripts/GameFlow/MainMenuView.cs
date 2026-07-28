using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startMessageText;
    [Header("Start Message Pulse")]
    [Min(0.01f)]
    [SerializeField] private float pulseHalfDuration = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float pulseMinimumAlpha = 0.35f;
    [SerializeField] private Ease pulseEaseType = Ease.InOutSine;

    private Tween pulseTween;
    private float authoredMessageAlpha = 1f;
    private bool hasCapturedAuthoredAlpha;
    private bool isInteractionEnabled;

    public event Action StartRequested;

    public bool IsInteractionEnabled => isInteractionEnabled;

    private void Awake()
    {
        CaptureAuthoredMessageAlpha();
        SetInteractionEnabled(false);
    }

    private void OnEnable()
    {
        CaptureAuthoredMessageAlpha();
        BindButton();
        StartPulse();
    }

    private void OnDisable()
    {
        UnbindButton();
        SetInteractionEnabled(false);
        StopPulseAndRestoreAlpha();
    }

    private void OnDestroy()
    {
        StopPulseAndRestoreAlpha();
    }

    public bool TryValidateReferences(out string failureReason)
    {
        if (startButton == null)
        {
            failureReason = "Start Button is not assigned.";
            return false;
        }

        if (startButton.targetGraphic == null ||
            !startButton.targetGraphic.raycastTarget)
        {
            failureReason =
                "Start Button requires a raycast-enabled full-screen target Graphic.";
            return false;
        }

        if (startMessageText == null)
        {
            failureReason = "Start Message Text is not assigned.";
            return false;
        }

        if (startMessageText.raycastTarget)
        {
            failureReason =
                "Start Message Text must not block pointer raycasts.";
            return false;
        }

        failureReason = null;
        return true;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        SetInteractionEnabled(true);
    }

    public void HideAndLock()
    {
        SetInteractionEnabled(false);
        gameObject.SetActive(false);
    }

    public void SetInteractionEnabled(bool enabled)
    {
        isInteractionEnabled = enabled && isActiveAndEnabled;

        if (startButton != null)
        {
            startButton.interactable = isInteractionEnabled;
        }
    }

    private void BindButton()
    {
        if (startButton == null)
        {
            return;
        }

        startButton.onClick.RemoveListener(HandleStartClicked);
        startButton.onClick.AddListener(HandleStartClicked);
    }

    private void UnbindButton()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
        }
    }

    private void HandleStartClicked()
    {
        if (!isInteractionEnabled)
        {
            return;
        }

        StartRequested?.Invoke();
    }

    private void CaptureAuthoredMessageAlpha()
    {
        if (hasCapturedAuthoredAlpha ||
            startMessageText == null)
        {
            return;
        }

        authoredMessageAlpha = startMessageText.color.a;
        hasCapturedAuthoredAlpha = true;
    }

    private void StartPulse()
    {
        StopPulseAndRestoreAlpha();

        if (startMessageText == null)
        {
            return;
        }

        float minimumAlpha = Mathf.Min(
            Mathf.Clamp01(pulseMinimumAlpha),
            Mathf.Clamp01(authoredMessageAlpha));

        if (Mathf.Approximately(minimumAlpha, authoredMessageAlpha))
        {
            return;
        }

        pulseTween = startMessageText
            .DOFade(minimumAlpha, Mathf.Max(0.01f, pulseHalfDuration))
            .SetEase(pulseEaseType)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopPulseAndRestoreAlpha()
    {
        if (pulseTween != null)
        {
            pulseTween.Kill();
            pulseTween = null;
        }

        if (startMessageText != null &&
            hasCapturedAuthoredAlpha)
        {
            Color color = startMessageText.color;
            color.a = authoredMessageAlpha;
            startMessageText.color = color;
        }
    }
}
