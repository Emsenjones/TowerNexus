using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

internal readonly struct PreparedPendingDraftConsumption
{
    internal PreparedPendingDraftConsumption(
        PendingDraftUIItem item,
        int ownedIndex)
    {
        Item = item;
        OwnedIndex = ownedIndex;
    }

    internal PendingDraftUIItem Item { get; }
    internal int OwnedIndex { get; }
}

public class BattleHUDUI : MonoBehaviour
{
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private DraftUI draftUI;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [FormerlySerializedAs("expSlider")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private RectTransform pendingDraftContainer;
    [SerializeField] private RectTransform pendingDraftDragVisualRoot;
    [SerializeField] private GameObject pendingDraftItemPrefab;

    private readonly List<PendingDraftUIItem> pendingDraftItems = new List<PendingDraftUIItem>();
    private bool isBattleActive;

    public IReadOnlyList<PendingDraftUIItem> PendingDraftItems => pendingDraftItems;
    public bool IsDraftOpen => draftUI != null && draftUI.IsOpen;

    private void OnEnable()
    {
        SubscribeToPlayerSystem();
        InitializeFromPlayerSystem();
        draftUI?.CloseDraft();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerSystem();
    }

    public void UpdateLevel(int level)
    {
        if (levelText == null)
        {
            Debug.LogWarning("Battle HUD UI cannot update level: level text is not assigned.", this);
            return;
        }

        levelText.text = level.ToString();
    }

    public void UpdateProgress(float currentProgress, float requiredProgress)
    {
        if (progressSlider == null)
        {
            Debug.LogWarning("Battle HUD UI cannot update progress: progress slider is not assigned.", this);
            return;
        }

        if (requiredProgress <= 0f)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 1f;
            return;
        }

        progressSlider.minValue = 0f;
        progressSlider.maxValue = requiredProgress;
        progressSlider.value = Mathf.Clamp(currentProgress, 0f, requiredProgress);
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (hpText == null)
        {
            Debug.LogWarning("Battle HUD UI cannot update HP: HP text is not assigned.", this);
            return;
        }

        hpText.text = currentHealth.ToString();
    }


    public bool TryValidateBattleReferences(out string failureReason)
    {
        if (playerSystem == null)
        {
            failureReason = "Player System is not assigned.";
            return false;
        }

        if (draftUI == null)
        {
            failureReason = "Draft UI is not assigned.";
            return false;
        }

        if (!draftUI.TryValidateReferences(
                out string draftUiFailureReason))
        {
            failureReason = $"Draft UI is invalid: {draftUiFailureReason}";
            return false;
        }

        if (towerPlacementController == null)
        {
            failureReason = "Tower Placement Controller is not assigned.";
            return false;
        }

        if (levelText == null ||
            hpText == null ||
            progressSlider == null)
        {
            failureReason =
                "one or more Player information presentation references are missing.";
            return false;
        }

        if (!TryValidatePendingItemRoots(out failureReason))
        {
            return false;
        }

        if (pendingDraftItemPrefab == null)
        {
            failureReason = "Pending Draft Item Prefab is not assigned.";
            return false;
        }

        PendingDraftUIItem[] rootItems =
            pendingDraftItemPrefab.GetComponents<PendingDraftUIItem>();
        PendingDraftUIItem[] allItems =
            pendingDraftItemPrefab.GetComponentsInChildren<PendingDraftUIItem>(
                true);

        if (rootItems.Length != 1 || allItems.Length != 1)
        {
            failureReason =
                "Pending Draft Item Prefab requires exactly one " +
                "PendingDraftUIItem on its root.";
            return false;
        }

        if (!rootItems[0].TryValidateReferences(
                out string itemFailureReason))
        {
            failureReason =
                $"Pending Draft Item Prefab is invalid: {itemFailureReason}";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool TryAddPendingDraft(
        DraftResult draftResult,
        out PendingDraftUIItem committedItem,
        out string failureReason)
    {
        committedItem = null;

        if (!isBattleActive)
        {
            failureReason = "the Battle HUD battle gate is closed.";
            return false;
        }

        if (draftResult == null || !draftResult.IsValid)
        {
            failureReason = "the Draft result is invalid.";
            return false;
        }

        if (!TryValidatePendingItemRoots(out failureReason))
        {
            return false;
        }

        if (pendingDraftItemPrefab == null)
        {
            failureReason = "Pending Draft Item Prefab is not assigned.";
            return false;
        }

        GameObject itemObject = null;

        try
        {
            itemObject = Instantiate(
                pendingDraftItemPrefab,
                pendingDraftContainer);

            if (itemObject == null)
            {
                failureReason =
                    "Unity did not create the Pending Draft Item instance.";
                return false;
            }

            if (!itemObject.TryGetComponent(
                    out PendingDraftUIItem item))
            {
                return FailPendingItemCreation(
                    itemObject,
                    "Pending Draft Item Prefab is missing PendingDraftUIItem.",
                    out failureReason);
            }

            if (!item.TryInitialize(
                    draftResult,
                    towerPlacementController,
                    pendingDraftContainer,
                    pendingDraftDragVisualRoot,
                    out string initializationFailureReason))
            {
                return FailPendingItemCreation(
                    itemObject,
                    initializationFailureReason,
                    out failureReason);
            }

            item.BeginBattle();
            pendingDraftItems.Add(item);
            committedItem = item;
            failureReason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return FailPendingItemCreation(
                itemObject,
                $"Pending Draft Item creation threw " +
                $"{exception.GetType().Name}.",
                out failureReason);
        }
    }

    public void RemovePendingDraft(PendingDraftUIItem item)
    {
        if (!OwnsPendingDraft(item))
        {
            Debug.LogWarning(
                "Battle HUD UI cannot remove pending draft: the item is not " +
                "owned by the active pending-item collection.",
                this);
            return;
        }

        ConsumePendingDraft(item);
    }

    internal bool OwnsPendingDraft(PendingDraftUIItem item)
    {
        return isBattleActive &&
               item != null &&
               !item.IsConsumed &&
               pendingDraftItems.Contains(item);
    }

    internal bool TryPreparePendingDraftConsumption(
        PendingDraftUIItem item,
        out PreparedPendingDraftConsumption preparedConsumption)
    {
        preparedConsumption = default;

        if (!isBattleActive || item == null || item.IsConsumed)
        {
            return false;
        }

        int ownedIndex = pendingDraftItems.IndexOf(item);

        if (ownedIndex < 0)
        {
            return false;
        }

        preparedConsumption = new PreparedPendingDraftConsumption(
            item,
            ownedIndex);
        return true;
    }

    internal void CommitPreparedPendingDraftConsumption(
        PreparedPendingDraftConsumption preparedConsumption)
    {
        pendingDraftItems.RemoveAt(preparedConsumption.OwnedIndex);
        preparedConsumption.Item.MarkConsumed();
    }

    internal void ReleaseConsumedPendingDraftView(PendingDraftUIItem item)
    {
        if (item != null)
        {
            Destroy(item.gameObject);
        }
    }

    internal void ConsumePendingDraft(PendingDraftUIItem item)
    {
        if (!TryPreparePendingDraftConsumption(
                item,
                out PreparedPendingDraftConsumption preparedConsumption))
        {
            return;
        }

        CommitPreparedPendingDraftConsumption(preparedConsumption);
        ReleaseConsumedPendingDraftView(item);
    }

    public bool IsScreenPositionInsideDraftItemInteractionArea(Vector2 screenPosition)
    {
        if (pendingDraftContainer == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            pendingDraftContainer,
            screenPosition);
    }

    public bool TryOpenDraft(
        IReadOnlyList<DraftResult> draftResults,
        Action<DraftResult> onSelected,
        out string failureReason)
    {
        if (!isBattleActive)
        {
            failureReason = "the Battle HUD battle gate is closed.";
            return false;
        }

        if (draftUI == null)
        {
            failureReason = "Draft UI is not assigned.";
            return false;
        }

        return draftUI.TryOpenDraft(
            draftResults,
            onSelected,
            out failureReason);
    }

    public void CloseDraft()
    {
        draftUI?.CloseDraft();
    }

    public void BeginBattle()
    {
        isBattleActive = true;
        draftUI?.BeginBattle();

        for (int i = pendingDraftItems.Count - 1; i >= 0; i--)
        {
            PendingDraftUIItem pendingItem = pendingDraftItems[i];

            if (pendingItem == null)
            {
                pendingDraftItems.RemoveAt(i);
                continue;
            }

            pendingItem.BeginBattle();
        }
    }

    public void StopBattle()
    {
        isBattleActive = false;
        draftUI?.StopBattle();

        for (int i = pendingDraftItems.Count - 1; i >= 0; i--)
        {
            PendingDraftUIItem pendingItem = pendingDraftItems[i];

            if (pendingItem == null)
            {
                pendingDraftItems.RemoveAt(i);
                continue;
            }

            pendingItem.StopBattle();
        }
    }

    public void ClearStageRuntime()
    {
        StopBattle();

        for (int i = pendingDraftItems.Count - 1; i >= 0; i--)
        {
            PendingDraftUIItem pendingItem = pendingDraftItems[i];

            if (pendingItem == null)
            {
                continue;
            }

            pendingItem.gameObject.SetActive(false);
            Destroy(pendingItem.gameObject);
        }

        pendingDraftItems.Clear();
    }

    private void SubscribeToPlayerSystem()
    {
        if (playerSystem == null)
        {
            return;
        }

        playerSystem.OnLevelChanged += UpdateLevel;
        playerSystem.OnProgressChanged += HandleProgressChanged;
        playerSystem.OnHealthChanged += UpdateHealth;
        playerSystem.OnBattleStateInitialized += InitializeFromPlayerSystem;
    }

    private void UnsubscribeFromPlayerSystem()
    {
        if (playerSystem == null)
        {
            return;
        }

        playerSystem.OnLevelChanged -= UpdateLevel;
        playerSystem.OnProgressChanged -= HandleProgressChanged;
        playerSystem.OnHealthChanged -= UpdateHealth;
        playerSystem.OnBattleStateInitialized -= InitializeFromPlayerSystem;
    }

    private void InitializeFromPlayerSystem()
    {
        if (playerSystem == null)
        {
            return;
        }

        UpdateLevel(playerSystem.CurrentLevel);
        UpdateProgress(playerSystem.CurrentProgress, playerSystem.RequiredProgress);
        UpdateHealth(playerSystem.CurrentHealth, playerSystem.MaxHealth);
    }

    private void HandleProgressChanged(int currentProgress, int requiredProgress)
    {
        UpdateProgress(currentProgress, requiredProgress);
    }

    private bool TryValidatePendingItemRoots(out string failureReason)
    {
        if (pendingDraftContainer == null)
        {
            failureReason = "Pending Draft Container is not assigned.";
            return false;
        }

        if (pendingDraftDragVisualRoot == null)
        {
            failureReason = "Pending Draft Drag Visual Root is not assigned.";
            return false;
        }

        if (pendingDraftContainer == pendingDraftDragVisualRoot)
        {
            failureReason =
                "Pending Draft Container and Drag Visual Root must be distinct.";
            return false;
        }

        if (!pendingDraftContainer.IsChildOf(transform) ||
            !pendingDraftDragVisualRoot.IsChildOf(transform))
        {
            failureReason =
                "both Pending Draft roots must be descendants of BattleHUDUI.";
            return false;
        }

        if (pendingDraftDragVisualRoot.IsChildOf(pendingDraftContainer) ||
            pendingDraftContainer.IsChildOf(pendingDraftDragVisualRoot))
        {
            failureReason =
                "Pending Draft roots cannot contain one another.";
            return false;
        }

        Canvas containerCanvas =
            pendingDraftContainer.GetComponentInParent<Canvas>(true);
        Canvas dragCanvas =
            pendingDraftDragVisualRoot.GetComponentInParent<Canvas>(true);

        if (containerCanvas == null ||
            dragCanvas == null ||
            containerCanvas != dragCanvas)
        {
            failureReason =
                "Pending Draft roots must use the same UI Canvas space.";
            return false;
        }

        if (pendingDraftDragVisualRoot.GetComponent<LayoutGroup>() != null ||
            pendingDraftDragVisualRoot.GetComponent<ContentSizeFitter>() != null)
        {
            failureReason =
                "Pending Draft Drag Visual Root cannot own layout or content sizing.";
            return false;
        }

        if (pendingDraftDragVisualRoot.GetComponentsInParent<Mask>(true).Length > 0 ||
            pendingDraftDragVisualRoot.GetComponentsInParent<RectMask2D>(true).Length > 0)
        {
            failureReason =
                "Pending Draft Drag Visual Root cannot be clipped by a UI Mask.";
            return false;
        }

        Graphic[] rootGraphics =
            pendingDraftDragVisualRoot.GetComponents<Graphic>();

        for (int i = 0; i < rootGraphics.Length; i++)
        {
            if (rootGraphics[i] != null && rootGraphics[i].raycastTarget)
            {
                failureReason =
                    "Pending Draft Drag Visual Root cannot introduce a " +
                    "raycast-blocking Graphic.";
                return false;
            }
        }

        if (pendingDraftDragVisualRoot.rect.width <= 0f ||
            pendingDraftDragVisualRoot.rect.height <= 0f)
        {
            failureReason =
                "Pending Draft Drag Visual Root requires a positive coordinate area.";
            return false;
        }

        if (transform is RectTransform battleHudRoot &&
            !DoesRectContainRect(
                pendingDraftDragVisualRoot,
                battleHudRoot))
        {
            failureReason =
                "Pending Draft Drag Visual Root must cover the Battle HUD area.";
            return false;
        }

        if (!IsRenderedAfterWithinHierarchy(
                pendingDraftContainer,
                pendingDraftDragVisualRoot,
                transform))
        {
            failureReason =
                "Pending Draft Drag Visual Root must be ordered after the " +
                "Pending Draft Container within the Battle HUD hierarchy.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private static bool DoesRectContainRect(
        RectTransform containingRect,
        RectTransform containedRect)
    {
        Vector3[] worldCorners = new Vector3[4];
        containedRect.GetWorldCorners(worldCorners);

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 localCorner =
                containingRect.InverseTransformPoint(worldCorners[i]);
            Rect containerBounds = containingRect.rect;
            const float edgeTolerance = 0.5f;

            if (localCorner.x < containerBounds.xMin - edgeTolerance ||
                localCorner.x > containerBounds.xMax + edgeTolerance ||
                localCorner.y < containerBounds.yMin - edgeTolerance ||
                localCorner.y > containerBounds.yMax + edgeTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRenderedAfterWithinHierarchy(
        Transform ordinaryContent,
        Transform overlayContent,
        Transform boundary)
    {
        List<Transform> ordinaryPath =
            BuildPathFromBoundary(ordinaryContent, boundary);
        List<Transform> overlayPath =
            BuildPathFromBoundary(overlayContent, boundary);
        int sharedCount = Mathf.Min(
            ordinaryPath.Count,
            overlayPath.Count);

        for (int i = 0; i < sharedCount; i++)
        {
            if (ordinaryPath[i] == overlayPath[i])
            {
                continue;
            }

            return overlayPath[i].GetSiblingIndex() >=
                   ordinaryPath[i].GetSiblingIndex();
        }

        return false;
    }

    private static List<Transform> BuildPathFromBoundary(
        Transform leaf,
        Transform boundary)
    {
        List<Transform> reversedPath = new List<Transform>();
        Transform current = leaf;

        while (current != null && current != boundary)
        {
            reversedPath.Add(current);
            current = current.parent;
        }

        reversedPath.Reverse();
        return reversedPath;
    }

    private bool FailPendingItemCreation(
        GameObject itemObject,
        string reason,
        out string failureReason)
    {
        if (itemObject != null)
        {
            itemObject.SetActive(false);
            Destroy(itemObject);
        }

        failureReason = string.IsNullOrWhiteSpace(reason)
            ? "Pending Draft Item initialization failed."
            : reason;
        return false;
    }
}
