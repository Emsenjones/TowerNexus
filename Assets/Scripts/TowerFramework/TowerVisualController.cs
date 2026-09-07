using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class TowerVisualController : MonoBehaviour
{
    [TitleGroup("Core Visual References")]
    [SerializeField] private Transform visualRoot;
    [TitleGroup("Core Visual References")]
    [SerializeField] private Transform towerBaseVisualRoot;
    [TitleGroup("Core Visual References")]
    [SerializeField] private Transform towerPrefabSpawnPoint;
    [FormerlySerializedAs("attackRangePreviewRoot")]
    [TitleGroup("Core Visual References")]
    [SerializeField] private AttackRangePreviewMotion attackRangePreviewMotion;
    [TitleGroup("Runtime Anchors")]
    [SerializeField] private Transform attackOriginFallback;
    [FormerlySerializedAs("towerDeployRefreshVfxAnchor")]
    [TitleGroup("Runtime Anchors")]
    [SerializeField] private Transform towerSpawnRefreshVfxAnchor;
    [TitleGroup("Runtime Anchors")]
    [SerializeField] private Transform towerUpgradeAppliedVfxAnchor;
    [TitleGroup("Tower Feedback VFX")]
    [SerializeField] private GameObject towerSpawnRefreshVfxPrefab;
    [TitleGroup("Tower Feedback VFX")]
    [FormerlySerializedAs("upgradeAppliedVfxPrefab")]
    [SerializeField] private GameObject towerUpgradeAppliedVfxPrefab;
    [TitleGroup("Valid Upgrade Target Highlight")]
    [SerializeField] private Color validUpgradeTargetHighlightColor = Color.white;
    [TitleGroup("Valid Upgrade Target Highlight")]
    [SerializeField] private float validUpgradeTargetHighlightIntensity = 1.35f;
    [TitleGroup("Valid Upgrade Target Highlight")]
    [FormerlySerializedAs("validUpgradeTargetPulseSpeed")]
    [MinValue(0.01f)]
    [SerializeField] private float validUpgradeTargetPulseDuration = 0.8f;

    private GameObject requestedTowerModelPrefab;
    private GameObject currentTowerModelInstance;
    private Transform currentAttackOrigin;
    private TowerModelPresentation currentTowerModelPresentation;
    private readonly List<PreviewMaterialBinding> previewMaterialBindings = new List<PreviewMaterialBinding>();
    private readonly List<HighlightMaterialBinding> highlightMaterialBindings = new List<HighlightMaterialBinding>();
    private bool hasLoggedMissingRequiredReferences;
    private bool hasLoggedMissingAttackOriginFallback;
    private bool hasLoggedMissingModelAttackOrigin;
    private bool isValidUpgradeTargetHighlighted;

    public Transform VisualRoot => visualRoot;
    public Transform TowerBaseVisualRoot => towerBaseVisualRoot;
    public Transform TowerPrefabSpawnPoint => towerPrefabSpawnPoint;
    public Transform AttackRangePreviewMotion => attackRangePreviewMotion.transform;
    public Transform AttackOriginFallback => attackOriginFallback;
    public Transform TowerSpawnRefreshVfxAnchor => towerSpawnRefreshVfxAnchor;
    public Transform TowerUpgradeAppliedVfxAnchor => towerUpgradeAppliedVfxAnchor;
    public GameObject RequestedTowerModelPrefab => requestedTowerModelPrefab;
    public GameObject CurrentTowerModelInstance => currentTowerModelInstance;
    public TowerModelPresentation CurrentTowerModelPresentation => currentTowerModelPresentation;

    private void Awake()
    {
        CacheDefaultReferences();
        HideAttackRangePreview();
        ValidateRequiredReferences();
    }

    private void Update()
    {
        if (isValidUpgradeTargetHighlighted)
        {
            UpdateValidUpgradeTargetHighlight();
        }
    }

    private void OnDisable()
    {
        ClearValidUpgradeTargetHighlight();
    }

    public Transform GetCurrentAttackOrigin()
    {
        return currentAttackOrigin != null ? currentAttackOrigin : attackOriginFallback;
    }

    public TowerModelPresentation GetCurrentTowerModelPresentation()
    {
        return currentTowerModelPresentation;
    }

    public bool SetTowerVisual(GameObject towerModelPrefab)
    {
        DestroyCurrentTowerModel();
        requestedTowerModelPrefab = towerModelPrefab;

        if (towerModelPrefab == null)
        {
            Debug.LogWarning("Tower visual controller cannot set tower visual: tower model prefab is null.", this);
            ResolveCurrentAttackOrigin();
            return false;
        }

        if (towerPrefabSpawnPoint == null)
        {
            Debug.LogWarning("Tower visual controller cannot set tower visual: TowerPrefabSpawnPoint is missing.", this);
            ResolveCurrentAttackOrigin();
            return false;
        }

        currentTowerModelInstance = Instantiate(towerModelPrefab, towerPrefabSpawnPoint);

        if (currentTowerModelInstance == null)
        {
            Debug.LogWarning(
                "Tower visual controller could not instantiate the requested tower model.",
                this);
            ResolveCurrentAttackOrigin();
            return false;
        }

        currentTowerModelInstance.transform.localPosition = Vector3.zero;
        currentTowerModelInstance.transform.localRotation = Quaternion.identity;
        currentTowerModelInstance.transform.localScale = Vector3.one;
        ResolveCurrentTowerModelPresentation();
        ResolveCurrentAttackOrigin();
        RecachePreviewMaterialBindings();
        return true;
    }

    public void ClearCurrentTowerModel()
    {
        DestroyCurrentTowerModel();
        requestedTowerModelPrefab = null;
    }

    public void SetPreviewMaterialState(Color tint)
    {
        if (previewMaterialBindings.Count == 0)
        {
            RecachePreviewMaterialBindings();
        }

        float clampedAlpha = Mathf.Clamp01(tint.a);

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

        if (attackRangePreviewMotion == null)
        {
            return;
        }

        attackRangePreviewMotion.transform.localScale = Vector3.one * attackRange;
        attackRangePreviewMotion.gameObject.SetActive(true);
    }

    public void HideAttackRangePreview()
    {
        CacheDefaultReferences();

        if (attackRangePreviewMotion != null)
        {
            attackRangePreviewMotion.gameObject.SetActive(false);
        }
    }

    public void UpdateAttackRangePreview(float attackRange)
    {
        ShowAttackRangePreview(attackRange);
    }

    public void SetValidUpgradeTargetHighlight(bool active)
    {
        if (active)
        {
            ShowValidUpgradeTargetHighlight();
        }
        else
        {
            ClearValidUpgradeTargetHighlight();
        }
    }

    public void ClearValidUpgradeTargetHighlight()
    {
        if (highlightMaterialBindings.Count > 0)
        {
            RestoreHighlightMaterialBindings();
        }

        highlightMaterialBindings.Clear();
        isValidUpgradeTargetHighlighted = false;
    }

    public void PlayTowerSpawnRefreshFeedback()
    {
        PlayTowerFeedback(towerSpawnRefreshVfxPrefab, ResolveTowerSpawnRefreshVfxAnchor());
    }

    public void PlayUpgradeAppliedFeedback()
    {
        PlayTowerFeedback(towerUpgradeAppliedVfxPrefab, ResolveTowerUpgradeAppliedVfxAnchor());
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

        if (attackRangePreviewMotion == null)
        {
            attackRangePreviewMotion = transform.Find("AttackRangePreviewMotion").GetComponent<AttackRangePreviewMotion>();
        }

        if (attackOriginFallback == null)
        {
            attackOriginFallback = transform.Find("AttackOriginFallback");
        }

        if (towerSpawnRefreshVfxAnchor == null)
        {
            towerSpawnRefreshVfxAnchor = FindChildByName(transform, "TowerSpawnRefreshVfxAnchor");
        }

        if (towerUpgradeAppliedVfxAnchor == null)
        {
            towerUpgradeAppliedVfxAnchor = FindChildByName(transform, "TowerUpgradeAppliedVfxAnchor");
        }

        if (towerSpawnRefreshVfxAnchor == null)
        {
            towerSpawnRefreshVfxAnchor = FindChildByName(transform, "TowerVfxAnchor");
        }

        if (towerSpawnRefreshVfxAnchor == null)
        {
            towerSpawnRefreshVfxAnchor = FindChildByName(transform, "TowerFeedbackAnchor");
        }
    }

    private void DestroyCurrentTowerModel()
    {
        ClearValidUpgradeTargetHighlight();

        if (currentTowerModelInstance != null)
        {
            Destroy(currentTowerModelInstance);
        }

        currentTowerModelInstance = null;
        currentAttackOrigin = null;
        currentTowerModelPresentation = null;
        previewMaterialBindings.Clear();
        hasLoggedMissingModelAttackOrigin = false;
    }

    private void ShowValidUpgradeTargetHighlight()
    {
        CacheDefaultReferences();

        if (highlightMaterialBindings.Count == 0)
        {
            RecacheHighlightMaterialBindings();
        }

        if (highlightMaterialBindings.Count == 0)
        {
            return;
        }

        isValidUpgradeTargetHighlighted = true;
        UpdateValidUpgradeTargetHighlight();
    }

    private void UpdateValidUpgradeTargetHighlight()
    {
        if (highlightMaterialBindings.Count == 0)
        {
            RecacheHighlightMaterialBindings();
        }

        if (highlightMaterialBindings.Count == 0)
        {
            isValidUpgradeTargetHighlighted = false;
            return;
        }

        float pulse = Mathf.PingPong(Time.time / Mathf.Max(0.01f, validUpgradeTargetPulseDuration), 1f);
        float highlightIntensity = Mathf.Max(0f, validUpgradeTargetHighlightIntensity);

        for (int i = 0; i < highlightMaterialBindings.Count; i++)
        {
            HighlightMaterialBinding binding = highlightMaterialBindings[i];

            if (binding.Renderer == null)
            {
                continue;
            }

            Color targetColor = new Color(
                binding.InitialColor.r * validUpgradeTargetHighlightColor.r * highlightIntensity,
                binding.InitialColor.g * validUpgradeTargetHighlightColor.g * highlightIntensity,
                binding.InitialColor.b * validUpgradeTargetHighlightColor.b * highlightIntensity,
                binding.InitialColor.a
            );

            Color color = Color.Lerp(binding.InitialColor, targetColor, pulse);
            binding.PropertyBlock.SetColor(binding.ColorPropertyName, color);
            binding.Renderer.SetPropertyBlock(binding.PropertyBlock, binding.MaterialIndex);
        }
    }

    private void RecacheHighlightMaterialBindings()
    {
        highlightMaterialBindings.Clear();

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

            Material[] sharedMaterials = targetRenderer.sharedMaterials;

            for (int j = 0; j < sharedMaterials.Length; j++)
            {
                Material material = sharedMaterials[j];

                if (material == null)
                {
                    continue;
                }

                string colorPropertyName = null;

                if (material.HasProperty("_BaseColor"))
                {
                    colorPropertyName = "_BaseColor";
                }
                else if (material.HasProperty("_Color"))
                {
                    colorPropertyName = "_Color";
                }

                if (string.IsNullOrEmpty(colorPropertyName))
                {
                    continue;
                }

                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(propertyBlock, j);
                highlightMaterialBindings.Add(new HighlightMaterialBinding(
                    targetRenderer,
                    j,
                    propertyBlock,
                    colorPropertyName,
                    material.GetColor(colorPropertyName)
                ));
            }
        }
    }

    private void RestoreHighlightMaterialBindings()
    {
        for (int i = 0; i < highlightMaterialBindings.Count; i++)
        {
            HighlightMaterialBinding binding = highlightMaterialBindings[i];

            if (binding.Renderer == null)
            {
                continue;
            }

            binding.PropertyBlock.SetColor(binding.ColorPropertyName, binding.InitialColor);
            binding.Renderer.SetPropertyBlock(binding.PropertyBlock, binding.MaterialIndex);
        }
    }

    private void PlayTowerFeedback(GameObject vfxPrefab, Transform feedbackAnchor)
    {
        if (vfxPrefab == null || feedbackAnchor == null)
        {
            return;
        }

        Instantiate(vfxPrefab, feedbackAnchor.position, feedbackAnchor.rotation);
    }

    private Transform ResolveTowerSpawnRefreshVfxAnchor()
    {
        CacheDefaultReferences();

        if (towerSpawnRefreshVfxAnchor != null)
        {
            return towerSpawnRefreshVfxAnchor;
        }

        if (towerPrefabSpawnPoint != null)
        {
            return towerPrefabSpawnPoint;
        }

        if (visualRoot != null)
        {
            return visualRoot;
        }

        return transform;
    }

    private Transform ResolveTowerUpgradeAppliedVfxAnchor()
    {
        CacheDefaultReferences();

        if (towerUpgradeAppliedVfxAnchor != null)
        {
            return towerUpgradeAppliedVfxAnchor;
        }

        return ResolveTowerSpawnRefreshVfxAnchor();
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

        if (currentTowerModelPresentation != null)
        {
            currentAttackOrigin = currentTowerModelPresentation.AttackOrigin;
        }

        if (currentAttackOrigin == null && currentTowerModelInstance != null)
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

    private void ResolveCurrentTowerModelPresentation()
    {
        currentTowerModelPresentation = null;

        if (currentTowerModelInstance == null)
        {
            return;
        }

        if (!currentTowerModelInstance.TryGetComponent(out currentTowerModelPresentation))
        {
            currentTowerModelPresentation = currentTowerModelInstance.GetComponentInChildren<TowerModelPresentation>(true);
        }

        if (currentTowerModelPresentation != null)
        {
            currentTowerModelPresentation.Initialize(GetComponent<TowerCombatBehaviour>());
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

    private readonly struct HighlightMaterialBinding
    {
        public HighlightMaterialBinding(
            Renderer renderer,
            int materialIndex,
            MaterialPropertyBlock propertyBlock,
            string colorPropertyName,
            Color initialColor)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
            PropertyBlock = propertyBlock;
            ColorPropertyName = colorPropertyName;
            InitialColor = initialColor;
        }

        public Renderer Renderer { get; }
        public int MaterialIndex { get; }
        public MaterialPropertyBlock PropertyBlock { get; }
        public string ColorPropertyName { get; }
        public Color InitialColor { get; }
    }
}
