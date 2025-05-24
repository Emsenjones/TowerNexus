using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;

public class MonsterBehaviour : MonoBehaviour
{
    [Title("Configs")]
    [SerializeField] int defaultHealth;
    int maxHealth;
    [SerializeField] float moveSpeed;
    [SerializeField] int exp;
    [SerializeField] int coin;
    public int Exp
    {
        get { return exp; }
    }
    public int Coin
    {
        get { return coin; }
    }
    public int Health { get; private set; }

    BoxCollider2D trigger;
    Animator animator;
    SpriteRenderer spriteRenderer;
    void Awake()
    {
        trigger = GetComponent<BoxCollider2D>();


        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    /// <summary>
    ///     Must call this method to initialize this object.
    /// </summary>
    /// <param name="healthCoefficient">health coefficient parameter.</param>
    public void Initialize(float healthCoefficient)
    {
        if (trigger == null)
        {
            Debug.LogError($"{gameObject.name} is missing BoxCollider2D!");
            return;
        }
        trigger.isTrigger = true;

        maxHealth = Mathf.RoundToInt(healthCoefficient * defaultHealth);
        Health = maxHealth;
        OnInitialize?.Invoke(this);
        FollowOnePath();

        MapController.OnDeployOneTower += FollowOnePath;
        OnArrivedDestination += RecycleAllMonsters;
    }
    void OnDisable()
    {
        MapController.OnDeployOneTower -= FollowOnePath;
        OnArrivedDestination -= RecycleAllMonsters;
    }
    public float HealthRatio
    {
        get {
            return Mathf.Round(Health) / Mathf.Round(maxHealth);
        }
    }

    /// <summary>
    ///     make the monster move to the target node.
    /// </summary>
    void FollowOnePath()
    {
        DOTween.Kill(this); // 自动清理旧 tween
        if (animator != null) animator.SetBool(AnimatorParams.IsMoving, true);
        else Debug.LogError(gameObject.name + " cannot find animator!");

        Sequence seq = DOTween.Sequence().SetId(this); // 加上 ID 更保险

        List<Transform> pathList = DungeonManager.Instance.MapController.FindOnePath(transform);
        if (pathList == null || pathList.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name} cannot find any paths.");
            return;
        }

        Vector3 currentPos = transform.position;
        foreach (Transform node in pathList)
        {
            Vector3 nextPos = node.position;
            float duration = Vector3.Distance(currentPos, nextPos) / moveSpeed;

            // 1. 在移动前插入一个方向判断的回调
            Vector3 dir = nextPos - currentPos;
            seq.AppendCallback(() =>
            {
                if (spriteRenderer != null && dir.x < 0)
                    spriteRenderer.flipX = true; // 向左
                else if (spriteRenderer != null && dir.x > 0)
                    spriteRenderer.flipX = false; // 向右
                // 如果 dir.x == 0，则不翻转
            });

            // 2. 移动 tween
            seq.Append(transform.DOMove(nextPos, duration).SetEase(Ease.Linear));

            currentPos = nextPos; // 更新当前位置
        }

        seq.OnComplete(PathComplete);
    }

    /// <summary>
    ///     will call this method when the monster achieves the target.
    /// </summary>
    void PathComplete()
    {
        if (animator != null) animator.SetBool(AnimatorParams.IsMoving, false);
        else Debug.LogError(gameObject.name + "cannot find animator!");
        OnArrivedDestination?.Invoke(this);
    }


    public static event Action<MonsterBehaviour> OnInitialize;
    public event Action OnIsDamaged;
    [SerializeField] float hitFlashDuration = 0.2f;
    /// <summary>
    ///     Call this method when the monster is attacked.
    /// </summary>
    /// <param name="theDamage">The damage which the monster will be taken.</param>
    /// <param name="isByRole">Is damaged by the Role?</param>
    public void TakeDamage(int theDamage, bool isByRole)
    {
        Health -= theDamage;
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill(); // To kill the color animation.
            spriteRenderer.color = Color.red; // Set its color to red.
            spriteRenderer.DOColor(Color.white, hitFlashDuration); // Then reset to default.
        }
        else
            Debug.LogError($"{gameObject.name} is missing SpriteRenderer!");

        OnIsDamaged?.Invoke();
        if (Health > 0) return;

        DOTween.Kill(this); //To kill its Tween.

        if (animator == null)
        {
            Debug.Log($"{gameObject.name} is missing an animator!");
            return;
        }
        animator.SetTrigger(AnimatorParams.Die);
        OnIsKilled?.Invoke(this, isByRole);
    }
    void RecycleAllMonsters(MonsterBehaviour achievedDestinationMonster)
    {
        RecycleItself();
        OnIsKilled?.Invoke(this,false);
    }

    /// <summary>
    /// It is called by the animation event.
    /// </summary>
    void RecycleItself()
    {
        DOTween.Kill(this);
        DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
    }
    public static event Action<MonsterBehaviour, bool> OnIsKilled;
    public static event Action<MonsterBehaviour> OnArrivedDestination;
}
