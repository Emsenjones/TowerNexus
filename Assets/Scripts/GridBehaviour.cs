using System;
using UnityEngine;

public class GridBehaviour : MonoBehaviour
{
    [SerializeField] bool isWalkable = true;
    bool savedIsWalkable;
    public bool IsWalkable
    {
        get { return isWalkable; }
        set {
            isWalkable = value;
        }
    }
    public void Initialize()
    {
        savedIsWalkable = isWalkable;
    }
    public void Dispose()
    {
        isWalkable = savedIsWalkable;
    }
}
