using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class UiController : MonoBehaviour
{
    [Title("References")]
    [SerializeField] Camera mainCamera;
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
    [Title("Roll Tower Button")]
    [SerializeField] Button summonTowerButton;
    [SerializeField] TextMeshProUGUI summonTowerButtonText;
    [Title("Tower Panel")]
    [SerializeField] RectTransform towerPanel;
    [SerializeField] GameObject towerSlotPrefab;
    [Title("Windows")]
    [SerializeField] List<GameObject> windowPrefabList;

    /// <summary>
    ///     To initialize UiController.
    /// </summary>
    /// <param name="role">The RoleBehaviour.</param>
    /// <param name="coin">The coin amount.</param>
    /// <param name="shred">The shred amount.</param>
    /// <param name="towerPrice">The price to summon a tower.</param>
    public void Initialize(RoleBehaviour role, int coin, int shred, int towerPrice)
    {
        if (expSlider is null)
        {
            Debug.LogError($"{gameObject.name} is missing the ExpSlider.");
            return;
        }
        expSlider.value = role.GetLevelRatio();
        if (levelText is null)
        {
            Debug.LogError($"{gameObject.name} is missing the LevelText.");
            return;
        }
        levelText.text = role.GetLevel().ToString();
        if (coinText == null)
        {
            Debug.LogError($"{gameObject.name} is missing the CoinText.");
            return;
        }

        coinText.text = coin.ToString();
        if (shredText == null)
        {
            Debug.LogError($"{gameObject.name} is missing the ShredText.");
            return;
        }

        shredText.text = shred.ToString();
        if (summonTowerButton == null)
        {
            Debug.LogError($"{gameObject.name} is missing the towerButton.");
            return;
        }
        summonTowerButton.onClick.AddListener(() => OnClickSummonTowerButton?.Invoke());
        if (summonTowerButtonText is null)
        {
            Debug.LogError($"{gameObject.name} is missing the summonTowerButton.");
            return;
        }
        summonTowerButtonText.text = string.Format(GameTexts.SummonOneTower, towerPrice);

        MonsterBehaviour.OnInitialize += GenerateOneHealthBar;
        RoleBehaviour.OnGetExp += OnRoleGetExp;

    }
    public static event Action OnClickSummonTowerButton;
    public void SynchronizeCoin(int coin)
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
        HealthBarBehaviour healthBar = DungeonManager.Instance.RecyclePoolController
            .GenerateOneObject(monsterHealthBarPrefab, monsterHealthBarContainer)
            .GetComponent<HealthBarBehaviour>();
        if (healthBar != null) healthBar.Initialize(monster, mainCamera);
        else
            Debug.LogError($"{healthBar.gameObject.name} is missing the HealthBarBehaviour!");
    }
    public void GenerateOneTowerSlot(GameObject towerPrefab)
    {
        if (towerSlotPrefab == null)
        {
            Debug.LogError(gameObject.name + " is missing the towerSlotPrefab.");
            return;
        }
        if (towerPanel == null)
        {
            Debug.LogError($"{gameObject.name} is missing the towerPanel.");
            return;
        }
        TowerSlotBehaviour towerSlot = DungeonManager.Instance.RecyclePoolController
            .GenerateOneObject(towerSlotPrefab, towerPanel)
            .GetComponent<TowerSlotBehaviour>();

        if (towerSlot is null)
        {
            // ReSharper disable once PossibleNullReferenceException
            Debug.LogError($"{towerSlot.gameObject.name} is missing the TowerSlotBehaviour!");
            return;
        }
        towerSlot.Initialize(towerPrefab, mainCamera);
    }
    [SerializeField] RectTransform windowContainer;
    public void OpenOneWindow<T>(object data = null) where T : WindowBehaviour
    {
        if (windowContainer == null)
        {
            Debug.LogError($"{gameObject.name} is missing the windowContainer.");
            return;
        }
        GameObject windowPrefab = windowPrefabList.FirstOrDefault(go => go.GetComponent<T>() != null);
        if (windowPrefab == null)
        {
            Debug.LogError($"{gameObject.name} is missing {typeof(T)} type of  windowPrefab in the windowPrefabList.");
            return;
        }
        T window = DungeonManager.Instance.RecyclePoolController
            .GenerateOneObject(windowPrefab,windowContainer).GetComponent<T>();
        window.Initialize(data);
    }

    void OnDisable()
    {
        MonsterBehaviour.OnInitialize -= GenerateOneHealthBar;
        RoleBehaviour.OnGetExp -= OnRoleGetExp;
    }

}
