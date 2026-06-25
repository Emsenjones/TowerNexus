using UnityEngine;

public class TowerPlacementPreview : MonoBehaviour
{
    [SerializeField] private Color validPreviewTint = Color.white;
    [SerializeField] private Color invalidPreviewTint = Color.red;
    [SerializeField] private float previewAlpha = 0.5f;

    private TowerBehaviour towerBehaviour;
    private TowerVisualController visualController;
    private TowerInstance towerInstance;
    private TowerDefinition towerDefinition;
    private TowerAnchorSet towerAnchorSet;
    private bool isPlacementValid;
    private bool hasInitializedPreviewVisual;

    public TowerDefinition TowerDefinition => towerDefinition;
    public TowerAnchorSet TowerAnchorSet => towerAnchorSet;
    public TowerVisualController VisualController => visualController;
    public bool IsPlacementValid => isPlacementValid;

    public void Initialize(TowerDefinition towerDefinition)
    {
        this.towerDefinition = towerDefinition;
        hasInitializedPreviewVisual = false;
        towerAnchorSet = GetComponent<TowerAnchorSet>();
        towerInstance = GetComponent<TowerInstance>();
        towerBehaviour = GetComponent<TowerBehaviour>();
        visualController = towerBehaviour != null ? towerBehaviour.VisualController : GetComponent<TowerVisualController>();
        DisablePreviewRendererVisuals();
        DisablePreviewCombatRuntime();

        if (towerDefinition == null)
        {
            Debug.LogWarning("Tower placement preview cannot initialize: tower definition is null.", this);
            SetPlacementState(false);
            return;
        }

        if (towerAnchorSet == null)
        {
            Debug.LogWarning("Tower placement preview cannot initialize: TowerAnchorSet is missing.", this);
            SetPlacementState(false);
            return;
        }

        if (towerAnchorSet.CenterAnchor == null)
        {
            Debug.LogWarning("Tower placement preview cannot initialize: center anchor is missing.", this);
            SetPlacementState(false);
            return;
        }

        EnsurePreviewVisual();
        SetPlacementState(false);
    }

    public void SetWorldPosition(Vector3 position)
    {
        if (towerAnchorSet == null || towerAnchorSet.CenterAnchor == null)
        {
            transform.position = position;
            return;
        }

        Vector3 anchorOffset = towerAnchorSet.CenterAnchor.position - transform.position;
        transform.position = position - anchorOffset;
    }

    public void SetPlacementState(bool isValid)
    {
        isPlacementValid = isValid;
        ApplyPreviewMaterialState(isValid ? validPreviewTint : invalidPreviewTint);
    }

    public bool SetPreviewLevel(int level)
    {
        EnsurePreviewVisual();

        if (towerInstance == null || !towerInstance.TrySetLevel(level))
        {
            Debug.LogWarning($"Tower placement preview cannot set preview level to {level}.", this);
            return false;
        }

        if (towerBehaviour == null || !towerBehaviour.RefreshTowerVisual())
        {
            Debug.LogWarning($"Tower placement preview cannot refresh preview visual for level {level}.", this);
            return false;
        }

        ApplyPreviewMaterialState(isPlacementValid ? validPreviewTint : invalidPreviewTint);
        return true;
    }

    public bool ResetPreviewLevel()
    {
        EnsurePreviewVisual();

        if (towerInstance == null || towerBehaviour == null)
        {
            return false;
        }

        towerInstance.Initialize(towerDefinition, null);

        if (!towerBehaviour.RefreshTowerVisual())
        {
            return false;
        }

        ApplyPreviewMaterialState(isPlacementValid ? validPreviewTint : invalidPreviewTint);
        return true;
    }

    private void EnsurePreviewVisual()
    {
        if (towerInstance == null && !TryGetComponent(out towerInstance))
        {
            towerInstance = gameObject.AddComponent<TowerInstance>();
        }

        if (towerBehaviour == null && !TryGetComponent(out towerBehaviour))
        {
            towerBehaviour = gameObject.AddComponent<TowerBehaviour>();
        }

        towerBehaviour.Initialize(towerInstance);
        visualController = towerBehaviour.VisualController;

        if (hasInitializedPreviewVisual)
        {
            return;
        }

        towerInstance.Initialize(towerDefinition, null);
        towerBehaviour.RefreshTowerVisual();
        hasInitializedPreviewVisual = true;
    }

    private void ApplyPreviewMaterialState(Color tint)
    {
        if (visualController != null)
        {
            visualController.SetPreviewMaterialState(tint, previewAlpha);
        }
        else
        {
            Debug.LogWarning("Tower placement preview cannot apply preview material state: TowerVisualController is missing.", this);
        }
    }

    private void DisablePreviewRendererVisuals()
    {
        Transform compatibilityPreviewRenderer = transform.Find("PreviewRenderer");

        if (compatibilityPreviewRenderer == null)
        {
            return;
        }

        Renderer[] compatibilityRenderers = compatibilityPreviewRenderer.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < compatibilityRenderers.Length; i++)
        {
            Renderer targetRenderer = compatibilityRenderers[i];

            if (targetRenderer != null)
            {
                targetRenderer.enabled = false;
            }
        }
    }

    private void DisablePreviewCombatRuntime()
    {
        TowerCombatBehaviour[] combatBehaviours = GetComponentsInChildren<TowerCombatBehaviour>(true);

        for (int i = 0; i < combatBehaviours.Length; i++)
        {
            TowerCombatBehaviour combatBehaviour = combatBehaviours[i];

            if (combatBehaviour != null)
            {
                combatBehaviour.enabled = false;
            }
        }
    }
}
