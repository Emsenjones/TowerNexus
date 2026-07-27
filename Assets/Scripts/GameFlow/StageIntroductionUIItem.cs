using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageIntroductionUIItem : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    public bool TryValidateReferences(out string failureReason)
    {
        if (nameText == null)
        {
            failureReason = "Name Text is not assigned.";
            return false;
        }

        if (descriptionText == null)
        {
            failureReason = "Description Text is not assigned.";
            return false;
        }

        if (iconImage == null)
        {
            failureReason = "Icon Image is not assigned.";
            return false;
        }

        failureReason = null;
        return true;
    }

    public bool TryInitialize(
        string displayName,
        string description,
        Sprite icon)
    {
        if (!TryValidateReferences(out string failureReason))
        {
            Debug.LogError(
                $"Stage Introduction item cannot initialize: {failureReason}",
                this);
            return false;
        }

        nameText.text = displayName ?? string.Empty;
        descriptionText.text = description ?? string.Empty;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        return true;
    }
}
