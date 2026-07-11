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
    [SerializeField] private Slider expSlider;
    [SerializeField] private Transform pendingDraftContainer;
    [SerializeField] private GameObject pendingDraftItemPrefab;

    private readonly List<PendingDraftUIItem> pendingDraftItems = new List<PendingDraftUIItem>();

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

    public void UpdateExp(float currentExp, float requiredExp)
    {
        if (expSlider == null)
        {
            Debug.LogWarning("Battle HUD UI cannot update EXP: EXP slider is not assigned.", this);
            return;
        }

        if (requiredExp <= 0f)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
            expSlider.value = 1f;
            return;
        }

        expSlider.minValue = 0f;
        expSlider.maxValue = requiredExp;
        expSlider.value = Mathf.Clamp(currentExp, 0f, requiredExp);
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
        if (draftUI == null)
        {
            Debug.LogWarning("Battle HUD UI cannot open draft: Draft UI is not assigned.", this);
            return;
        }

        draftUI.OpenDraft(draftResults, onSelected);
    }

    private void SubscribeToPlayerSystem()
    {
        if (playerSystem == null)
        {
            return;
        }

        playerSystem.OnLevelChanged += UpdateLevel;
        playerSystem.OnExpChanged += HandleExpChanged;
        playerSystem.OnHealthChanged += UpdateHealth;
    }

    private void UnsubscribeFromPlayerSystem()
    {
        if (playerSystem == null)
        {
            return;
        }

        playerSystem.OnLevelChanged -= UpdateLevel;
        playerSystem.OnExpChanged -= HandleExpChanged;
        playerSystem.OnHealthChanged -= UpdateHealth;
    }

    private void InitializeFromPlayerSystem()
    {
        if (playerSystem == null)
        {
            return;
        }

        UpdateLevel(playerSystem.CurrentLevel);
        UpdateExp(playerSystem.CurrentExp, playerSystem.RequiredExp);
        UpdateHealth(playerSystem.CurrentHealth, playerSystem.MaxHealth);
    }

    private void HandleExpChanged(int currentExp, int requiredExp)
    {
        UpdateExp(currentExp, requiredExp);
    }
}
