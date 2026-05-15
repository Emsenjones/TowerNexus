using System.Collections.Generic;
using UnityEngine;

public class TowerAnchorSet : MonoBehaviour
{
    [SerializeField] private Transform centerAnchor;
    [SerializeField] private List<Transform> occupiedAnchors = new List<Transform>();

    public Transform CenterAnchor => centerAnchor;
    public IReadOnlyList<Transform> OccupiedAnchors => occupiedAnchors;

    public bool IsValid()
    {
        if (centerAnchor == null)
        {
            Debug.LogWarning("Tower anchor set is invalid: center anchor is missing.", this);
            return false;
        }

        if (occupiedAnchors == null || occupiedAnchors.Count == 0)
        {
            Debug.LogWarning("Tower anchor set is invalid: occupied anchors are missing.", this);
            return false;
        }

        for (int i = 0; i < occupiedAnchors.Count; i++)
        {
            if (occupiedAnchors[i] == null)
            {
                Debug.LogWarning($"Tower anchor set is invalid: occupied anchor at index {i} is missing.", this);
                return false;
            }
        }

        return true;
    }

    public IReadOnlyList<Vector3> GetOccupiedAnchorLocalPositions()
    {
        if (!IsValid())
        {
            return System.Array.Empty<Vector3>();
        }

        List<Vector3> localPositions = new List<Vector3>(occupiedAnchors.Count);

        foreach (Transform occupiedAnchor in occupiedAnchors)
        {
            localPositions.Add(occupiedAnchor.localPosition);
        }

        return localPositions;
    }
}
