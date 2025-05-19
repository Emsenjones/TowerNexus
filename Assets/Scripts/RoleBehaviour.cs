using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class RoleBehaviour : MonoBehaviour
{
    [Title("Configs")]
    [SerializeField] float moveSpeed;
    [Serializable]
    class LevelConfig
    {
        [SerializeField]
        int level;
        public int Level
        {
            get {
                return level;
            }
        }
        [SerializeField] int requiredExp;

        public int RequiredExp
        {
            get { return requiredExp; }
        }

        [SerializeField] int attackPower;
        public int AttackPower { get { return attackPower; } }
    }
    int exp;
    /// <summary>
    /// Get level of this role.
    /// </summary>
    public int GetLevel()
    {
        int tempExp = this.exp;
        foreach (LevelConfig levelConfig in levelConfigList)
        {
            if (tempExp > levelConfig.RequiredExp)
                tempExp -= levelConfig.RequiredExp;
            else
                return levelConfig.Level;
        }
        return levelConfigList[^1].Level;
    }
    /// <summary>
    /// Call this method to get the Level ratio of exp.
    /// </summary>
    /// <returns>LevelRatio, which is between 0 and 1.</returns>
    public float GetLevelRatio()
    {
        int tempExp = this.exp;
        foreach (LevelConfig levelConfig in levelConfigList)
        {
            if (tempExp >= levelConfig.RequiredExp)
                tempExp -= levelConfig.RequiredExp;
            else
            {
                float levelRatio = Mathf.Round(tempExp) / Mathf.Round(levelConfig.RequiredExp);
                return Mathf.Clamp01(levelRatio);
            }
        }
        Debug.LogError($"{this.gameObject.name} cannot get LevelRatio!");
        return 0f;
    }

    [FormerlySerializedAs("levelInfoList")]
    [SerializeField]
    List<LevelConfig> levelConfigList;
    new Rigidbody2D rigidbody2D;
    Animator animator;
    SpriteRenderer spriteRenderer;
    /// <summary>
    /// Check if the role can attack enemies. 
    /// </summary>
    bool isMoving;
    public void Initialize()
    {
        exp = 0;
        rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        isMoving = false;
        timmer = 0f;
        #region Add MonsterDetector component.

        if (monsterDetectorTransform != null)
        {
            monsterDetector = monsterDetectorTransform.AddComponent<MonsterDetector>();
            monsterDetector.Initialize();
        }
        else
            Debug.Log($"{gameObject.name} is missing a detector Transform!");

        #endregion
        MonsterBehaviour.OnDead += OnOneMonsterDead;

    }
    /// <summary>
    /// Call this method when one monster is dead.
    /// </summary>
    /// <param name="monster">The dead monster.</param>
    /// <param name="isKilledByRole">is killed by the role? </param>
    void OnOneMonsterDead(MonsterBehaviour monster, bool isKilledByRole)
    {
        if (!isKilledByRole) return;

        // int level = GetLevel();
        exp += monster.Exp;
        // int newLevel = GetLevel();
        // if (newLevel > level)
        // {
        //    
        // }
        OnGetExp?.Invoke(this);
    }
    public static event Action<RoleBehaviour> OnGetExp;
    void OnDisable()
    {
        MonsterBehaviour.OnDead -= OnOneMonsterDead;
    }
    /// <summary>
    /// Moved by the Joystick.
    /// </summary>
    void FixedUpdate()
    {
        if (rigidbody2D == null)
        {
            Debug.LogError($"{gameObject.name} is missing Rigidbody2D!");
            Debug.Break();
        }

        Vector2 directionInput = DungeonManager.Instance.GetJoystickInput();
        Vector2 moveDelta = directionInput.normalized * (moveSpeed * Time.fixedDeltaTime);

        // move roleBehaviour.
        rigidbody2D.MovePosition(rigidbody2D.position + moveDelta);

        // Check if the role has moved in this frame.
        isMoving = moveDelta.sqrMagnitude > 0.001f;

        // 设置动画状态
        if (animator != null)
            animator.SetBool(AnimatorParams.IsMoving, isMoving);
        else
        {
            Debug.LogError($"{gameObject.name} is missing Animator!");
            Debug.Break();}

        // 设置朝向（左右翻转）
        if (spriteRenderer != null && isMoving)
            spriteRenderer.flipX = moveDelta.x < 0;

        else if (spriteRenderer == null)
        {
            Debug.LogError($"{gameObject.name} is missing SpriteRenderer!");
            Debug.Break();
        }
    }

    float timmer;

    [Serializable]
    class MonsterDetector : MonoBehaviour
    {
        List<MonsterBehaviour> monsterList = new List<MonsterBehaviour>();
        public List<MonsterBehaviour> MonsterList
        {
            get { 
                monsterList.RemoveAll(m => m is null|| m.Health<=0);
                return monsterList;
            }
        }
        public MonsterBehaviour GetTheNearestMonster()
        {
            if (MonsterList.Count <= 0) return null;
            
            MonsterBehaviour nearestMonster = null;
            float minDistance = float.MaxValue;

            foreach (MonsterBehaviour monster in MonsterList)
            {
                float dist = Vector3.Distance(monster.transform.position, this.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestMonster = monster;
                }
            }

            return nearestMonster;
        }
        Collider2D detectTrigger;

        public void Initialize()
        {
            monsterList = new List<MonsterBehaviour>();
            detectTrigger = GetComponent<Collider2D>();
            if (detectTrigger != null)
                detectTrigger.isTrigger = true;
            else
                Debug.LogError($"{gameObject.name} is missing Collider2D!");
            

        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var monster = other.GetComponent<MonsterBehaviour>();
            if (monster != null && !monsterList.Contains(monster))
                monsterList.Add(monster);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var monsterBehaviour = other.GetComponent<MonsterBehaviour>();
            if (monsterBehaviour != null && monsterList.Contains(monsterBehaviour))
                monsterList.Remove(monsterBehaviour);
        }
    }

    MonsterDetector monsterDetector;

    [SerializeField]
    Transform monsterDetectorTransform;
    [SerializeField] float fireSpeed = 1f;
    
    [InfoBox("Choose an attack strategy.")]
    [SerializeReference]
    IRoleAttack iRoleAttack;
    /// <summary>
    /// The method is about detecting monsters.
    /// </summary>
    void Update()
    {
        // To get the nearest enemy.
        MonsterBehaviour theNearestMonster = monsterDetector.GetTheNearestMonster();

        // To check if RoleBehaviour can attack: 
        // 1. _timmer <= 0f;
        // 2. _isMoving = false;
        // 3. nearestMonster != null;
        bool canAttack = timmer <= 0f && !isMoving && theNearestMonster is not null;

        if (canAttack)
        {
            timmer = fireSpeed;

            #region Flip the roleBehaviour base on the transform of nearestMonster.

            if (spriteRenderer == null)
            {
                Debug.LogError($"{gameObject.name} is missing a SpriteRenderer!");
                return;
            }

            float deltaX = theNearestMonster.transform.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) > 0.01f)
                spriteRenderer.flipX = deltaX < 0;

            #endregion
            
            #region  Play attack animation.

            if (animator)
                animator.SetTrigger(AnimatorParams.Attack);
            else Debug.LogError($"{gameObject.name} is missing an Animator!");

            #endregion

            // Attack(nearestMonster);
        }
        else timmer -= Time.deltaTime;

    }
    /// <summary>
    /// is Called by the animation event. 
    /// </summary>
    void Attack()
    {
        if (levelConfigList.Count <= 0)
        {
            Debug.LogError($"{this.gameObject.name} is missing levelInfoList!");
            return;
        }
        if (spriteRenderer == null)
        {
            Debug.LogError($"{gameObject.name} is missing a SpriteRenderer!");
            return;
        }

        #region To attack monsters which the role face with in the area.

        var targetMonsterList = new List<MonsterBehaviour>();
        Vector2 forward = spriteRenderer.flipX ? Vector2.left : Vector2.right;

        foreach (MonsterBehaviour monster in monsterDetector.MonsterList)
        {
            if (monster == null || monster.Health <= 0) continue;
            Vector2 toMonster = (monster.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(forward, toMonster);

            if (angle <= 90f) targetMonsterList.Add(monster);
        }
        int level = GetLevel();
        if (level >= 0 && level < levelConfigList.Count)
        {
            int attackPower = levelConfigList[GetLevel()].AttackPower;
            iRoleAttack.Do(targetMonsterList, attackPower);
        }

        #endregion

    }
    

}
