using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class UiController : MonoBehaviour
{
    [Title("References")]
    [SerializeField] Camera mainCamera;
    [SerializeField] Joystick joystick;
    bool enableJoystick;
    void Awake()
    {
        enableJoystick = false;
    }
    public Vector2 JoystickInput
    {
        get {
            if (enableJoystick)
                return joystick is not null
                    ? new Vector2(joystick.Horizontal, joystick.Vertical)
                    : Vector2.zero;
            return Vector2.zero;

        }
    }

    [FormerlySerializedAs("healthBarPrefab")]
    [Title("Monster Health bar")]
    [SerializeField] GameObject monsterHealthBarPrefab;
    [FormerlySerializedAs("healthBarContainer")]
    [SerializeField] RectTransform monsterHealthBarContainer;
    [Title("Level & Exp Slider")]
    [SerializeField] Slider expSlider;
    [SerializeField] TextMeshProUGUI levelText;
    [Title("Health Progress Bar")]
    [SerializeField] Slider healthSlider;
    public void Initialize(RoleBehaviour role)
    {
        if (expSlider is not null)
            expSlider.value = role.GetLevelRatio();
        else
        {
            Debug.LogError($"{gameObject.name} is missing an ExpSlider.");
            return;
        }
        if (levelText is not null)
            levelText.text = role.GetLevel().ToString();
        else
        {
            Debug.LogError($"{gameObject.name} is missing a LevelText.");
            return;
        }
        if (healthSlider != null)
            healthSlider.value = 1f;
        else
        {
            Debug.LogError($"{gameObject.name} is missing a HealthSlider.");
            return;
        }

        enableJoystick = true;
        MonsterBehaviour.OnInitialize += GenerateOneHealthBar;
        RoleBehaviour.OnGetExp += OnRoleGetExp;

    }
    public void SetHealthProgressBar(float value)
    {
        healthSlider.value = value;
    }
    void OnRoleGetExp(RoleBehaviour role)
    {
        expSlider.value = role.GetLevelRatio();
        levelText.text = role.GetLevel().ToString();
    }
    void GenerateOneHealthBar(MonsterBehaviour monster)
    {
        HealthBarBehaviour healthBar = DungeonManager.Instance.RecyclePoolController
            .GenerateOneObject(monsterHealthBarPrefab, monsterHealthBarContainer).GetComponent<HealthBarBehaviour>();
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
