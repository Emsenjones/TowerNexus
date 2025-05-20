using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
public class TowerBehaviour : MonoBehaviour
{
    [Title("Configs")]
    [SerializeField] string theName;
    public string TheName
    {
        get {
            return theName;
        }
    }
    [SerializeField] Sprite iconSprite;
    public Sprite IconSprite
    {
        get {
            return iconSprite;
        }
    }
    [SerializeField] List<Transform> gridTransformList;
    public List<Transform> GridTransformList
    {
        get {
            return gridTransformList;
        }
    }
    [SerializeField] Vector4 deployableGridColor;
    [SerializeField] Vector4 notDeployableGridColor;
    [SerializeField] MonsterDetector monsterDetector;
    Collider2D collider2d;
    void Awake()
    {
        collider2d = GetComponent<Collider2D>();
    }

    public void Deploying(bool isDeployable)
    {
        if (gridTransformList.Count <= 0)
        {
            Debug.LogError($"{gameObject.name}'s gridTransformList is empty");
            return;
        }
        Color deployColor = isDeployable ? deployableGridColor : notDeployableGridColor;
        foreach (Transform gridTransform in gridTransformList)
        {
            SpriteRenderer spriteRenderer = gridTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                Debug.LogError($"{gridTransform.name} is missing SpriteRenderer!");
            else
                spriteRenderer.color = deployColor;
        }
        
        if(collider2d == null)
            Debug.LogError($"{gameObject.name} is missing Collider2D!");
        else if(collider2d.enabled)
            collider2d.enabled = false;
    }
    public void Initialize()
    {
        if (gridTransformList.Count <= 0)
        {
            Debug.LogError($"{gameObject.name}'s gridTransformList is empty");
            return;
        }
        foreach (Transform gridTransform in gridTransformList)
        {
            SpriteRenderer spriteRenderer = gridTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                Debug.LogError($"{gridTransform.name} is missing SpriteRenderer!");
            else
                spriteRenderer.color = Vector4.zero;
        }
        
        if(collider2d == null)
            Debug.LogError($"{gameObject.name} is missing Collider2D!");
        else
        {
            collider2d.enabled = true;
            collider2d.isTrigger = true;
        }
            
        
        if (monsterDetector == null)
        {
            Debug.LogError($"{gameObject.name} is missing a monsterDetector!");
            return;
        }
        monsterDetector.Initialize();
    }
    void OnMouseDown()
    {
        Debug.Log($"{gameObject.name} is clicked.");
    }
    [SerializeReference] ITower iTower;

}
