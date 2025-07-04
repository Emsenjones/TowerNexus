using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Title("References")]
    [SerializeField]
    RecyclePoolController recyclePoolController;
    public RecyclePoolController RecyclePoolController
    {
        get {
            if (recyclePoolController == null)
                Debug.LogError($"{gameObject.name} is missing the recyclePoolController!");
            return recyclePoolController;
        }
    }
    void InstantiateRecyclePool()
    {
        if (recyclePoolController == null)
            Debug.LogError($"{gameObject.name} is missing a RecyclePoolController!");

        else
            recyclePoolController.Initialize();
    }
    void DisposeRecyclePool()
    {
        if (recyclePoolController == null)
            Debug.LogError($"{gameObject.name} is missing a RecyclePoolController!");

        else
            recyclePoolController.Dispose();
    }
    [SerializeField]
    MapController mapController;
    public MapController MapController
    {
        get {
            if (mapController == null)
                Debug.LogError($"{gameObject.name} is missing a MapController!");
            return mapController;
        }
    }
    void InstantiateMap()
    {
        if (mapController == null)
        {
            Debug.LogError($"{gameObject.name} is missing mapController..");
            return;
        }
        if (!monsterSpawnConfigList.Any())
        {
            Debug.LogError($"{gameObject.name}'s spawnList is empty!");
            return;
        }
        var spawnGridList = new List<GridBehaviour>();
        foreach (MonsterSpawnConfig spawn in monsterSpawnConfigList)
        {
            if (spawn.Grid is null)
            {
                Debug.LogError($"{gameObject.name}'s spawnList has a null reference!");
                break;
            }
            spawnGridList.Add(spawn.Grid);
        }
        mapController.Initialize(spawnGridList);
    }
    void DisposeMap()
    {
        if (mapController == null)
            Debug.LogError($"{gameObject.name} is missing mapController..");
        else mapController.Dispose();
    }
    
    public UiController uiController;
    void InstantiateUi()
    {
        if (uiController == null)
            Debug.LogError($"{gameObject.name} is missing a uiController..");
        else
            uiController.Initialize(roleBehaviour, coin, shred, towerPrice);
    }

    [SerializeField] InputDispatcher inputDispatcher;
    public InputDispatcher InputDispatcher
    {
        get {
            if (inputDispatcher == null)
                Debug.LogError($"{gameObject.name} is missing a inputDispatcher.");
            return inputDispatcher;
        }
    }
    void InitializeInputDispatcher()
    {
        if (inputDispatcher == null)
            Debug.LogError($"{gameObject.name} is missing a inputDispatcher.");
        else inputDispatcher.Initialize();
    }
    void DisposeInputDispatcher()
    {
        if (inputDispatcher == null)
            Debug.LogError($"{gameObject.name} is missing a inputDispatcher.");
        else inputDispatcher.Dispose();
    }
    [SerializeField] GameObject rolePrefab;
    [SerializeField] GridBehaviour roleSpawnGrid;
    RoleBehaviour roleBehaviour;
    void InstantiateRole()
    {
        roleBehaviour = recyclePoolController.GenerateOneObject(rolePrefab).GetComponent<RoleBehaviour>();
        if (roleBehaviour == null)
        {
            Debug.LogError($"{gameObject.name} is missing roleBehaviour.");
            return;
        }
        Transform roleSpawnTransform = roleSpawnGrid.transform;
        if (roleSpawnTransform == null)
            Debug.LogError($"{gameObject.name} is missing the roleSpawnGrid!");
        else
            roleBehaviour.Initialize(roleSpawnTransform);
    }
    void DisposeRole()
    {
        if (roleBehaviour == null)
            Debug.LogError($"{gameObject.name} is missing roleBehaviour.");
        else
            recyclePoolController.RecycleOneObject(roleBehaviour.gameObject);
    }
    [Title("configs")]
    [Serializable] class MonsterSpawnConfig
    {
        [SerializeField]
        GameObject monsterPrefab;
        public GameObject MonsterPrefab
        {
            get {
                return monsterPrefab;
            }
        }
        [SerializeField] GridBehaviour grid;
        public GridBehaviour Grid
        {
            get {
                return grid;
            }
        }
        [SerializeField][Min(1f)]
        float healthCoefficient = 1f;
        public float HealthCoefficient
        {
            get {
                return healthCoefficient;
            }
        }
        [SerializeField][Min(1)] int count;
        public int Count
        {
            get {
                return count;
            }
        }
        [SerializeField] float delay = 1f;
        public float Delay
        {
            get {
                return delay;
            }
        }
    }
    [SerializeField] List<MonsterSpawnConfig> monsterSpawnConfigList;
    Sequence spawnSequence;
    void InstantiateMonsters()
    {
        if (monsterSpawnConfigList.Count <= 0)
        {
            Debug.LogError($"{gameObject.name} doesn't have any monsters to spawn!");
            return;
        }

        spawnSequence = DOTween.Sequence();

        foreach (MonsterSpawnConfig spawn in monsterSpawnConfigList)
        {
            for (int i = 0; i < spawn.Count; i++)
            {
                // 对每只 Monster 追加间隔
                spawnSequence.AppendInterval(spawn.Delay);

                // 捕获当前循环变量
                MonsterSpawnConfig capturedSpawn = spawn;

                spawnSequence.AppendCallback(() =>
                {
                    if (capturedSpawn.MonsterPrefab == null)
                    {
                        Debug.LogError("A MonsterPrefab is null in spawnList!");
                        return;
                    }

                    MonsterBehaviour monster = recyclePoolController
                        .GenerateOneObject(capturedSpawn.MonsterPrefab)
                        .GetComponent<MonsterBehaviour>();

                    if (monster == null)
                    {
                        Debug.LogError($"{capturedSpawn.MonsterPrefab.name} is missing MonsterBehaviour!");
                        return;
                    }

                    monster.transform.position = capturedSpawn.Grid.transform.position;
                    monster.Initialize(capturedSpawn.HealthCoefficient);
                });
            }
        }

        spawnSequence.Play();
    }
    [SerializeField] int coin;
    [SerializeField] int shred;
    [SerializeField] int towerPrice;
    [Serializable] class TowerPoolItem
    {
        [SerializeField] GameObject towerPrefab;
        public GameObject TowerPrefab
        {
            get {
                return towerPrefab;
            }
        }
        [SerializeField] int weight;
        public int Weight
        {
            get {
                return weight;
            }
        }
    }
    [SerializeField] List<TowerPoolItem> towerPoolItemList;

    void Awake()
    {
        Instance = this;
    }
    bool isGameOver;
    void Start() { GameStart(); }
    /// <summary>
    /// Calling this method to start the game.
    /// </summary>
    void GameStart()
    {
        isGameOver = false;

        InstantiateRecyclePool();
        InstantiateMap();
        InstantiateRole();
        InstantiateMonsters();
        InitializeInputDispatcher();
        InstantiateUi();

        MonsterBehaviour.OnArrivedDestination += GameOver;
        MonsterBehaviour.OnIsKilled += OneMonsterIsKilled;
        UiController.OnClickSummonTowerButton += SummonOneTower;
    }

    void OnDisable()
    {
        MonsterBehaviour.OnArrivedDestination -= GameOver;
        MonsterBehaviour.OnIsKilled -= OneMonsterIsKilled;
        UiController.OnClickSummonTowerButton -= SummonOneTower;
    }
    
    void OneMonsterIsKilled(MonsterBehaviour monster, bool isByRole)
    {
        if (isGameOver) return;

        coin += monster.Coin;
        uiController.SynchronizeCoin(coin);
    }

    void GameOver(MonsterBehaviour arrivedMonster)
    {
        isGameOver = true;

        DisposeInputDispatcher();
        spawnSequence.Kill(); //To stop spawning monsters.
        DisposeRole();
        DisposeMap();
        DisposeRecyclePool();
        
        string gameOverString = GameTexts.GameOver;
        MessageWindowBehaviour.Data messageWindowData
            = new MessageWindowBehaviour.Data(gameOverString,GameStart);
        uiController.OpenOneWindow<MessageWindowBehaviour>(messageWindowData);
    }
    void SummonOneTower()
    {
        if (coin >= towerPrice)
        {
            coin -= towerPrice;
            uiController.SynchronizeCoin(coin);
            GameObject randomTowerPrefab = GetOneTowerPrefab();
            if (randomTowerPrefab == null)
            {
                Debug.LogError($"{gameObject.name} cannot get a random tower!");
                return;
            }
            uiController.GenerateOneTowerSlot(randomTowerPrefab);
        }
        else
            Debug.Log("Not enough Coins!");
    }
    /// <summary>
    /// Get a towerPrefab in the towerPoolItemList.
    /// </summary>
    /// <returns>Randomly return a TowerPrefab via weights.</returns>
    GameObject GetOneTowerPrefab()
    {
        if (towerPoolItemList.Count <= 0)
        {
            Debug.LogError($"{gameObject.name}'s towerPoolItemList is empty!");
            return null;
        }

        int totalWeight = towerPoolItemList.Sum(towerPoolItem => towerPoolItem.Weight);

        int randomValue = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (TowerPoolItem towerPoolItem in towerPoolItemList)
        {
            cumulative += towerPoolItem.Weight;
            if (randomValue < cumulative)
                return towerPoolItem.TowerPrefab;
        }
        Debug.LogError($"{gameObject.name} cannot return a random towerPrefab!");
        return null; // theoretically never hit
    }


}
