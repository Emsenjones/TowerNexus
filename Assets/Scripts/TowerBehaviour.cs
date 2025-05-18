using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
public class TowerBehaviour : MonoBehaviour
{
    [Title("Configs")]
    [SerializeField] Sprite iconSprite;
    public Sprite IconSprite
    {
        get {
            return iconSprite;
        }
    }
    [SerializeField] Transform firePoint;
    //[SerializeField] List<Animator> 
    [SerializeField] float fireSpeed;
    [SerializeField] GameObject projectilePrefab;
    [Serializable] class Level
    {
        //[SerializeField]
    }


}
