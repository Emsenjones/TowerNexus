using UnityEngine;
using UnityEngine.UI;

public class MonsterHealthBarManager : MonoBehaviour
{
    [SerializeField] private MonsterHealthBarUI healthBarPrefab;
    [SerializeField] private RectTransform healthBarContainer;
    private Camera worldCamera;

    private const string RuntimeContainerName = "MonsterHealthBarContainer";

    public MonsterHealthBarUI CreateHealthBar(MonsterBehaviour monster, Vector3 offset)
    {
        if (monster == null)
        {
            return null;
        }

        RectTransform parent = ResolveHealthBarContainer();

        if (parent == null)
        {
            Debug.LogWarning("Monster health bar manager cannot create health bar: no UI container is available.", this);
            return null;
        }

        MonsterHealthBarUI healthBar = healthBarPrefab != null
            ? Instantiate(healthBarPrefab, parent)
            : CreateDefaultHealthBar(parent);

        if (healthBar == null)
        {
            Debug.LogWarning("Monster health bar manager cannot create health bar: prefab is missing MonsterHealthBarUI.", this);
            return null;
        }

        healthBar.Initialize(monster, offset, ResolveWorldCamera());
        return healthBar;
    }

    private Camera ResolveWorldCamera()
    {
        return worldCamera != null ? worldCamera : Camera.main;
    }

    private RectTransform ResolveHealthBarContainer()
    {
        if (healthBarContainer != null)
        {
            return healthBarContainer;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return null;
        }

        GameObject containerObject = new GameObject(RuntimeContainerName, typeof(RectTransform));
        healthBarContainer = containerObject.GetComponent<RectTransform>();
        healthBarContainer.SetParent(canvas.transform, false);
        healthBarContainer.anchorMin = Vector2.zero;
        healthBarContainer.anchorMax = Vector2.one;
        healthBarContainer.offsetMin = Vector2.zero;
        healthBarContainer.offsetMax = Vector2.zero;
        healthBarContainer.pivot = new Vector2(0.5f, 0.5f);

        return healthBarContainer;
    }

    private MonsterHealthBarUI CreateDefaultHealthBar(RectTransform parent)
    {
        GameObject root = new GameObject("MonsterHealthBarUI", typeof(RectTransform));
        RectTransform rootTransform = root.GetComponent<RectTransform>();
        rootTransform.SetParent(parent, false);
        rootTransform.pivot = new Vector2(0.5f, 0.5f);

        Image backgroundImage = root.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.65f);
        backgroundImage.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
        RectTransform fillTransform = fillObject.GetComponent<RectTransform>();
        fillTransform.SetParent(rootTransform, false);
        fillTransform.anchorMin = Vector2.zero;
        fillTransform.anchorMax = Vector2.one;
        fillTransform.offsetMin = new Vector2(1f, 1f);
        fillTransform.offsetMax = new Vector2(-1f, -1f);

        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.1f, 0.85f, 0.25f, 1f);
        fillImage.raycastTarget = false;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;

        MonsterHealthBarUI healthBar = root.AddComponent<MonsterHealthBarUI>();
        healthBar.SetFillImage(fillImage);
        return healthBar;
    }
}
