using UnityEngine;

public class TowerPlacementPreview : MonoBehaviour
{
    [SerializeField] private Color validColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private Transform previewRendererRoot;

    private Renderer[] previewRenderers;
    private TowerDefinition towerDefinition;
    private TowerAnchorSet towerAnchorSet;
    private bool isPlacementValid;

    public TowerDefinition TowerDefinition => towerDefinition;
    public TowerAnchorSet TowerAnchorSet => towerAnchorSet;
    public bool IsPlacementValid => isPlacementValid;

    public void Initialize(TowerDefinition towerDefinition)
    {
        this.towerDefinition = towerDefinition;
        towerAnchorSet = GetComponent<TowerAnchorSet>();
        CachePreviewRenderers();

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
        ApplyPreviewColor(isValid ? validColor : invalidColor);
    }

    private void ApplyPreviewColor(Color color)
    {
        if (previewRenderers == null || previewRenderers.Length == 0)
        {
            Debug.Log("Tower placement preview renderers are null or empty.", this);
            return;
        }

        for (int i = 0; i < previewRenderers.Length; i++)
        {
            Renderer targetRenderer = previewRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            Material material = targetRenderer.material;

            if (material == null)
            {
                continue;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }

    private void CachePreviewRenderers()
    {
        if (previewRendererRoot == null)
        {
            Debug.LogWarning("Tower placement preview renderer root is null.", this);
            previewRenderers = null;
            return;
        }

        previewRenderers = previewRendererRoot.GetComponentsInChildren<Renderer>(true);
    }
}
