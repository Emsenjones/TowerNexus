using System;
using UnityEngine;

public class GridBehaviour : MonoBehaviour
{
    [Serializable] public enum Type {Normal, Spawn, Target};

    [SerializeField]
    Type type;
    public Type ThisType
    {
        get { return type; }
    }

    [SerializeField] bool isWalkable = true;
    public Transform Transform
    {
        get { return transform; }
    }

    public bool IsWalkable
    {
        get { return isWalkable; }
    }
}
