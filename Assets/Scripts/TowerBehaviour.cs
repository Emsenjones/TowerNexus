using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
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
    [FormerlySerializedAs("fireSpeed")]
    [SerializeField] float attackSpeed;
    [FormerlySerializedAs("firePoint")]
    float timmer;
    [SerializeField] GameObject projectilePrefab;
    Collider2D collider2d;
    void Awake()
    {
        timmer = 0f;

        collider2d = GetComponent<Collider2D>();
        if (collider2d is null)
            Debug.LogError($"{gameObject.name} is missing a Collider2D!");
        else collider2d.enabled = false;
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

        if (monsterDetector == null)
            Debug.LogError($"{gameObject.name} is missing a monsterDetector!");
        else monsterDetector.SetVisible();



    }
    public void Initialize()
    {
        #region Set grids' color to blank.

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

        #endregion
        #region Set collider2d.isTrigger = false.

        if (collider2d is null)
        {
            Debug.LogError($"{gameObject.name} is missing a Collider2D!");
            return;
        }
        collider2d.enabled = true;
        collider2d.isTrigger = false;

        #endregion
        #region Start detecting monsters.

        if (monsterDetector == null)
        {
            Debug.LogError($"{gameObject.name} is missing a monsterDetector!");
            return;
        }
        monsterDetector.Initialize();

        #endregion
        iTower.Initialize();
    }
    void OnMouseDown()
    {
        //Debug.Log($"{gameObject.name} is clicked.");
    }
    [InfoBox("Selecting the type of the tower.")]
    [SerializeReference] ITower iTower;

    void Update()
    {
        if (timmer > 0f)
            timmer -= Time.deltaTime;
        else if (monsterDetector.GetTheNearestMonster() is not null)
        {
            iTower.Attack(monsterDetector.MonsterList);
            timmer = attackSpeed;
        }
    }

}
