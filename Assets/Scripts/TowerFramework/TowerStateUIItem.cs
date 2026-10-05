using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class TowerStateUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private RectTransform levelDisplay;

    private TowerInstance boundTower;
    private Transform modelRoot;
    private RectTransform itemRect;
    private Action<TowerInstance, TowerStateUIItem> onReleased;
    private bool disposed;

    internal bool HasTarget => !disposed && boundTower != null && modelRoot != null;

    internal bool TryInitialize(TowerInstance tower, Transform anchor,
        Action<TowerInstance, TowerStateUIItem> released)
    {
        if (disposed || !ReferenceEquals(boundTower, null)) return false;
        itemRect = transform as RectTransform;
        if (tower == null || anchor == null || itemRect == null || levelText == null ||
            levelDisplay == null || levelDisplay == itemRect ||
            !levelDisplay.IsChildOf(itemRect) || !levelText.transform.IsChildOf(levelDisplay))
        {
            Debug.LogWarning("Tower state UI requires a Tower, TowerModelRoot, and a child LevelDisplay containing LevelText.", this);
            return false;
        }

        boundTower = tower;
        modelRoot = anchor;
        onReleased = released;
        SetVisible(false);
        levelText.raycastTarget = false;
        boundTower.OnLevelChanged += HandleLevelChanged;
        RefreshLevel();
        return true;
    }

    internal void RefreshLevel()
    {
        if (HasTarget && levelText != null) levelText.SetText("{0}", boundTower.CurrentLevel);
    }

    internal void RefreshPosition(Camera worldCamera, RectTransform container)
    {
        if (!HasTarget || !isActiveAndEnabled || !boundTower.gameObject.activeInHierarchy ||
            worldCamera == null || !worldCamera.isActiveAndEnabled || container == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 viewport = worldCamera.WorldToViewportPoint(modelRoot.position);
        if (!(viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
              viewport.y >= 0f && viewport.y <= 1f))
        {
            SetVisible(false);
            return;
        }

        Vector3 screen = worldCamera.WorldToScreenPoint(modelRoot.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container, screen, null, out Vector2 localPosition))
        {
            SetVisible(false);
            return;
        }

        // The converted position is relative to the parent's pivot, not its anchors.
        itemRect.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        SetVisible(true);
    }

    internal void SetVisible(bool visible)
    {
        if (levelDisplay != null && levelDisplay.gameObject.activeSelf != visible)
            levelDisplay.gameObject.SetActive(visible);
    }

    public void Dispose()
    {
        if (disposed) return;
        ReleaseBinding();
        Destroy(gameObject);
    }

    private void OnEnable() => RefreshLevel();
    private void OnDisable() => SetVisible(false);
    private void OnDestroy() => ReleaseBinding();

    private void HandleLevelChanged(TowerInstance tower, int previousLevel, int currentLevel)
    {
        if (!disposed && ReferenceEquals(boundTower, tower)) RefreshLevel();
    }

    private void ReleaseBinding()
    {
        if (disposed) return;
        disposed = true;
        TowerInstance outgoingTower = boundTower;
        Action<TowerInstance, TowerStateUIItem> released = onReleased;
        boundTower = null;
        modelRoot = null;
        onReleased = null;
        // Unity's destroyed-object equality must not skip managed event cleanup.
        if (!ReferenceEquals(outgoingTower, null)) outgoingTower.OnLevelChanged -= HandleLevelChanged;
        SetVisible(false);
        released?.Invoke(outgoingTower, this);
    }
}
