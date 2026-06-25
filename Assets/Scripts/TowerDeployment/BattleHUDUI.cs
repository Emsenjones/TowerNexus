using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUDUI : MonoBehaviour
{
    [SerializeField] private PlayerSystem playerSystem;
    [SerializeField] private TowerDraftUI towerDraftUI;
    [SerializeField] private TowerPlacementController towerPlacementController;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Transform pendingTowerContainer;
    [SerializeField] private GameObject pendingTowerItemPrefab;

    private readonly List<PendingTowerItemUI> pendingTowerItems = new List<PendingTowerItemUI>();

    public IReadOnlyList<PendingTowerItemUI> PendingTowerItems => pendingTowerItems;

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
        if (towerDefinition == null)
        {
            Debug.LogWarning("Battle HUD UI cannot add pending tower: tower definition is null.", this);
            return;
        }

        if (pendingTowerContainer == null)
        {
            Debug.LogWarning("Battle HUD UI cannot add pending tower: pending tower container is not assigned.", this);
            return;
        }

        if (pendingTowerItemPrefab == null)
        {
            Debug.LogWarning("Battle HUD UI cannot add pending tower: pending tower item prefab is not assigned.", this);
            return;
        }

        GameObject itemObject = Instantiate(pendingTowerItemPrefab, pendingTowerContainer);

        if (!itemObject.TryGetComponent(out PendingTowerItemUI item))
        {
            Debug.LogWarning("Battle HUD UI cannot add pending tower: pending tower item prefab is missing PendingTowerItemUI.", itemObject);
            Destroy(itemObject);
            return;
        }

        item.Initialize(towerDefinition, towerPlacementController);
        pendingTowerItems.Add(item);
    }

    public void RemovePendingTower(PendingTowerItemUI item)
    {
        if (item == null)
        {
            Debug.LogWarning("Battle HUD UI cannot remove pending tower: item is null.", this);
            return;
        }

        pendingTowerItems.Remove(item);
        Destroy(item.gameObject);
    }

    public bool IsScreenPositionInsideDraftItemInteractionArea(Vector2 screenPosition)
    {
        RectTransform draftItemArea = pendingTowerContainer as RectTransform;

        if (draftItemArea == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(draftItemArea, screenPosition);
    }

    public void OpenDraft(List<TowerDefinition> towerDefinitions)
    {
        OpenDraft(towerDefinitions, null);
    }

    public void OpenDraft(List<TowerDefinition> towerDefinitions, Action<TowerDefinition> onSelected)
    {
        if (towerDraftUI == null)
        {
            Debug.LogWarning("Battle HUD UI cannot open draft: tower draft UI is not assigned.", this);
            return;
        }

        towerDraftUI.OpenDraft(towerDefinitions, onSelected);
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
