using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerDraftUIItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [Header("Icon Background")]
    [SerializeField] private Image iconBackgroundImage;
    [SerializeField] private Sprite towerDraftIconBackground;
    [SerializeField] private Sprite basicUpgradeIconBackground;
    [SerializeField] private Sprite behaviourUpgradeIconBackground;
    [SerializeField] private Sprite elementalUpgradeIconBackground;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text descriptionText;

    private DraftResult draftResult;
    private Action<DraftResult> onSelected;

    public DraftResult DraftResult => draftResult;

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    public void Initialize(DraftResult draftResult, Action<DraftResult> onSelected)
    {
        this.draftResult = draftResult;
        this.onSelected = onSelected;

        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Draft item UI cannot initialize: draft result is invalid.", this);
            return;
        }

        UpdateIcon(draftResult.Icon);
        UpdateIconBackground(draftResult);
        UpdateText(draftResult);
        UpdateDescription(draftResult);
        BindButton();
    }

    private void UpdateIcon(Sprite icon)
    {
        if (iconImage == null)
        {
            Debug.LogWarning("Tower draft item UI cannot update icon: icon image is not assigned.", this);
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        if (icon == null)
        {
            Debug.LogWarning($"Draft item UI has no icon for '{GetDraftName()}'.", this);
        }
    }

    private void UpdateIconBackground(DraftResult draftResult)
    {
        if (iconBackgroundImage == null)
        {
            Debug.LogWarning("Tower draft item UI cannot update icon background: icon background image is not assigned.", this);
            return;
        }

        Sprite background = GetIconBackground(draftResult);
        iconBackgroundImage.sprite = background;
        iconBackgroundImage.enabled = background != null;

        if (background == null)
        {
            Debug.LogWarning($"Draft item UI has no icon background for '{GetDraftName()}'.", this);
        }
    }

    private Sprite GetIconBackground(DraftResult draftResult)
    {
        switch (draftResult.ResultType)
        {
            case DraftResultType.TowerDraft:
                return towerDraftIconBackground;
            case DraftResultType.TowerUpgradeDraft:
                return GetUpgradeIconBackground(draftResult.TowerUpgradeDefinition);
            default:
                return null;
        }
    }

    private Sprite GetUpgradeIconBackground(TowerUpgradeDefinition upgradeDefinition)
    {
        if (upgradeDefinition == null)
        {
            return null;
        }

        switch (upgradeDefinition.UpgradeLayer)
        {
            case TowerUpgradeLayer.Basic:
                return basicUpgradeIconBackground;
            case TowerUpgradeLayer.Behaviour:
                return behaviourUpgradeIconBackground;
            case TowerUpgradeLayer.Elemental:
                return elementalUpgradeIconBackground;
            default:
                return null;
        }
    }

    private void UpdateText(DraftResult draftResult)
    {
        if (nameText == null)
        {
            Debug.LogWarning("Tower draft item UI cannot update name: name text is not assigned.", this);
        }
        else
        {
            nameText.text = draftResult.DisplayName;
        }
    }

    private void UpdateDescription(DraftResult draftResult)
    {
        if (descriptionText == null)
        {
            Debug.LogWarning("Tower draft item UI cannot update description: description text is not assigned.", this);
        }
        else
        {
            descriptionText.text = draftResult.Description;
        }
    }

    private void BindButton()
    {
        if (button == null)
        {
            Debug.LogWarning("Tower draft item UI cannot bind selection: button is not assigned.", this);
            return;
        }

        button.onClick.RemoveListener(HandleButtonClicked);
        button.onClick.AddListener(HandleButtonClicked);
    }

    private void HandleButtonClicked()
    {
        if (draftResult == null || !draftResult.IsValid)
        {
            Debug.LogWarning("Draft item UI cannot select draft result: draft result is invalid.", this);
            return;
        }

        onSelected?.Invoke(draftResult);
    }

    private string GetDraftName()
    {
        return draftResult != null ? draftResult.DisplayName : string.Empty;
    }
}
