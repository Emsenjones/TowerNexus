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
    [Title("Currency")]
    [SerializeField] TextMeshProUGUI coinText;
    [SerializeField] TextMeshProUGUI shredText;
    [FormerlySerializedAs("healthBarPrefab")]
    [Title("Monster Health bar")]
    [SerializeField] GameObject monsterHealthBarPrefab;
    [FormerlySerializedAs("healthBarContainer")]
    [SerializeField] RectTransform monsterHealthBarContainer;
    [Title("Level & Exp Slider")]
    [SerializeField] Slider expSlider;
    [SerializeField] TextMeshProUGUI levelText;
    [Title("Roll Towers")]
    [SerializeField] Button summonTowerButton;
    [SerializeField] TextMeshProUGUI summonTowerButtonText;
    [Title("TowerSlot")]
    [SerializeField] GameObject towerSlotPrefab;
    /// <summary>
    /// To initialize UiController.
    /// </summary>
    /// <param name="role">The RoleBehaviour.</param>
    /// <param name="coin">The coin amount.</param>
    /// <param name="shred">The shred amount.</param>
    /// <param name="towerPrice">The price to summon a tower.</param>
    public void Initialize(RoleBehaviour role, int coin, int shred, int towerPrice)
    {
        if (expSlider is not null)
            expSlider.value = role.GetLevelRatio();
        else
        {
            Debug.LogError($"{gameObject.name} is missing the ExpSlider.");
            return;
        }
        if (levelText is not null)
            levelText.text = role.GetLevel().ToString();
        else
        {
            Debug.LogError($"{gameObject.name} is missing the LevelText.");
            return;
        }
        if (coinText != null)
            coinText.text = coin.ToString();
        else
        {
            Debug.LogError($"{gameObject.name} is missing the CoinText.");
            return;
        }
        if (shredText != null)
            shredText.text = shred.ToString();
        else
        {
            Debug.LogError($"{gameObject.name} is missing the ShredText.");
            return;
        }
        if (summonTowerButton != null)
        {
            summonTowerButton.onClick.AddListener(()=>OnClickSummonTowerButton?.Invoke());
            //To add the listener.
        }
        else
        {
            Debug.LogError($"{gameObject.name} is missing the towerButton.");
            return;
        }
        if(summonTowerButtonText is not null)
            summonTowerButtonText.text = string.Format(GameTexts.SummonOneTower, towerPrice);
        else
        {
            Debug.LogError($"{gameObject.name} is missing the summonTowerButton.");
        }

        enableJoystick = true;
        MonsterBehaviour.OnInitialize += GenerateOneHealthBar;
        RoleBehaviour.OnGetExp += OnRoleGetExp;

    }
    public static event Action OnClickSummonTowerButton;
    public void UpdateCoin(int coin)
    {
        coinText.text = coin.ToString();
    }
    void OnRoleGetExp(RoleBehaviour role)
    {
        expSlider.value = role.GetLevelRatio();
        levelText.text = role.GetLevel().ToString();
    }
    void GenerateOneHealthBar(MonsterBehaviour monster)
    {
        HealthBarBehaviour healthBar = RecyclePoolController.Instance
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
