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
    [SerializeField]  List<Transform> gridTransformList;
    [SerializeField] Vector4 deployableGridColor;
    [SerializeField] Vector4 notDeployableGridColor;
    
    public List<Transform> GridTransformList
    {
        get {
            return gridTransformList;
        }
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
    }
    public void Initialize()
    {
        foreach (Transform gridTransform in gridTransformList)
        {
            SpriteRenderer spriteRenderer = gridTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                Debug.LogError($"{gridTransform.name} is missing SpriteRenderer!");
            else
                spriteRenderer.color = Vector4.zero;
        }
    }
    void OnMouseDown()
    {
        Debug.Log($"{gameObject.name} is clicked.");
    }
    [SerializeReference] ITower iTower;
    
}
