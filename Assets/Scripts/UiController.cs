using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [Title("Level & Exp Slider")]
    [SerializeField] Slider expSlider;
    [SerializeField] TextMeshProUGUI levelText;
    public void Initialize(RoleBehaviour role)
    {
        OnRoleGetExp(role);
        
        MonsterBehaviour.OnInitialize += GenerateOneHealthBar;
        RoleBehaviour.OnGetExp += OnRoleGetExp;
    }
    void OnRoleGetExp(RoleBehaviour role)
    {
        expSlider.value = role.GetLevelRatio();
        levelText.text = role.GetLevel().ToString();
    }
    void GenerateOneHealthBar(MonsterBehaviour monster)
    {
        HealthBarBehaviour healthBar = DungeonManager.Instance.RecyclePoolController
            .GenerateOneObject(healthBarPrefab, healthBarContainer).GetComponent<HealthBarBehaviour>();
        if (healthBar != null) healthBar.Initialize(monster, mainCamera);
        else
            Debug.LogError($"{healthBar.gameObject.name} is missing the HealthBarBehaviour!");
    }
    void OnDisable()
    {
        MonsterBehaviour.OnInitialize -= GenerateOneHealthBar;
        RoleBehaviour.OnGetExp -= OnRoleGetExp;
    }

}
