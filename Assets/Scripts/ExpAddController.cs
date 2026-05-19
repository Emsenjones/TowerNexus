using System;
using UnityEngine;

public class ExpAddController : MonoBehaviour
{
        [SerializeField] private PlayerLevelSystem playerLevelSystem;
        [SerializeField] private MapGeneratorBehaviour mapGenerator;
        [SerializeField]
        int addedExp;
 
        void Update()
        {
                //Debug.Log("Updating...");
                if (Input.GetKeyDown(KeyCode.Space)&& playerLevelSystem !=null)
                {
                        Debug.Log("Add Exp!");
                        playerLevelSystem.AddExp(addedExp);
                }
        }
}
