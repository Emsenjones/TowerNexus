using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class TowerStateUIManager : MonoBehaviour
{
    [SerializeField] private TowerStateUIItem stateUIItemPrefab;

    private readonly Dictionary<TowerInstance, TowerStateUIItem> items =
        new Dictionary<TowerInstance, TowerStateUIItem>();
    private readonly List<KeyValuePair<TowerInstance, TowerStateUIItem>> refreshItems =
        new List<KeyValuePair<TowerInstance, TowerStateUIItem>>();
    private TowerPlacementSubmission submission;
    private Action<TowerInstance> deploymentHandler;
    private Camera worldCamera;
    private RectTransform container;
    private Canvas canvas;
    private ulong bindingRevision;
    private bool canvasSubscribed;
    private bool clearing;
    private bool refreshing;

    internal void Bind(TowerPlacementSubmission source, Camera camera)
    {
        if (clearing) return;
        Clear();
        container = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        if (source == null || stateUIItemPrefab == null || container == null ||
            canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogWarning("Tower state UI requires a submission, Item Prefab, and RectTransform container under a Screen Space Overlay Canvas.", this);
            return;
        }

        submission = source;
        worldCamera = camera;
        if (camera == null)
            Debug.LogWarning("Tower state UI has no placement output Camera; labels will remain hidden.", this);
        ulong revision = bindingRevision;
        deploymentHandler = tower =>
        {
            if (!IsCurrent(source, revision) || !isActiveAndEnabled ||
                !source.OwnsDeployedTower(tower)) return;
            TryCreate(tower, source, revision);
        };
        source.OnTowerDeploymentCommitted += deploymentHandler;
        if (isActiveAndEnabled)
        {
            Reconcile();
            SubscribeCanvas();
        }
    }

    public void Remove(TowerInstance tower)
    {
        if (ReferenceEquals(tower, null) || !items.TryGetValue(tower, out TowerStateUIItem item)) return;
        items.Remove(tower);
        DisposeSafely(item);
    }

    public void Clear()
    {
        if (clearing) return;
        clearing = true;
        try
        {
            ++bindingRevision;
            TowerPlacementSubmission outgoing = submission;
            Action<TowerInstance> handler = deploymentHandler;
            submission = null;
            deploymentHandler = null;
            worldCamera = null;
            UnsubscribeCanvas();
            if (outgoing != null && handler != null) outgoing.OnTowerDeploymentCommitted -= handler;
            var snapshot = new List<TowerStateUIItem>(items.Values);
            items.Clear();
            foreach (TowerStateUIItem item in snapshot) DisposeSafely(item);
        }
        finally { clearing = false; }
    }

    private void OnEnable()
    {
        if (submission == null) return;
        try
        {
            Reconcile();
            SubscribeCanvas();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            Clear();
        }
    }

    private void OnDisable()
    {
        UnsubscribeCanvas();
        // Keep the runtime subscription; deployments while hidden are reconciled on enable.
        foreach (TowerStateUIItem item in new List<TowerStateUIItem>(items.Values))
            if (item != null) item.SetVisible(false);
    }

    private void OnDestroy() => Clear();

    private bool IsCurrent(TowerPlacementSubmission source, ulong revision) =>
        !clearing && ReferenceEquals(submission, source) && source != null && bindingRevision == revision;

    private static bool Contains(TowerPlacementSubmission source, TowerInstance tower)
    {
        if (source == null || tower == null) return false;
        // Membership deliberately survives StopBattle; OwnsDeployedTower includes the active gate.
        IReadOnlyList<TowerInstance> members = source.DeployedTowerInstances;
        for (int i = 0; i < members.Count; i++)
            if (ReferenceEquals(members[i], tower)) return true;
        return false;
    }

    private void Reconcile()
    {
        TowerPlacementSubmission source = submission;
        ulong revision = bindingRevision;
        if (!IsCurrent(source, revision)) return;
        RefreshItems();
        var members = new List<TowerInstance>(source.DeployedTowerInstances);
        foreach (TowerInstance tower in members)
        {
            if (!IsCurrent(source, revision) || !isActiveAndEnabled) return;
            TryCreate(tower, source, revision);
        }
        RefreshItems();
    }

    private void TryCreate(TowerInstance tower, TowerPlacementSubmission source, ulong revision)
    {
        if (!IsCurrent(source, revision) || !Contains(source, tower)) return;
        if (items.TryGetValue(tower, out TowerStateUIItem existing))
        {
            if (existing != null && existing.HasTarget)
            {
                existing.RefreshLevel();
                return;
            }
            Remove(tower);
        }

        TowerStateUIItem item = null;
        try
        {
            if (!tower.TryGetComponent(out TowerBehaviour behaviour) ||
                behaviour.VisualController == null || behaviour.VisualController.TowerModelRoot == null)
            {
                Debug.LogWarning("Tower state UI cannot bind a deployed Tower without TowerModelRoot.", tower);
                return;
            }
            item = Instantiate(stateUIItemPrefab, container);
            if (!item.TryInitialize(tower, behaviour.VisualController.TowerModelRoot, HandleItemReleased)) return;
            // Instantiation/initialization may trigger callbacks that clear or rebind this manager.
            if (!IsCurrent(source, revision) || !isActiveAndEnabled || !Contains(source, tower)) return;
            items.Add(tower, item);
            item.RefreshPosition(worldCamera, container);
            item = null; // Collection owns it only after successful initialization and presentation.
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally { DisposeSafely(item); }
    }

    private void HandleItemReleased(TowerInstance tower, TowerStateUIItem item)
    {
        if (!ReferenceEquals(tower, null) && items.TryGetValue(tower, out TowerStateUIItem current) &&
            ReferenceEquals(current, item)) items.Remove(tower);
    }

    private void SubscribeCanvas()
    {
        if (canvasSubscribed || submission == null || !isActiveAndEnabled) return;
        Canvas.preWillRenderCanvases += RefreshItems;
        canvasSubscribed = true;
    }

    private void UnsubscribeCanvas()
    {
        if (!canvasSubscribed) return;
        Canvas.preWillRenderCanvases -= RefreshItems;
        canvasSubscribed = false;
    }

    private void RefreshItems()
    {
        if (refreshing || submission == null || !isActiveAndEnabled) return;
        refreshing = true;
        TowerPlacementSubmission source = submission;
        ulong revision = bindingRevision;
        try
        {
            refreshItems.Clear();
            refreshItems.AddRange(items);
            foreach (KeyValuePair<TowerInstance, TowerStateUIItem> pair in refreshItems)
            {
                if (!IsCurrent(source, revision) || !isActiveAndEnabled) return;
                if (!items.TryGetValue(pair.Key, out TowerStateUIItem current) ||
                    !ReferenceEquals(current, pair.Value)) continue;
                if (!Contains(source, pair.Key) || current == null || !current.HasTarget)
                {
                    Remove(pair.Key);
                    continue;
                }
                try
                {
                    if (canvas != null && canvas.isActiveAndEnabled)
                        current.RefreshPosition(worldCamera, container);
                    else current.SetVisible(false);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    Remove(pair.Key);
                }
            }
        }
        finally
        {
            refreshItems.Clear();
            refreshing = false;
        }
    }

    private void DisposeSafely(TowerStateUIItem item)
    {
        if (item == null) return;
        try { item.Dispose(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
