using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUDUI : MonoBehaviour
{
    [SerializeField] private PlayerLevelSystem playerLevelSystem;
    [SerializeField] private TowerDraftUI towerDraftUI;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Transform pendingTowerContainer;
    [SerializeField] private GameObject pendingTowerItemPrefab;

    private readonly List<PendingTowerItemUI> pendingTowerItems = new List<PendingTowerItemUI>();

    public IReadOnlyList<PendingTowerItemUI> PendingTowerItems => pendingTowerItems;

    private void OnEnable()
    {
        SubscribeToPlayerLevelSystem();
        InitializeFromPlayerLevelSystem();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerLevelSystem();
    }

    public void UpdateLevel(int level)
    {
        if (levelText == null)
        {
            Debug.LogWarning("Battle HUD UI cannot update level: level text is not assigned.", this);
            return;
        }

        levelText.text = $"Level {level}";
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

        item.Initialize(towerDefinition);
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

    private void SubscribeToPlayerLevelSystem()
    {
        if (playerLevelSystem == null)
        {
            return;
        }

        playerLevelSystem.OnLevelChanged += UpdateLevel;
        playerLevelSystem.OnExpChanged += HandleExpChanged;
    }

    private void UnsubscribeFromPlayerLevelSystem()
    {
        if (playerLevelSystem == null)
        {
            return;
        }

        playerLevelSystem.OnLevelChanged -= UpdateLevel;
        playerLevelSystem.OnExpChanged -= HandleExpChanged;
    }

    private void InitializeFromPlayerLevelSystem()
    {
        if (playerLevelSystem == null)
        {
            return;
        }

        UpdateLevel(playerLevelSystem.CurrentLevel);
        UpdateExp(playerLevelSystem.CurrentExp, playerLevelSystem.RequiredExp);
    }

    private void HandleExpChanged(int currentExp, int requiredExp)
    {
        UpdateExp(currentExp, requiredExp);
    }
}
