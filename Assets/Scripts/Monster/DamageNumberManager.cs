using UnityEngine;
using UnityEngine.Serialization;

public class DamageNumberManager : MonoBehaviour
{
    private const string RuntimeContainerName = "DamageNumberContainer";
    
    [SerializeField] private DamageNumberUI damageNumberPrefab;
    [SerializeField] private RectTransform damageNumberContainer;

    private Camera worldCamera;

    public void ShowDamage(int damage, Vector3 worldPosition)
    {
        if (damage <= 0)
        {
            return;
        }

        DamageNumberUI prefab = damageNumberPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("Damage number manager cannot show damage: no damage number prefab is assigned.", this);
            return;
        }

        RectTransform parent = ResolveDamageNumberContainer();

        if (parent == null)
        {
            Debug.LogWarning("Damage number manager cannot show damage: no damageNumberPrefab is available.", this);
            return;
        }

        DamageNumberUI damageNumber = Instantiate(prefab, parent);

        if (damageNumber == null)
        {
            Debug.LogWarning("Damage number manager cannot show damage: prefab is missing DamageNumberUI.", this);
            return;
        }

        RectTransform damageNumberTransform = damageNumber.transform as RectTransform;

        if (damageNumberTransform == null)
        {
            Debug.LogWarning("Damage number manager cannot position damage number: prefab root is not a RectTransform.", damageNumber);
            Destroy(damageNumber.gameObject);
            return;
        }

        Camera cameraToUse = ResolveWorldCamera();

        if (cameraToUse == null)
        {
            Debug.LogWarning("Damage number manager cannot show damage: no world camera is available.", this);
            Destroy(damageNumber.gameObject);
            return;
        }

        damageNumberTransform.position = cameraToUse.WorldToScreenPoint(worldPosition);
        damageNumber.Play(damage, HandleDamageNumberComplete);
    }

    private Camera ResolveWorldCamera()
    {
        return worldCamera != null ? worldCamera : Camera.main;
    }

    private RectTransform ResolveDamageNumberContainer()
    {
        if (damageNumberContainer != null)
        {
            return damageNumberContainer;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return null;
        }

        GameObject containerObject = new GameObject(RuntimeContainerName, typeof(RectTransform));
        damageNumberContainer = containerObject.GetComponent<RectTransform>();
        damageNumberContainer.SetParent(canvas.transform, false);
        damageNumberContainer.anchorMin = Vector2.zero;
        damageNumberContainer.anchorMax = Vector2.one;
        damageNumberContainer.offsetMin = Vector2.zero;
        damageNumberContainer.offsetMax = Vector2.zero;
        damageNumberContainer.pivot = new Vector2(0.5f, 0.5f);

        return damageNumberContainer;
    }

    private void HandleDamageNumberComplete(DamageNumberUI damageNumber)
    {
        if (damageNumber == null)
        {
            return;
        }

        Destroy(damageNumber.gameObject);
    }
}
