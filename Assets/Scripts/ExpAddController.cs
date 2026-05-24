using System;
using UnityEngine;

public class ExpAddController : MonoBehaviour
{
        [SerializeField] private PlayerSystem playerSystem;
        [SerializeField]
        int addedExp;
 
        void Update()
        {
                //Debug.Log("Updating...");
                if (Input.GetKeyDown(KeyCode.Space)&& playerSystem !=null)
                {
                        Debug.Log("Add Exp!");
                        playerSystem.AddExp(addedExp);
                }
        }
}
