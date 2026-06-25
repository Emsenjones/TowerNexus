using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TowerVisualController : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform towerBaseVisualRoot;
    [FormerlySerializedAs("towerModelSpawnPoint")]
    [SerializeField] private Transform towerPrefabSpawnPoint;
    [SerializeField] private Transform attackRangePreviewRoot;
    [SerializeField] private Transform attackOriginFallback;

    private GameObject requestedTowerModelPrefab;
    private GameObject currentTowerModelInstance;
    private Transform currentAttackOrigin;
    private readonly List<PreviewMaterialBinding> previewMaterialBindings = new List<PreviewMaterialBinding>();
    private bool hasLoggedMissingRequiredReferences;
    private bool hasLoggedMissingAttackOriginFallback;
    private bool hasLoggedMissingModelAttackOrigin;

    public Transform VisualRoot => visualRoot;
    public Transform TowerBaseVisualRoot => towerBaseVisualRoot;
    public Transform TowerPrefabSpawnPoint => towerPrefabSpawnPoint;
    public Transform AttackRangePreviewRoot => attackRangePreviewRoot;
    public Transform AttackOriginFallback => attackOriginFallback;
    public GameObject RequestedTowerModelPrefab => requestedTowerModelPrefab;
    public GameObject CurrentTowerModelInstance => currentTowerModelInstance;

    private void Awake()
    {
        CacheDefaultReferences();
        HideAttackRangePreview();
        ValidateRequiredReferences();
    }

    public Transform GetCurrentAttackOrigin()
    {
        return currentAttackOrigin != null ? currentAttackOrigin : attackOriginFallback;
    }

    public void SetTowerVisual(GameObject towerModelPrefab)
    {
        DestroyCurrentTowerModel();
        requestedTowerModelPrefab = towerModelPrefab;

        if (towerModelPrefab == null)
        {
            Debug.LogWarning("Tower visual controller cannot set tower visual: tower model prefab is null.", this);
            ResolveCurrentAttackOrigin();
            return;
        }

        if (towerPrefabSpawnPoint == null)
        {
            Debug.LogWarning("Tower visual controller cannot set tower visual: TowerPrefabSpawnPoint is missing.", this);
            ResolveCurrentAttackOrigin();
            return;
        }

        currentTowerModelInstance = Instantiate(towerModelPrefab, towerPrefabSpawnPoint);
        currentTowerModelInstance.transform.localPosition = Vector3.zero;
        currentTowerModelInstance.transform.localRotation = Quaternion.identity;
        currentTowerModelInstance.transform.localScale = Vector3.one;
        ResolveCurrentAttackOrigin();
        RecachePreviewMaterialBindings();
    }

    public void ClearCurrentTowerModel()
    {
        DestroyCurrentTowerModel();
        requestedTowerModelPrefab = null;
    }

    public void SetPreviewMaterialState(Color tint, float alpha)
    {
        if (previewMaterialBindings.Count == 0)
        {
            RecachePreviewMaterialBindings();
        }

        float clampedAlpha = Mathf.Clamp01(alpha);

        for (int i = 0; i < previewMaterialBindings.Count; i++)
        {
            PreviewMaterialBinding binding = previewMaterialBindings[i];

            if (binding.Material == null)
            {
                continue;
            }

            Color color = new Color(
                binding.OriginalColor.r * tint.r,
                binding.OriginalColor.g * tint.g,
                binding.OriginalColor.b * tint.b,
                clampedAlpha
            );

            if (binding.UsesBaseColor)
            {
                binding.Material.SetColor("_BaseColor", color);
            }
            else if (binding.UsesColor)
            {
                binding.Material.SetColor("_Color", color);
            }
        }
    }

    public void ClearPreviewMaterialState()
    {
        previewMaterialBindings.Clear();
    }

    public void ShowAttackRangePreview(float attackRange)
    {
        if (attackRange <= 0f)
        {
            HideAttackRangePreview();
            return;
        }

        CacheDefaultReferences();

        if (attackRangePreviewRoot == null)
        {
            return;
        }

        attackRangePreviewRoot.localScale = Vector3.one * attackRange;
        attackRangePreviewRoot.gameObject.SetActive(true);
    }

    public void HideAttackRangePreview()
    {
        CacheDefaultReferences();

        if (attackRangePreviewRoot != null)
        {
            attackRangePreviewRoot.gameObject.SetActive(false);
        }
    }

    public void UpdateAttackRangePreview(float attackRange)
    {
        ShowAttackRangePreview(attackRange);
    }

    private void CacheDefaultReferences()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.Find("VisualRoot");
        }

        if (towerBaseVisualRoot == null && visualRoot != null)
        {
            towerBaseVisualRoot = visualRoot.Find("TowerBaseVisualRoot");
        }

        if (towerPrefabSpawnPoint == null && visualRoot != null)
        {
            towerPrefabSpawnPoint = visualRoot.Find("TowerPrefabSpawnPoint");
        }

        if (attackRangePreviewRoot == null)
        {
            attackRangePreviewRoot = transform.Find("AttackRangePreviewRoot");
        }

        if (attackOriginFallback == null)
        {
            attackOriginFallback = transform.Find("AttackOriginFallback");
        }
    }

    private void DestroyCurrentTowerModel()
    {
        if (currentTowerModelInstance != null)
        {
            Destroy(currentTowerModelInstance);
        }

        currentTowerModelInstance = null;
        currentAttackOrigin = null;
        previewMaterialBindings.Clear();
        hasLoggedMissingModelAttackOrigin = false;
    }

    private void RecachePreviewMaterialBindings()
    {
        previewMaterialBindings.Clear();

        if (visualRoot == null)
        {
            return;
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            Material[] materials = targetRenderer.materials;

            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];

                if (material == null)
                {
                    continue;
                }

                bool usesBaseColor = material.HasProperty("_BaseColor");
                bool usesColor = material.HasProperty("_Color");

                if (!usesBaseColor && !usesColor)
                {
                    continue;
                }

                Color originalColor = usesBaseColor
                    ? material.GetColor("_BaseColor")
                    : material.GetColor("_Color");

                previewMaterialBindings.Add(new PreviewMaterialBinding(
                    material,
                    originalColor,
                    usesBaseColor,
                    usesColor
                ));
            }
        }
    }

    private void ResolveCurrentAttackOrigin()
    {
        currentAttackOrigin = null;

        if (currentTowerModelInstance != null)
        {
            currentAttackOrigin = FindChildByName(currentTowerModelInstance.transform, "AttackOrigin");

            if (currentAttackOrigin == null && !hasLoggedMissingModelAttackOrigin)
            {
                Debug.LogWarning(
                    $"Tower visual controller cannot find AttackOrigin on tower model '{currentTowerModelInstance.name}'. Falling back to AttackOriginFallback.",
                    currentTowerModelInstance
                );
                hasLoggedMissingModelAttackOrigin = true;
            }
        }

        if (currentAttackOrigin == null)
        {
            currentAttackOrigin = attackOriginFallback;
        }

        if (currentAttackOrigin == null && !hasLoggedMissingAttackOriginFallback)
        {
            Debug.LogWarning("Tower visual controller cannot resolve an attack origin: AttackOriginFallback is missing.", this);
            hasLoggedMissingAttackOriginFallback = true;
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child != null && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private void ValidateRequiredReferences()
    {
        if (hasLoggedMissingRequiredReferences)
        {
            return;
        }

        if (visualRoot == null)
        {
            Debug.LogWarning("Tower visual controller is missing VisualRoot.", this);
        }

        if (towerPrefabSpawnPoint == null)
        {
            Debug.LogWarning("Tower visual controller is missing TowerPrefabSpawnPoint.", this);
        }

        if (attackOriginFallback == null)
        {
            Debug.LogWarning("Tower visual controller is missing AttackOriginFallback.", this);
        }

        hasLoggedMissingRequiredReferences = true;
    }

    private readonly struct PreviewMaterialBinding
    {
        public PreviewMaterialBinding(Material material, Color originalColor, bool usesBaseColor, bool usesColor)
        {
            Material = material;
            OriginalColor = originalColor;
            UsesBaseColor = usesBaseColor;
            UsesColor = usesColor;
        }

        public Material Material { get; }
        public Color OriginalColor { get; }
        public bool UsesBaseColor { get; }
        public bool UsesColor { get; }
    }
}
