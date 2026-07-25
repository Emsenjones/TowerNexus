using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BattleHUDUI : MonoBehaviour
{
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private DraftUI draftUI;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [FormerlySerializedAs("expSlider")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Transform pendingDraftContainer;
    [SerializeField] private GameObject pendingDraftItemPrefab;

    private readonly List<PendingDraftUIItem> pendingDraftItems = new List<PendingDraftUIItem>();
    private bool isBattleActive;

    public IReadOnlyList<PendingDraftUIItem> PendingDraftItems => pendingDraftItems;

    private void OnEnable()
    {
        SubscribeToPlayerSystem();
        InitializeFromPlayerSystem();
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

    public void AddPendingTower(TowerDefinition towerDefinition)
    {
        AddPendingDraft(DraftResult.CreateTowerDraft(towerDefinition));
    }

    public void AddPendingDraft(DraftResult draftResult)
    {
        if (!isBattleActive)
        {
            return;
        }

        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Battle HUD UI cannot add pending draft: draft result is invalid.", this);
            return;
        }

        if (pendingDraftContainer == null)
        {
            Debug.LogWarning("Battle HUD UI cannot add pending draft: pending draft container is not assigned.", this);
            return;
        }

        if (pendingDraftItemPrefab == null)
        {
            Debug.LogWarning("Battle HUD UI cannot add pending draft: pending draft item prefab is not assigned.", this);
            return;
        }

        GameObject itemObject = Instantiate(pendingDraftItemPrefab, pendingDraftContainer);

        if (!itemObject.TryGetComponent(out PendingDraftUIItem item))
        {
            Debug.LogWarning("Battle HUD UI cannot add pending draft: pending draft item prefab is missing PendingDraftUIItem.", itemObject);
            Destroy(itemObject);
            return;
        }

        item.Initialize(draftResult, towerPlacementController);
        item.BeginBattle();
        pendingDraftItems.Add(item);
    }

    public void RemovePendingTower(PendingDraftUIItem item)
    {
        RemovePendingDraft(item);
    }

    public void RemovePendingDraft(PendingDraftUIItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("Battle HUD UI cannot remove pending draft: item is null.", this);
            return;
        }

        pendingDraftItems.Remove(item);
        Destroy(item.gameObject);
    }

    public bool IsScreenPositionInsideDraftItemInteractionArea(Vector2 screenPosition)
    {
        RectTransform draftItemArea = pendingDraftContainer as RectTransform;

        if (draftItemArea == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(draftItemArea, screenPosition);
    }

    public void OpenDraft(List<DraftResult> draftResults)
    {
        OpenDraft(draftResults, null);
    }

    public void OpenDraft(List<DraftResult> draftResults, Action<DraftResult> onSelected)
    {
        if (!isBattleActive)
        {
            return;
        }

        if (draftUI == null)
        {
            Debug.LogWarning("Battle HUD UI cannot open draft: Draft UI is not assigned.", this);
            return;
        }

        draftUI.OpenDraft(draftResults, onSelected);
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
}
