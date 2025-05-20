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
    void Awake()
    {
        Collider2D collider2d = GetComponent<Collider2D>();
        if (collider2d == null)
            Debug.LogError($"{gameObject.name} is missing Collider2D");
        else
        {
            collider2d.enabled = !isWalkable;
            collider2d.isTrigger = false;
        }
    }
}
