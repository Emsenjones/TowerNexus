using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterBuffStatusIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stackCountText;

    public void Refresh(MonsterBuffStateSnapshot snapshot)
    {
        if (snapshot.Definition == null)
        {
            return;
        }

        bool isProtection = snapshot.Phase == BuffRuntimePhase.Protection;
        Sprite icon = isProtection && snapshot.Definition.ProtectionStatusIcon != null
            ? snapshot.Definition.ProtectionStatusIcon
            : snapshot.Definition.StatusIcon;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = icon != null ? Color.white : Color.clear;
        }

        bool showStackCount = !isProtection && snapshot.StackCount > 1;

        if (stackCountText != null)
        {
            stackCountText.gameObject.SetActive(showStackCount);

            if (showStackCount)
            {
                stackCountText.text = snapshot.StackCount.ToString();
            }
        }
    }
}
