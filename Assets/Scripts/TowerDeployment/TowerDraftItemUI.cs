using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerDraftItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
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
