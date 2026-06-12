using UnityEngine;

public class BeamVfxBehaviour : MonoBehaviour
{
    [SerializeField] private Transform startAnchor;
    [SerializeField] private Transform targetAnchor;
    [SerializeField] private LineRenderer lineRenderer;

    private Transform startFollowTarget;
    private Transform endFollowTarget;
    private bool isInitialized;
    private bool isStopping;

    public Transform StartAnchor => startAnchor;
    public Transform TargetAnchor => targetAnchor;
    public LineRenderer LineRenderer => lineRenderer;
    public bool IsInitialized => isInitialized;

    public void Initialize(Transform startFollowTarget, Transform endFollowTarget)
    {
        this.startFollowTarget = startFollowTarget;
        this.endFollowTarget = endFollowTarget;
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
        startFollowTarget = null;
        endFollowTarget = null;
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
        if (startAnchor != null && startFollowTarget != null)
        {
            startAnchor.position = startFollowTarget.position;
            startAnchor.rotation = startFollowTarget.rotation;
        }

        if (targetAnchor != null && endFollowTarget != null)
        {
            targetAnchor.position = endFollowTarget.position;
            targetAnchor.rotation = endFollowTarget.rotation;
        }

        if (lineRenderer == null || !TryGetLinePositions(out Vector3 startPosition, out Vector3 endPosition))
        {
            return;
        }

        if (lineRenderer.positionCount < 2)
        {
            lineRenderer.positionCount = 2;
        }

        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
    }

    private bool TryGetLinePositions(out Vector3 startPosition, out Vector3 endPosition)
    {
        startPosition = Vector3.zero;
        endPosition = Vector3.zero;

        Transform resolvedStart = startAnchor != null ? startAnchor : startFollowTarget;
        Transform resolvedEnd = targetAnchor != null ? targetAnchor : endFollowTarget;

        if (resolvedStart == null || resolvedEnd == null)
        {
            return false;
        }

        startPosition = resolvedStart.position;
        endPosition = resolvedEnd.position;
        return true;
    }
}
