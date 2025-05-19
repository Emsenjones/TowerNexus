using System;
using UnityEngine;

public class GridBehaviour : MonoBehaviour
{
    [SerializeField] int id;
    public int Id
    {
        get { return id; }
    }

    [SerializeField] bool isWalkable = true;
    public bool IsWalkable
    {
        get { return isWalkable; }
        set {
            isWalkable = value;
        }
    }
}
