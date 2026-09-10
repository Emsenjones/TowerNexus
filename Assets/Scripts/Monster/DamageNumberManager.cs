using System.Collections.Generic;
using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    [SerializeField] private DamageNumberUI damageNumberPrefab;

    private readonly List<DamageNumberUI> createdDamageNumbers = new List<DamageNumberUI>();

    private Camera worldCamera;
    private RectTransform itemContainer;

    private void Awake()
    {
        itemContainer = transform as RectTransform;
    }

    private void OnDestroy()
    {
        ClearCreatedDamageNumbers();
    }

    private void OnDisable() => ClearCreatedDamageNumbers();

    public void ShowDamage(int damage, Vector3 worldPosition)
    {
        if (damage <= 0 || !isActiveAndEnabled)
        {
            return;
        }

        if (damageNumberPrefab == null)
        {
            Debug.LogWarning("Damage number manager cannot show damage: no damage number prefab is assigned.", this);
            return;
        }

        if (!TryGetItemContainer(out RectTransform parent))
        {
            return;
        }

        DamageNumberUI damageNumber = Instantiate(damageNumberPrefab, parent);

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

        createdDamageNumbers.Add(damageNumber);
        damageNumberTransform.position = cameraToUse.WorldToScreenPoint(worldPosition);
        damageNumber.Unavailable += HandleDamageNumberComplete;
        if (!damageNumber.TryPlay(damage, HandleDamageNumberComplete, out string failureReason))
        {
            Debug.LogWarning(failureReason, this);
            HandleDamageNumberComplete(damageNumber);
        }
    }

    private bool TryGetItemContainer(out RectTransform parent)
    {
        parent = itemContainer != null ? itemContainer : transform as RectTransform;

        if (parent != null)
        {
            itemContainer = parent;
            return true;
        }

        Debug.LogWarning("Damage number manager must be attached to a RectTransform so it can own its runtime damage-number items.", this);
        return false;
    }

    private Camera ResolveWorldCamera()
    {
        return worldCamera != null ? worldCamera : Camera.main;
    }

    private void HandleDamageNumberComplete(DamageNumberUI damageNumber)
    {
        if (!createdDamageNumbers.Remove(damageNumber)) return;
        ReleaseDamageNumber(damageNumber);
    }

    private void ClearCreatedDamageNumbers()
    {
        while (createdDamageNumbers.Count > 0)
        {
            int index = createdDamageNumbers.Count - 1;
            DamageNumberUI damageNumber = createdDamageNumbers[index];
            createdDamageNumbers.RemoveAt(index);
            ReleaseDamageNumber(damageNumber);
        }
    }

    private void ReleaseDamageNumber(DamageNumberUI damageNumber)
    {
        if (damageNumber == null) return;
        damageNumber.Unavailable -= HandleDamageNumberComplete;
        damageNumber.Cancel();
        damageNumber.gameObject.SetActive(false);
        Destroy(damageNumber.gameObject);
    }
}
