using System;
using UnityEngine;
using UnityEngine.UI;

// Authored on Window_ConfirmExitStage, the common background/content root.
public sealed class ConfirmExitStageWindow : MonoBehaviour
{
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private CanvasGroup visibility;
    private Button boundCancel, boundRestart, boundMainMenu;
    private Action cancelRequested, restartRequested, mainMenuRequested, presentationLost;
    private bool interactionEnabled;

    internal bool TryValidateReferences(out string reason)
    {
        reason = "Exit confirmation requires a full-stretch common root, blocking Image, content, and three distinct Buttons.";
        var root = transform as RectTransform;
        var background = GetComponent<Image>();
        if (!enabled || root == null || root.anchorMin != Vector2.zero || root.anchorMax != Vector2.one ||
            root.offsetMin != Vector2.zero || root.offsetMax != Vector2.zero || background == null ||
            !background.enabled || !background.raycastTarget || contentRoot == null || contentRoot == root ||
            contentRoot.parent != root || !contentRoot.gameObject.activeSelf ||
            !InContent(cancelButton) || !InContent(restartButton) || !InContent(mainMenuButton) ||
            cancelButton == restartButton || cancelButton == mainMenuButton || restartButton == mainMenuButton ||
            !cancelButton.enabled || !restartButton.enabled || !mainMenuButton.enabled ||
            !cancelButton.gameObject.activeSelf || !restartButton.gameObject.activeSelf || !mainMenuButton.gameObject.activeSelf)
            return false;
        // interactable is controlled by this view, not an authoring requirement.
        reason = string.Empty;
        return true;
    }

    private bool InContent(Component item) => item != null && item.transform.IsChildOf(contentRoot);

    internal void Attach()
    {
        if (visibility == null)
        {
            visibility = GetComponent<CanvasGroup>();
            if (visibility == null) visibility = gameObject.AddComponent<CanvasGroup>();
        }
        UnbindButtons();
        boundCancel = cancelButton;
        boundRestart = restartButton;
        boundMainMenu = mainMenuButton;
        boundCancel.onClick.AddListener(HandleCancel);
        boundRestart.onClick.AddListener(HandleRestart);
        boundMainMenu.onClick.AddListener(HandleMainMenu);
        HideAndLock();
    }

    internal void Show(Action cancel, Action restart, Action mainMenu, Action lost)
    {
        cancelRequested = cancel;
        restartRequested = restart;
        mainMenuRequested = mainMenu;
        presentationLost = lost;
        SetInteractionEnabled(false);
        visibility.alpha = 1f;
        visibility.blocksRaycasts = true;
        gameObject.SetActive(true);
    }

    internal void SetInteractionEnabled(bool value)
    {
        interactionEnabled = value && IsVisible;
        if (visibility != null) visibility.interactable = interactionEnabled;
        if (boundCancel != null) boundCancel.interactable = interactionEnabled;
        if (boundRestart != null) boundRestart.interactable = interactionEnabled;
        if (boundMainMenu != null) boundMainMenu.interactable = interactionEnabled;
    }

    // Locking actions must not look like presentation loss or remove the mask.
    internal bool IsVisible => isActiveAndEnabled && visibility != null && visibility.alpha > 0f &&
        visibility.blocksRaycasts && contentRoot != null && contentRoot.gameObject.activeInHierarchy;
    internal bool IsInteractionEnabled => interactionEnabled && IsVisible;

    internal void HideAndLock()
    {
        ClearCallbacks();
        SetInteractionEnabled(false);
        if (visibility != null)
        {
            visibility.alpha = 0f;
            visibility.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    internal void Detach()
    {
        UnbindButtons();
        HideAndLock();
    }

    private void UnbindButtons()
    {
        if (boundCancel != null) boundCancel.onClick.RemoveListener(HandleCancel);
        if (boundRestart != null) boundRestart.onClick.RemoveListener(HandleRestart);
        if (boundMainMenu != null) boundMainMenu.onClick.RemoveListener(HandleMainMenu);
        boundCancel = boundRestart = boundMainMenu = null;
    }

    private void HandleCancel() { if (IsInteractionEnabled) cancelRequested?.Invoke(); }
    private void HandleRestart() { if (IsInteractionEnabled) restartRequested?.Invoke(); }
    private void HandleMainMenu() { if (IsInteractionEnabled) mainMenuRequested?.Invoke(); }

    private void ClearCallbacks()
    {
        cancelRequested = restartRequested = mainMenuRequested = presentationLost = null;
    }

    private void NotifyLost()
    {
        var callback = presentationLost;
        ClearCallbacks();
        SetInteractionEnabled(false);
        callback?.Invoke();
    }

    private void OnDisable() { NotifyLost(); }
    private void OnDestroy() { UnbindButtons(); NotifyLost(); }
}
