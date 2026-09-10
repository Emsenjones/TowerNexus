using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DraftUI : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Transform draftItemContainer;
    [SerializeField] private GameObject towerDraftItemPrefab;

    [Header("Re-roll")]
    [SerializeField] private Button rerollActiveButton;
    [SerializeField] private Button rerollInactiveButton;
    [SerializeField] private TMP_Text rerollCountText;
    [SerializeField] private ToastUI toastPrefab;
    [SerializeField] private Transform toastContainer;
    [SerializeField] private string noOtherChoicesMessage = "No other draft choices available.";

    private Action onReroll;
    private ToastUI toastInstance;
    private int presentationEpoch;

    private List<TowerContentUIItem> draftItems =
        new List<TowerContentUIItem>();
    private bool isBattleActive;

    public bool IsOpen => isActiveAndEnabled && rootObject != null && rootObject.activeInHierarchy;

    private void Awake()
    {
        if (rootObject != null && rootObject != gameObject)
        {
            rootObject.SetActive(false);
        }
    }

    public bool TryValidateReferences(out string failureReason)
    {
        if (rootObject == null)
        {
            failureReason = "Root Object is not assigned.";
            return false;
        }

        Graphic modalSurface = rootObject.GetComponent<Graphic>();

        if (modalSurface == null || !modalSurface.raycastTarget)
        {
            failureReason =
                "Root Object requires a raycast-enabled modal Graphic.";
            return false;
        }

        if (draftItemContainer == null)
        {
            failureReason = "Draft Item Container is not assigned.";
            return false;
        }

        if (towerDraftItemPrefab == null)
        {
            failureReason = "Tower Draft Item Prefab is not assigned.";
            return false;
        }

        TowerContentUIItem[] rootItems =
            towerDraftItemPrefab.GetComponents<TowerContentUIItem>();
        TowerContentUIItem[] allItems =
            towerDraftItemPrefab.GetComponentsInChildren<TowerContentUIItem>(
                true);

        if (rootItems.Length != 1 || allItems.Length != 1)
        {
            failureReason =
                "Tower Draft Item Prefab requires exactly one " +
                "TowerContentUIItem on its root.";
            return false;
        }

        if (!rootItems[0].TryValidateReferences(
                out string itemFailureReason))
        {
            failureReason =
                $"Tower Draft Item Prefab is invalid: {itemFailureReason}";
            return false;
        }

        if (rerollActiveButton == null || rerollInactiveButton == null ||
            rerollActiveButton == rerollInactiveButton || rerollCountText == null ||
            !rerollCountText.transform.IsChildOf(rerollActiveButton.transform) ||
            toastPrefab == null || toastContainer == null)
        {
            failureReason = "Re-roll buttons, count child, Toast prefab and container are required.";
            return false;
        }
        failureReason = string.Empty;
        return true;
    }

    public bool TryOpenDraft(
        IReadOnlyList<DraftResult> draftResults,
        Action<DraftResult> onSelected,
        out string failureReason)
    {
        if (!isBattleActive)
        {
            failureReason = "the Draft UI battle gate is closed.";
            return false;
        }

        presentationEpoch++;
        ClearDraftItems();

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }

        if (!TryValidateReferences(out failureReason))
        {
            return false;
        }

        if (onSelected == null)
        {
            failureReason = "the selection callback is missing.";
            return false;
        }

        if (draftResults == null || draftResults.Count == 0)
        {
            rootObject.SetActive(false);
            failureReason = "the Draft result list is empty.";
            return false;
        }

        int createdItemCount = 0;
        int openingEpoch = presentationEpoch;

        for (int i = 0; i < draftResults.Count; i++)
        {
            DraftResult draftResult = draftResults[i];

            if (draftResult == null || !draftResult.IsValid)
            {
                return FailOpen(
                    $"Draft result at index {i} is invalid.",
                    out failureReason);
            }

            if (draftResult.ResultType == DraftResultType.TowerDraft &&
                draftResult.TowerDefinition != null &&
                draftResult.TowerDefinition.TowerPrefab == null)
            {
                Debug.LogWarning($"Draft UI entry '{draftResult.DisplayName}' is missing a tower prefab reference.", draftResult.TowerDefinition);
            }

            GameObject itemObject = null;

            try
            {
                itemObject = Instantiate(
                    towerDraftItemPrefab,
                    draftItemContainer);

                if (!itemObject.TryGetComponent(
                        out TowerContentUIItem item))
                {
                    Destroy(itemObject);
                    return FailOpen(
                        "Tower Draft Item Prefab is missing " +
                        "TowerContentUIItem.",
                        out failureReason);
                }

                if (!item.TryInitializeSelectable(
                        draftResult,
                        result => { if (isBattleActive && IsOpen) onSelected(result); }))
                {
                    Destroy(itemObject);
                    return FailOpen(
                        $"Draft item {i} failed initialization.",
                        out failureReason);
                }

                if (!isBattleActive || openingEpoch != presentationEpoch)
                {
                    itemObject.SetActive(false);
                    Destroy(itemObject);
                    failureReason = "Draft opening was cancelled during item preparation.";
                    return false;
                }
                draftItems.Add(item);
                createdItemCount++;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                if (itemObject != null)
                {
                    itemObject.SetActive(false);
                    Destroy(itemObject);
                }

                return FailOpen(
                    $"Draft item {i} creation threw " +
                    $"{exception.GetType().Name}.",
                    out failureReason);
            }
        }

        if (createdItemCount == 0)
        {
            return FailOpen(
                "no selectable Draft items were created.",
                out failureReason);
        }

        rootObject.SetActive(true);
        failureReason = string.Empty;
        return true;
    }

    public void CloseDraft()
    {
        presentationEpoch++;
        onReroll = null;
        if (rerollActiveButton != null)
            rerollActiveButton.onClick.RemoveListener(HandleReroll);
        ClearToast();
        ClearDraftItems();

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    public void BeginBattle()
    {
        isBattleActive = true;
    }

    public void StopBattle()
    {
        isBattleActive = false;
        CloseDraft();
    }

    public void BindReroll(int remaining, bool allowed, Action callback)
    {
        onReroll = callback;
        rerollActiveButton.onClick.RemoveListener(HandleReroll);
        rerollActiveButton.onClick.AddListener(HandleReroll);
        rerollCountText.text = remaining.ToString();
        rerollActiveButton.gameObject.SetActive(remaining > 0);
        rerollInactiveButton.gameObject.SetActive(remaining <= 0);
        rerollActiveButton.interactable = allowed;
        // The exhausted button has feedback only, with no gameplay listener.
        rerollInactiveButton.interactable = true;
    }

    private void HandleReroll()
    {
        if (isBattleActive && IsOpen) onReroll?.Invoke();
    }

    public void ShowNoOtherChoices()
    {
        if (!isBattleActive || !IsOpen) return;
        if (toastInstance == null) toastInstance = Instantiate(toastPrefab, toastContainer);
        toastInstance.transform.SetAsLastSibling();
        if (!toastInstance.TryPlay(noOtherChoicesMessage, completed =>
            { if (toastInstance == completed) ClearToast(); }, out string reason))
        {
            Debug.LogWarning("Draft Toast failed: " + reason, this);
            ClearToast();
        }
    }

    private void ClearToast()
    {
        ToastUI previous = toastInstance;
        toastInstance = null;
        if (previous == null) return;
        previous.Cancel();
        previous.gameObject.SetActive(false);
        Destroy(previous.gameObject);
    }

    private void OnDisable()
    {
        presentationEpoch++;
        ClearToast();
    }

    public bool TryPrepareChoices(IReadOnlyList<DraftResult> choices,
        Action<DraftResult> selection, out DraftViewPreparation prepared, out string reason)
    {
        prepared = null;
        reason = "Draft presentation is unavailable.";
        if (!isBattleActive || !IsOpen || selection == null || choices == null || choices.Count == 0)
            return false;
        var batch = new DraftViewPreparation { Owner = this, Epoch = presentationEpoch };
        try
        {
            batch.StagingRoot = new GameObject("Prepared Draft choices");
            batch.StagingRoot.SetActive(false);
            batch.StagingRoot.transform.SetParent(draftItemContainer, false);
            foreach (var choice in choices)
            {
                var instance = Instantiate(towerDraftItemPrefab, batch.StagingRoot.transform);
                if (!instance.TryGetComponent(out TowerContentUIItem item))
                { Destroy(instance); throw new InvalidOperationException("Missing Draft item component."); }
                batch.Items.Add(item);
                if (!item.TryInitializeSelectable(choice, selection))
                    throw new InvalidOperationException("Draft item initialization failed.");
            }
            if (!CanCommitChoices(batch)) { batch.Dispose(); return false; }
            prepared = batch;
            reason = string.Empty;
            return true;
        }
        catch (Exception error)
        {
            batch.Dispose();
            reason = "Draft preparation failed: " + error.Message;
            return false;
        }
    }

    public bool CanCommitChoices(DraftViewPreparation batch)
    {
        if (!OwnsPresentation(batch) || batch.Transferred || batch.Items.Count == 0) return false;
        foreach (var item in batch.Items) if (item == null) return false;
        return true;
    }

    private bool OwnsPresentation(DraftViewPreparation batch) => batch != null &&
        batch.Owner == this && batch.Epoch == presentationEpoch && isBattleActive && IsOpen;

    public void CommitChoiceOwnership(DraftViewPreparation batch)
    {
        // Caller has just validated. No callbacks, allocation, or Unity lifecycle operations.
        batch.Previous = draftItems;
        draftItems = batch.Items;
        batch.Transferred = true;
    }

    public DraftRerollPresentation PresentChoices(DraftViewPreparation batch, out string reason)
    {
        reason = string.Empty;
        if (!OwnsPresentation(batch)) return DraftRerollPresentation.Cancelled;
        try
        {
            foreach (var item in batch.Previous)
            {
                if (!OwnsPresentation(batch)) return DraftRerollPresentation.Cancelled;
                if (item != null) { item.gameObject.SetActive(false); Destroy(item.gameObject); }
            }
            // Snapshot: lifecycle cancellation can detach/clear the live list.
            foreach (var item in batch.Items.ToArray())
            {
                if (!OwnsPresentation(batch)) return DraftRerollPresentation.Cancelled;
                if (item == null) throw new InvalidOperationException("Prepared Draft card became unavailable.");
                item.gameObject.SetActive(false);
                item.transform.SetParent(draftItemContainer, false);
                item.gameObject.SetActive(true);
            }
            return OwnsPresentation(batch) ? DraftRerollPresentation.Completed : DraftRerollPresentation.Cancelled;
        }
        catch (Exception error)
        {
            reason = error.Message;
            return OwnsPresentation(batch) ? DraftRerollPresentation.TechnicalFailure : DraftRerollPresentation.Cancelled;
        }
    }

    private bool FailOpen(
        string reason,
        out string failureReason)
    {
        CloseDraft();
        failureReason = reason;
        return false;
    }

    private void ClearDraftItems()
    {
        var previous = draftItems;
        draftItems = new List<TowerContentUIItem>();
        for (int i = previous.Count - 1; i >= 0; i--)
        {
            TowerContentUIItem uiItem = previous[i];

            if (uiItem != null)
            {
                uiItem.gameObject.SetActive(false);
                Destroy(uiItem.gameObject);
            }
        }

        previous.Clear();
    }
}


// HUD-owned, single-use preparation. Gameplay does not know concrete card components.
public sealed class DraftViewPreparation : IDisposable
{
    internal DraftUI Owner;
    internal int Epoch;
    internal GameObject StagingRoot;
    internal List<TowerContentUIItem> Items = new List<TowerContentUIItem>();
    internal List<TowerContentUIItem> Previous;
    internal bool Transferred;
    private bool disposed;
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Retire(Transferred ? Previous : Items);
        if (StagingRoot != null) UnityEngine.Object.Destroy(StagingRoot);
        StagingRoot = null;
        Owner = null;
    }
    private static void Retire(List<TowerContentUIItem> items)
    {
        if (items == null) return;
        foreach (var item in items.ToArray())
        {
            if (item == null) continue;
            try { item.gameObject.SetActive(false); }
            catch (Exception error) { Debug.LogException(error); }
            finally { if (item != null) UnityEngine.Object.Destroy(item.gameObject); }
        }
        items.Clear();
    }
}
