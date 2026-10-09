using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// This component lives on the common, initially hidden mask/content root.
public sealed class TowerInfoWindow : MonoBehaviour
{
    [Header("Window Layout")]
    [SerializeField] private RectTransform contentRoot;

    [Header("Basic Stats")]
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private Image towerIcon;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text attackRangeText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text attackCycleDurationText;
    [SerializeField] private TMP_Text killCountText;

    [Header("Acquired Upgrades")]
    [SerializeField] private RectTransform upgradeContainer;
    [SerializeField] private TowerUpgradeInfoItem upgradeItemPrefab;

    [Header("Controls")]
    [SerializeField] private Button closeButton;

    private CanvasGroup visibility;
    private Action closeRequested, cancelled;
    private Button boundCloseButton;

    internal sealed class PreparedContent : IDisposable
    {
        internal readonly List<TowerUpgradeInfoItem> Items = new List<TowerUpgradeInfoItem>();
        internal bool Disposed;
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            var outgoing = Items.ToArray();
            Items.Clear();
            foreach (var icon in outgoing) Retire(icon);
        }
    }

    internal bool TryValidateReferences(out string reason)
    {
        reason = "TowerInfoWindow requires its common RectTransform root, content, Grid, text/Image references, and Close button.";
        var root = transform as RectTransform;
        if (root == null || root.anchorMin != Vector2.zero || root.anchorMax != Vector2.one ||
            root.offsetMin != Vector2.zero || root.offsetMax != Vector2.zero)
        { reason = "TowerInfoWindow common root must stretch to its full-screen UI parent with zero offsets."; return false; }
        if (!enabled || root == null || contentRoot == null || contentRoot == root ||
            contentRoot.parent != root ||
            upgradeContainer == null || !upgradeContainer.IsChildOf(contentRoot) ||
            upgradeContainer.GetComponent<GridLayoutGroup>() == null ||
            !InContent(displayNameText) || !InContent(killCountText) || !InContent(towerIcon) ||
            !InContent(levelText) || !InContent(attackRangeText) || !InContent(attackText) ||
            !InContent(attackCycleDurationText) ||
            closeButton == null || !closeButton.transform.IsChildOf(contentRoot) || !closeButton.interactable ||
            upgradeItemPrefab == null || upgradeItemPrefab.transform.parent != null ||
            !contentRoot.gameObject.activeSelf || !upgradeContainer.gameObject.activeSelf)
            return false;
        if (!upgradeItemPrefab.TryValidateReferences(out reason)) return false;
        reason = string.Empty;
        return true;
    }

    private bool InContent(Component component) => component != null && component.transform.IsChildOf(contentRoot);

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
        killCountText.text = snapshot.KillCount.ToString(CultureInfo.InvariantCulture);
        levelText.text = snapshot.Level.ToString(CultureInfo.InvariantCulture);
        attackRangeText.text = snapshot.Stats.AttackRange.ToString("0.##", CultureInfo.InvariantCulture);
        attackText.text = snapshot.Stats.ResolvedBasicDamage.ToString("0.##", CultureInfo.InvariantCulture);
        attackCycleDurationText.text = snapshot.Stats.AttackCycleDuration.ToString("0.##", CultureInfo.InvariantCulture) + " s";
        SetIcon(towerIcon, snapshot.Icon);
        for (int i = 0; i < snapshot.UpgradeIcons.Length; i++)
        {
            if (content.Disposed) return false;
            var icon = Instantiate(upgradeItemPrefab, upgradeContainer);
            // Instantiate can call authored lifecycle code before returning.
            if (content.Disposed) { Retire(icon); return false; }
            if (icon == null) { reason = "Upgrade icon creation failed."; return false; }
            content.Items.Add(icon);
            if (!icon.TryInitialize(snapshot.UpgradeNames[i], snapshot.UpgradeIcons[i], snapshot.UpgradeLayers[i], out reason)) return false;
            if (content.Disposed) return false;
            icon.gameObject.SetActive(true);
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
        visibility.interactable && visibility.blocksRaycasts && contentRoot != null && contentRoot.gameObject.activeInHierarchy;
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

    private static void Retire(TowerUpgradeInfoItem icon)
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
