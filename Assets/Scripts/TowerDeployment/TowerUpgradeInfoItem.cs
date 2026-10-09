using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: no reward, drag or click authority.
public sealed class TowerUpgradeInfoItem : MonoBehaviour
{
    [Header("Content References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text nameText;

    [Header("Upgrade Layer Backgrounds")]
    [SerializeField] private Sprite basicUpgradeIconBackground;
    [SerializeField] private Sprite behaviourUpgradeIconBackground;
    [SerializeField] private Sprite elementalUpgradeIconBackground;

    internal bool TryValidateReferences(out string reason)
    {
        reason = "Upgrade item requires local icon/background/name references and all three layer backgrounds.";
        if (!enabled || iconImage == null || backgroundImage == null || nameText == null ||
            iconImage == backgroundImage || !iconImage.transform.IsChildOf(transform) ||
            !backgroundImage.transform.IsChildOf(transform) || !nameText.transform.IsChildOf(transform) ||
            basicUpgradeIconBackground == null || behaviourUpgradeIconBackground == null ||
            elementalUpgradeIconBackground == null) return false;
        reason = string.Empty;
        return true;
    }

    public void Initialize(string displayName, Sprite icon, TowerUpgradeLayer layer)
    {
        if (!TryInitialize(displayName, icon, layer, out var reason))
            throw new System.InvalidOperationException(reason);
    }

    internal bool TryInitialize(string displayName, Sprite icon, TowerUpgradeLayer layer, out string reason)
    {
        if (!TryValidateReferences(out reason)) return false;
        Sprite background;
        switch (layer)
        {
            case TowerUpgradeLayer.Basic: background = basicUpgradeIconBackground; break;
            case TowerUpgradeLayer.Behaviour: background = behaviourUpgradeIconBackground; break;
            case TowerUpgradeLayer.Elemental: background = elementalUpgradeIconBackground; break;
            default: reason = "Unsupported TowerUpgradeLayer in acquired Upgrade item."; return false;
        }
        nameText.text = displayName ?? string.Empty;
        nameText.raycastTarget = false;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.raycastTarget = false;
        backgroundImage.sprite = background;
        backgroundImage.enabled = true;
        backgroundImage.raycastTarget = false;
        if (icon == null) Debug.LogWarning("Tower upgrade has a missing icon; its slot remains empty.", this);
        return true;
    }
}
