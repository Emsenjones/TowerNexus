using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
public class TowerBehaviour : MonoBehaviour
{
    [FormerlySerializedAs("name")]
    [FormerlySerializedAs("theName")]
    [Title("Configs")]
    [SerializeField] string nameString;
    public string NameString
    {
        get {
            return nameString;
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
        else monsterDetector.SetVisible(true);

    }
    int shard;
    public void Initialize()
    {
        shard = 0;
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

        MonsterBehaviour.OnArrivedDestination += RecycleAllTowers;
    }
    void OnDisable()
    {
        MonsterBehaviour.OnArrivedDestination -= RecycleAllTowers;
    }
    void RecycleAllTowers(MonsterBehaviour achievedDestinationMonster)
    {
        DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
    }

    public void IsClicked(bool isDown)
    {
        #region Show the detecting area of the tower.

        if (monsterDetector == null)
            Debug.LogError($"{gameObject.name} is missing a monsterDetector!");
        else monsterDetector.SetVisible(isDown);

        #endregion

        //Open the upgradeWindow.
        OpenUpgradeWindow();

    }
    void OpenUpgradeWindow()
    {
        LevelConfig levelConfig = GetLevelConfig();
        int level = levelConfig.Level;
        int maxLevel = levelConfigList.Max(lvConfig => lvConfig.Level);
        bool hasReachedMaxLevel = level >= maxLevel;
        LevelConfig nextLevelConfig = levelConfigList.FirstOrDefault(cfg => cfg.Level == level + 1);
        int requiredShard;
        if (nextLevelConfig == null) requiredShard = 0;
        else requiredShard = nextLevelConfig.RequiredShard;

        bool canUpgrade = !hasReachedMaxLevel && shard >= requiredShard;
        string upgradeButtonString = hasReachedMaxLevel ? GameTexts.Max
            : string.Format(GameTexts.UpgradeTower,requiredShard.ToString());
        var data = new TowerWindowBehaviour.Data(
            nameString,
            level.ToString(),
            iconSprite,
            levelConfig.Damage.ToString(),
            canUpgrade,
            upgradeButtonString,
            UpgradeLevel
        );
        DungeonManager.Instance.uiController.OpenOneWindow<TowerWindowBehaviour>(data);
    }
    void UpgradeLevel()
    {
        int level = GetLevelConfig().Level;
        LevelConfig nextLevelConfig = levelConfigList.FirstOrDefault(cfg => cfg.Level == level + 1);
        if (nextLevelConfig == null)
        {
            Debug.Log($"{gameObject.name} has reached the max level.");
        }
        else if (shard < nextLevelConfig.RequiredShard)
            Debug.Log($"Insufficient shard to upgrade the {gameObject.name}");
        else
        {
            shard -= nextLevelConfig.RequiredShard;
            OpenUpgradeWindow();
        }
    }
    [Serializable] class LevelConfig
    {
        [SerializeField] int level;
        public int Level
        {
            get {
                return level;
            }
        }
        [SerializeField] int requiredShard;
        public int RequiredShard
        {
            get {
                return requiredShard;
            }
        }
        [SerializeField] int damage;
        public int Damage
        {
            get {
                return damage;
            }
        }
        [SerializeField] int attackSpeed;
        public int AttackSpeed
        {
            get {
                return attackSpeed;
            }
        }

    }
    [SerializeField] List<LevelConfig> levelConfigList;
    LevelConfig GetLevelConfig()
    {
        int tempShard = shard;
        foreach (LevelConfig levelConfig in levelConfigList)
            if (tempShard >= levelConfig.RequiredShard)
                tempShard -= levelConfig.RequiredShard;
            else return levelConfig;

        return levelConfigList[^1];
    }

    [InfoBox("Selecting the type of the tower.")]
    [SerializeReference] ITower iTower;

    void Update()
    {
        if (timmer > 0f)
            timmer -= Time.deltaTime;
        else if (monsterDetector.GetTheNearestMonster() is not null)
        {
            iTower.Attack(monsterDetector.MonsterList, GetLevelConfig().Damage);
            timmer = attackSpeed;
        }
    }

}
