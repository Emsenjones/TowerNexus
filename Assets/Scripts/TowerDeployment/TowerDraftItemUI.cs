using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerDraftItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button button;

    private TowerDefinition towerDefinition;
    private Action<TowerDefinition> onSelected;

    public TowerDefinition TowerDefinition => towerDefinition;

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    public void Initialize(TowerDefinition towerDefinition, Action<TowerDefinition> onSelected)
    {
        this.towerDefinition = towerDefinition;
        this.onSelected = onSelected;

        if (towerDefinition == null)
        {
            Debug.LogWarning("Tower draft item UI cannot initialize: tower definition is null.", this);
            return;
        }

        UpdateIcon(towerDefinition.Icon);
        UpdateText(towerDefinition);
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
            Debug.LogWarning($"Tower draft item UI has no icon for tower '{GetTowerName(towerDefinition)}'.", this);
        }
    }

    private void UpdateText(TowerDefinition towerDefinition)
    {
        if (nameText == null)
        {
            Debug.LogWarning("Tower draft item UI cannot update name: name text is not assigned.", this);
        }
        else
        {
            nameText.text = GetTowerName(towerDefinition);
        }

        if (descriptionText == null)
        {
            Debug.LogWarning("Tower draft item UI cannot update description: description text is not assigned.", this);
        }
        else
        {
            descriptionText.text = towerDefinition.Description ?? string.Empty;
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
        if (towerDefinition == null)
        {
            Debug.LogWarning("Tower draft item UI cannot select tower: tower definition is null.", this);
            return;
        }

        onSelected?.Invoke(towerDefinition);
    }

    private static string GetTowerName(TowerDefinition towerDefinition)
    {
        if (towerDefinition == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(towerDefinition.DisplayName))
        {
            return towerDefinition.DisplayName;
        }

        if (!string.IsNullOrEmpty(towerDefinition.TowerId))
        {
            return towerDefinition.TowerId;
        }

        return "Unnamed Tower";
    }
}
