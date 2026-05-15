using UnityEngine;

public class TowerPlacementPreview : MonoBehaviour
{
    [SerializeField] private Color validColor = new Color(0.35f, 1f, 0.35f, 0.65f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.25f, 0.25f, 0.65f);

    private TowerDefinition towerDefinition;
    private TowerAnchorSet towerAnchorSet;
    private bool isPlacementValid;
    private SpriteRenderer[] spriteRenderers;
    private Renderer[] renderers;

    public TowerDefinition TowerDefinition => towerDefinition;
    public TowerAnchorSet TowerAnchorSet => towerAnchorSet;
    public bool IsPlacementValid => isPlacementValid;

    public void Initialize(TowerDefinition towerDefinition)
    {
        this.towerDefinition = towerDefinition;
        towerAnchorSet = GetComponent<TowerAnchorSet>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        renderers = GetComponentsInChildren<Renderer>(true);

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
        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = color;
                }
            }
        }

        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null || targetRenderer is SpriteRenderer)
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
}
