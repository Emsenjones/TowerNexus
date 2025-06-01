using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
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

        [FormerlySerializedAs("attackPower")]
        [SerializeField] int damage;
        public int Damage { get { return damage; } }
    }
    int exp;
    /// <summary>
    ///     Get level of this role.
    /// </summary>
    public int GetLevel()
    {
        int tempExp = exp;
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
        int tempExp = exp;
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
        Debug.LogError($"{gameObject.name} cannot get LevelRatio!");
        return 0f;
    }

    [FormerlySerializedAs("levelInfoList")]
    [SerializeField]
    List<LevelConfig> levelConfigList;
    [SerializeField] AnimationEventController animationEventController;
    new Rigidbody2D rigidbody2D;
    Animator animator;
    SpriteRenderer spriteRenderer;
    bool isMoving;
    /// <summary>
    ///     Check if the role can attack enemies.
    /// </summary>
    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody2D = GetComponent<Rigidbody2D>();
    }
    public void Initialize(Transform startTransform)
    {
        exp = 0;
        isMoving = false;
        timmer = 0f;

        transform.position = startTransform.position;

        if (monsterDetector == null)
            Debug.Log($"{gameObject.name} is missing a monsterDetector!");
        else
            monsterDetector.Initialize();

        MonsterBehaviour.OnIsKilled += CheckIfGainExp;

        if (animationEventController == null)
        {
            Debug.LogError($"{gameObject.name} is missing an AnimationEventController!");
            return;
        }
        animationEventController.OnTriggerEvent01 += Attack;
    }
    /// <summary>
    /// Call this method when one monster is dead.
    /// </summary>
    /// <param name="monster">The dead monster.</param>
    /// <param name="isKilledByRole">is killed by the role? </param>
    void CheckIfGainExp(MonsterBehaviour monster, bool isKilledByRole)
    {
        if (!isKilledByRole) return;
        
        exp += monster.Exp;
        OnGetExp?.Invoke(this);
    }
    public static event Action<RoleBehaviour> OnGetExp;
    void OnDisable()
    {
        MonsterBehaviour.OnIsKilled -= CheckIfGainExp;
        if (animationEventController == null)
        {
            Debug.LogError($"{gameObject.name} is missing an AnimationEventController!");
            return;
        }
        animationEventController.OnTriggerEvent01 -= Attack;
    }
    /// <summary>
    ///     Moved by the Joystick.
    /// </summary>
    void FixedUpdate()
    {
        if (rigidbody2D == null)
        {
            Debug.LogError($"{gameObject.name} is missing Rigidbody2D!");
            Debug.Break();
        }

        Vector2 directionInput = DungeonManager.Instance.InputDispatcher.GetJoystickInput();
        Vector2 moveDelta = directionInput.normalized * (moveSpeed * Time.fixedDeltaTime);

        // move roleBehaviour.
        rigidbody2D.MovePosition(rigidbody2D.position + moveDelta);

        // Check if the role has moved in this frame.
        isMoving = moveDelta.sqrMagnitude > 0.001f;

        // Set the animation state.
        if (animator != null)
            animator.SetBool(AnimatorParams.IsMoving, isMoving);
        else
        {
            Debug.LogError($"{gameObject.name} is missing Animator!");
            Debug.Break();
        }

        // Set facing
        if (spriteRenderer != null && isMoving)
            spriteRenderer.flipX = moveDelta.x < 0;

        else if (spriteRenderer == null)
        {
            Debug.LogError($"{gameObject.name} is missing SpriteRenderer!");
            Debug.Break();
        }
    }

    float timmer;


    [SerializeField] MonsterDetector monsterDetector;
    public void IsClicked(bool isDown)
    {
        if (monsterDetector == null)
            Debug.LogError($"{gameObject.name} is missing a monsterDetector!");
        else monsterDetector.SetVisible(isDown);
    }
    [SerializeField] float fireSpeed = 1f;

    [InfoBox("Choose an attack strategy.")]
    [SerializeReference]
    IRoleAttack iRoleAttack;
    /// <summary>
    ///     The method is about detecting monsters.
    /// </summary>
    void Update()
    {
        // To get the nearest enemy.
        if (monsterDetector == null)
        {
            Debug.Log($"{gameObject.name} is missing a monsterDetector!");
            return;
        }
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

            #region Play attack animation.

            if (animator)
                animator.SetTrigger(AnimatorParams.Attack);
            else Debug.LogError($"{gameObject.name} is missing an Animator!");

            #endregion

            // Attack(nearestMonster);
        }
        else timmer -= Time.deltaTime;

    }
    void Attack()
    {
        if (levelConfigList.Count <= 0)
        {
            Debug.LogError($"{gameObject.name} is missing levelInfoList!");
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

        if (monsterDetector == null)
        {
            Debug.Log($"{gameObject.name} is missing a monsterDetector!");
            return;
        }
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
            int damage = levelConfigList[GetLevel()].Damage;
            iRoleAttack.Do(targetMonsterList, damage);
        }

        #endregion

    }


}
