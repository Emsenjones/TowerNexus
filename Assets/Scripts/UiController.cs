using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class UiController : MonoBehaviour
{
    [Title("References")]
    [SerializeField] Camera mainCamera;
    [SerializeField] Joystick joystick;

    public Vector2 JoystickInput => joystick != null
        ? new Vector2(joystick.Horizontal, joystick.Vertical)
        : Vector2.zero;

    [Title("Health bar")]
    [SerializeField] GameObject healthBarPrefab;
    [SerializeField] RectTransform healthBarContainer;
    void GenerateOneHealthBar(MonsterBehaviour monster)
    {
        HealthBarBehaviour healthBar = DungeonManager.Instance.RecyclePoolController
            .GenerateOneObject(healthBarPrefab, healthBarContainer).GetComponent<HealthBarBehaviour>();
        if (healthBar != null) healthBar.Initialize(monster, mainCamera);
        else
            Debug.LogError($"{healthBar.gameObject.name} is missing the HealthBarBehaviour!");
    }
    void OnEnable()
    {
        MonsterBehaviour.OnInitialize += GenerateOneHealthBar;
    }
    void OnDisable()
    {
        MonsterBehaviour.OnInitialize -= GenerateOneHealthBar;
    }

}
