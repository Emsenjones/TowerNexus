using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class MapCameraBoundary : MonoBehaviour
{
    public const string RequiredGameObjectName = "MapCameraBoundary";
    public const string RequiredLayerName = "CameraBoundary";
    public const float PointContainmentTolerance = 0.001f;

    [SerializeField] private BoxCollider boundaryCollider;

    public BoxCollider BoundaryCollider => boundaryCollider;

    private void Reset()
    {
        boundaryCollider = GetComponent<BoxCollider>();
    }

    internal void InitializeAuthoring(BoxCollider authoredCollider)
    {
        boundaryCollider = authoredCollider;
    }

    internal void ValidateLocalStructure(MapValidationResult result)
    {
        Collider[] colliders = GetComponents<Collider>();

        if (colliders.Length != 1)
        {
            result.AddError(
                $"{RequiredGameObjectName} must own exactly one Collider; " +
                $"found {colliders.Length}.");
        }

        if (boundaryCollider == null)
        {
            result.AddError(
                $"{RequiredGameObjectName} BoxCollider reference is not assigned.");
            return;
        }

        if (boundaryCollider.gameObject != gameObject)
        {
            result.AddError(
                $"{RequiredGameObjectName} BoxCollider must be on the same GameObject " +
                "as its adapter.");
            return;
        }

        if (colliders.Length == 1 && colliders[0] != boundaryCollider)
        {
            result.AddError(
                $"{RequiredGameObjectName} sole Collider must be its referenced " +
                "BoxCollider.");
        }

        if (!enabled)
        {
            result.AddError($"{RequiredGameObjectName} adapter must be enabled.");
        }

        if (!gameObject.activeSelf)
        {
            result.AddError($"{RequiredGameObjectName} GameObject must be active.");
        }

        if (!boundaryCollider.enabled)
        {
            result.AddError($"{RequiredGameObjectName} BoxCollider must be enabled.");
        }

        if (!boundaryCollider.isTrigger)
        {
            result.AddError($"{RequiredGameObjectName} BoxCollider must be a trigger.");
        }

        int requiredLayer = LayerMask.NameToLayer(RequiredLayerName);

        if (requiredLayer < 0)
        {
            result.AddError(
                $"Required Layer '{RequiredLayerName}' does not exist.");
        }
        else if (gameObject.layer != requiredLayer)
        {
            result.AddError(
                $"{RequiredGameObjectName} must use Layer '{RequiredLayerName}'.");
        }

        Vector3 size = boundaryCollider.size;
        Vector3 scale = transform.lossyScale;

        if (size.x <= Mathf.Epsilon ||
            size.y <= Mathf.Epsilon ||
            size.z <= Mathf.Epsilon ||
            Mathf.Abs(scale.x) <= Mathf.Epsilon ||
            Mathf.Abs(scale.y) <= Mathf.Epsilon ||
            Mathf.Abs(scale.z) <= Mathf.Epsilon)
        {
            result.AddError(
                $"{RequiredGameObjectName} BoxCollider volume must not be degenerate.");
        }
    }

    public bool ContainsPoint(Vector3 worldPoint)
    {
        if (boundaryCollider == null)
        {
            return false;
        }

        Vector3 closestPoint = boundaryCollider.ClosestPoint(worldPoint);
        return (closestPoint - worldPoint).sqrMagnitude <=
               PointContainmentTolerance * PointContainmentTolerance;
    }
}
