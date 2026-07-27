using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageVictoryWindowView : MonoBehaviour
{
    private const string NextStageLabel = "Next Stage";
    private const string BackToMainMenuLabel = "Back to Main Menu";

    [SerializeField] private Graphic modalBlocker;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonLabel;

    private bool isInteractionEnabled;

    public event Action ContinueRequested;

    public bool IsInteractionEnabled => isInteractionEnabled;

    private void Awake()
    {
        SetInteractionEnabled(false);
    }

    private void OnEnable()
    {
        BindButton();
    }

    private void OnDisable()
    {
        UnbindButton();
        SetInteractionEnabled(false);
    }

    public bool TryValidateReferences(out string failureReason)
    {
        if (modalBlocker == null || !modalBlocker.raycastTarget)
        {
            failureReason =
                "Victory Window requires a raycast-enabled modal blocker Graphic.";
            return false;
        }

        if (continueButton == null)
        {
            failureReason = "Continue Button is not assigned.";
            return false;
        }

        if (continueButtonLabel == null)
        {
            failureReason = "Continue Button Label is not assigned.";
            return false;
        }

        failureReason = null;
        return true;
    }

    public void Show(bool hasNextStage)
    {
        SetInteractionEnabled(false);
        continueButtonLabel.text =
            hasNextStage ? NextStageLabel : BackToMainMenuLabel;
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

        if (continueButton != null)
        {
            continueButton.interactable = isInteractionEnabled;
        }
    }

    private void BindButton()
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.onClick.RemoveListener(HandleContinueClicked);
        continueButton.onClick.AddListener(HandleContinueClicked);
    }

    private void UnbindButton()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(HandleContinueClicked);
        }
    }

    private void HandleContinueClicked()
    {
        if (!isInteractionEnabled)
        {
            return;
        }

        ContinueRequested?.Invoke();
    }
}
