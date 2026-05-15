using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PendingTowerItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    private TowerDefinition towerDefinition;

    public TowerDefinition TowerDefinition => towerDefinition;

    public void Initialize(TowerDefinition towerDefinition)
    {
        this.towerDefinition = towerDefinition;

        if (towerDefinition == null)
        {
            Debug.LogWarning("Pending tower item UI cannot initialize: tower definition is null.", this);
            return;
        }

        UpdateIcon(towerDefinition.Icon);
        UpdateName(towerDefinition);
    }

    private void UpdateIcon(Sprite icon)
    {
        if (iconImage == null)
        {
            Debug.LogWarning("Pending tower item UI cannot update icon: icon image is not assigned.", this);
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        if (icon == null)
        {
            Debug.LogWarning($"Pending tower item UI has no icon for tower '{GetTowerName(towerDefinition)}'.", this);
        }
    }

    private void UpdateName(TowerDefinition towerDefinition)
    {
        if (nameText == null)
        {
            Debug.LogWarning("Pending tower item UI cannot update name: name text is not assigned.", this);
            return;
        }

        nameText.text = GetTowerName(towerDefinition);
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
