using System;
using UnityEngine;

public class GridBehaviour : MonoBehaviour
{
    [SerializeField] bool isWalkable = true;
    public bool IsWalkable
    {
        get { return isWalkable; }
        set {
            isWalkable = value;
        }
    }
}
