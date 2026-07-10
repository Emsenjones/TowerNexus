using UnityEngine;
using UnityEngine.UI;

public class MonsterStatusUIManager : MonoBehaviour
{
    [SerializeField] private MonsterStatusUIItem statusUIItemPrefab;
    [SerializeField] private RectTransform statusUiContainer;

    private Camera worldCamera;

    private const string RuntimeContainerFallBackName = "MonsterStatusUIFallBackContainer";

    public MonsterStatusUIItem CreateStatusUi(MonsterBehaviour monster, Vector3 offset)
    {
        if (monster == null)
        {
            return null;
        }

        RectTransform parent = ResolveStatusUiContainer();

        if (parent == null)
        {
            Debug.LogWarning("Monster status UI manager cannot create status UI: no UI container is available.", this);
            return null;
        }

        MonsterStatusUIItem statusUIItem = statusUIItemPrefab != null
            ? Instantiate(statusUIItemPrefab, parent)
            : CreateDefaultStatusUi(parent);

        if (statusUIItem == null)
        {
            Debug.LogWarning("Monster status UI manager cannot create status UI: prefab is missing MonsterStatusUIItem.", this);
            return null;
        }

        statusUIItem.Initialize(monster, offset, ResolveWorldCamera());
        return statusUIItem;
    }

    private Camera ResolveWorldCamera()
    {
        return worldCamera != null ? worldCamera : Camera.main;
    }

    private RectTransform ResolveStatusUiContainer()
    {
        if (statusUiContainer != null)
        {
            return statusUiContainer;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return null;
        }

        GameObject containerObject = new GameObject(RuntimeContainerFallBackName, typeof(RectTransform));
        statusUiContainer = containerObject.GetComponent<RectTransform>();
        statusUiContainer.SetParent(canvas.transform, false);
        statusUiContainer.anchorMin = Vector2.zero;
        statusUiContainer.anchorMax = Vector2.one;
        statusUiContainer.offsetMin = Vector2.zero;
        statusUiContainer.offsetMax = Vector2.zero;
        statusUiContainer.pivot = new Vector2(0.5f, 0.5f);
        return statusUiContainer;
    }

    private MonsterStatusUIItem CreateDefaultStatusUi(RectTransform parent)
    {
        GameObject root = new GameObject("MonsterStatusUI", typeof(RectTransform));
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

        MonsterStatusUIItem statusUIItem = root.AddComponent<MonsterStatusUIItem>();
        statusUIItem.SetFillImage(fillImage);
        return statusUIItem;
    }
}
