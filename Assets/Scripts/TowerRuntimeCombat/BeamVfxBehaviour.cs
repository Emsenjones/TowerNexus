using UnityEngine;

public class BeamVfxBehaviour : MonoBehaviour
{
    [SerializeField] private Transform startVfxRoot;
    [SerializeField] private Transform beamVisualRoot;
    [SerializeField] private Transform hitVfxRoot;
    [SerializeField] private LineRenderer lineRenderer;

    private Transform startAnchor;
    private Transform targetAnchor;
    private bool isInitialized;
    private bool isStopping;

    public Transform StartAnchor => startAnchor;
    public Transform TargetAnchor => targetAnchor;
    public bool IsInitialized => isInitialized;

    public void Initialize(Transform startAnchor, Transform targetAnchor)
    {
        this.startAnchor = startAnchor;
        this.targetAnchor = targetAnchor;
        isInitialized = true;
        isStopping = false;

        if (lineRenderer != null && lineRenderer.positionCount < 2)
        {
            lineRenderer.positionCount = 2;
        }

        UpdateVisuals();
    }

    public void StopAndDestroy()
    {
        if (isStopping)
        {
            return;
        }

        isStopping = true;
        startAnchor = null;
        targetAnchor = null;
        isInitialized = false;
        Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (!isInitialized || isStopping)
        {
            return;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (startAnchor != null)
        {
            if (startVfxRoot != null)
            {
                startVfxRoot.position = startAnchor.position;
            }

            if (beamVisualRoot != null)
            {
                beamVisualRoot.position = startAnchor.position;
            }
        }

        if (targetAnchor != null && hitVfxRoot != null)
        {
            hitVfxRoot.position = targetAnchor.position;
        }

        if (lineRenderer == null || startAnchor == null || targetAnchor == null)
        {
            return;
        }

        if (lineRenderer.positionCount < 2)
        {
            lineRenderer.positionCount = 2;
        }

        lineRenderer.SetPosition(0, startAnchor.position);
        lineRenderer.SetPosition(1, targetAnchor.position);
    }
}
