using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// This component lives on the common, initially hidden mask/content root.
public sealed class TowerInfoWindow : MonoBehaviour
{
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform basicStatsContainer;
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image towerIcon;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text attackRangeText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private RectTransform upgradeContainer;
    [SerializeField] private Image upgradeIconPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundMask;

    private CanvasGroup visibility;
    private Action closeRequested, cancelled;
    private Button boundCloseButton;

    internal sealed class PreparedContent : IDisposable
    {
        internal readonly List<Image> Icons = new List<Image>();
        internal bool Disposed;
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            var outgoing = Icons.ToArray();
            Icons.Clear();
            foreach (var icon in outgoing) Retire(icon);
        }
    }

    internal bool TryValidateReferences(out string reason)
    {
        reason = "TowerInfoWindow requires its common RectTransform root, content, stats, Grid, text/Image references, Close button, and mask.";
        var root = transform as RectTransform;
        if (root == null || root.anchorMin != Vector2.zero || root.anchorMax != Vector2.one ||
            root.offsetMin != Vector2.zero || root.offsetMax != Vector2.zero)
        { reason = "TowerInfoWindow common root must stretch to its full-screen UI parent with zero offsets."; return false; }
        if (!enabled || root == null || contentRoot == null || contentRoot == root ||
            contentRoot.parent != root || backgroundMask == null || backgroundMask.transform.parent != root ||
            !backgroundMask.enabled || !backgroundMask.raycastTarget ||
            backgroundMask.transform.GetSiblingIndex() >= contentRoot.GetSiblingIndex() ||
            basicStatsContainer == null || !basicStatsContainer.IsChildOf(contentRoot) ||
            upgradeContainer == null || !upgradeContainer.IsChildOf(contentRoot) ||
            upgradeContainer.GetComponent<GridLayoutGroup>() == null ||
            !InStats(displayNameText) || !InStats(descriptionText) || !InStats(towerIcon) ||
            !InStats(levelText) || !InStats(attackRangeText) || !InStats(attackText) ||
            closeButton == null || !closeButton.transform.IsChildOf(contentRoot) || !closeButton.interactable ||
            upgradeIconPrefab == null || upgradeIconPrefab.transform.parent != null ||
            !contentRoot.gameObject.activeSelf || !backgroundMask.gameObject.activeSelf ||
            !upgradeContainer.gameObject.activeSelf || !basicStatsContainer.gameObject.activeSelf)
            return false;
        var maskRect = backgroundMask.transform as RectTransform;
        if (maskRect == null || maskRect.anchorMin != Vector2.zero || maskRect.anchorMax != Vector2.one ||
            maskRect.offsetMin != Vector2.zero || maskRect.offsetMax != Vector2.zero)
        { reason = "TowerInfoWindow mask must stretch to the common root with zero offsets."; return false; }
        reason = string.Empty;
        return true;
    }

    private bool InStats(Component component) => component != null && component.transform.IsChildOf(basicStatsContainer);

    internal void Attach()
    {
        if (visibility == null)
        {
            visibility = GetComponent<CanvasGroup>();
            if (visibility == null) visibility = gameObject.AddComponent<CanvasGroup>();
        }
        if (boundCloseButton != closeButton)
        {
            if (boundCloseButton != null) boundCloseButton.onClick.RemoveListener(HandleClose);
            boundCloseButton = closeButton;
            boundCloseButton.onClick.AddListener(HandleClose);
        }
        Hide();
    }

    internal bool TryPrepare(TowerInspectionSnapshot snapshot, PreparedContent content, out string reason)
    {
        reason = "TowerInfoWindow preparation was cancelled.";
        displayNameText.text = snapshot.DisplayName ?? string.Empty;
        descriptionText.text = snapshot.Description ?? string.Empty;
        levelText.text = snapshot.Level.ToString(CultureInfo.InvariantCulture);
        attackRangeText.text = snapshot.Stats.AttackRange.ToString("0.##", CultureInfo.InvariantCulture);
        attackText.text = snapshot.Stats.ResolvedBasicDamage.ToString("0.##", CultureInfo.InvariantCulture);
        SetIcon(towerIcon, snapshot.Icon);
        for (int i = 0; i < snapshot.UpgradeIcons.Length; i++)
        {
            if (content.Disposed) return false;
            var icon = Instantiate(upgradeIconPrefab, upgradeContainer);
            // Instantiate can call authored lifecycle code before returning.
            if (content.Disposed) { Retire(icon); return false; }
            if (icon == null) { reason = "Upgrade icon creation failed."; return false; }
            content.Icons.Add(icon);
            icon.gameObject.SetActive(true);
            icon.raycastTarget = false;
            SetIcon(icon, snapshot.UpgradeIcons[i]);
        }
        if (content.Disposed) return false;
        reason = string.Empty;
        return true;
    }

    private void SetIcon(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        if (sprite == null) Debug.LogWarning("TowerInfoWindow has a missing authored icon; its slot remains empty.", this);
    }

    internal void Show(Action close, Action lost)
    {
        closeRequested = close;
        cancelled = lost;
        visibility.alpha = 1f;
        visibility.interactable = true;
        visibility.blocksRaycasts = true;
        gameObject.SetActive(true);
    }

    internal void Hide()
    {
        closeRequested = null;
        cancelled = null;
        if (visibility != null)
        {
            visibility.alpha = 0f;
            visibility.interactable = false;
            visibility.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    internal bool IsVisible => isActiveAndEnabled && visibility != null && visibility.alpha > 0f &&
        visibility.interactable && visibility.blocksRaycasts && backgroundMask != null && backgroundMask.enabled &&
        backgroundMask.raycastTarget && backgroundMask.gameObject.activeInHierarchy && contentRoot != null && contentRoot.gameObject.activeInHierarchy;
    internal void Detach()
    {
        if (boundCloseButton != null) boundCloseButton.onClick.RemoveListener(HandleClose);
        boundCloseButton = null;
        Hide();
    }
    private void HandleClose() { var callback = closeRequested; callback?.Invoke(); }
    private void OnDisable() { NotifyLost(); }
    private void OnDestroy()
    {
        if (boundCloseButton != null) boundCloseButton.onClick.RemoveListener(HandleClose);
        boundCloseButton = null;
        NotifyLost();
    }
    private void NotifyLost()
    {
        var callback = cancelled;
        closeRequested = null;
        cancelled = null;
        callback?.Invoke();
    }

    private static void Retire(Image icon)
    {
        if (icon == null) return;
        try { icon.gameObject.SetActive(false); }
        catch (Exception error) { Debug.LogException(error); }
        try { if (icon != null) icon.transform.SetParent(null, false); }
        catch (Exception error) { Debug.LogException(error); }
        try { if (icon != null) Destroy(icon.gameObject); }
        catch (Exception error) { Debug.LogException(error); }
    }
}
