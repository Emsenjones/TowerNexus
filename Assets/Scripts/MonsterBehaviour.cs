using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening;
using Sequence = DG.Tweening.Sequence;

public class MonsterBehaviour : MonoBehaviour
{
    [Title("Configs")]
    [SerializeField] int defaultHealth;
    [SerializeField] int maxHealth;
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

    [SerializeField] int damage;
    public int Damage
    {
        get {
            return damage;
        }
    }
    public int Health { get; private set; }

    BoxCollider2D trigger;
    Animator animator;
    SpriteRenderer spriteRenderer;
    /// <summary>
    /// Must call this method when instantiating this object.
    /// </summary>
    /// <param name="healthCoefficient">health coefficient parameter.</param>
    /// <param name="pathTransformList">the path of the monster to the target node.</param>
    public void Initialize(float healthCoefficient, List<Transform> pathTransformList)
    {
        trigger = GetComponent<BoxCollider2D>();
        if (trigger == null)
        {
            Debug.LogError($"{this.gameObject.name} is missing BoxCollider2D!");
            return;
        }
        trigger.isTrigger = true;

        maxHealth = Mathf.RoundToInt(healthCoefficient * defaultHealth);
        Health = maxHealth;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
        {
            Debug.LogError($"{gameObject.name} is missing animator!");
            return;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"{gameObject.name} is missing spriteRenderer!");
            return;
        }
        OnInitialize?.Invoke(this);
        FollowPath(pathTransformList);
    }

    public float HealthRatio
    {
        get {
            return Mathf.Round(Health) / Mathf.Round(maxHealth);
        }
    }

    /// <summary>
    /// make the monster move to the target node.
    /// </summary>
    /// <param name="pathTransformList">the path of the monster to the target node.</param>
    void FollowPath(List<Transform> pathTransformList)
    {
        DOTween.Kill(this); // 自动清理旧 tween
        if (animator != null) animator.SetBool(AnimatorParams.IsMoving, true);
        else Debug.LogError(gameObject.name + " cannot find animator!");

        Sequence seq = DOTween.Sequence().SetId(this); // 加上 ID 更保险

        if (pathTransformList == null || pathTransformList.Count == 0)
        {
            Debug.LogError($"{gameObject.name} cannot find any paths!");
            return;
        }

        Vector3 currentPos = transform.position;
        foreach (Transform node in pathTransformList)
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
    /// will call this method when the monster achieves the target.
    /// </summary>
    void PathComplete()
    {
        if (animator != null) animator.SetBool(AnimatorParams.IsMoving, false);
        else Debug.LogError(gameObject.name + "cannot find animator!");
        OnArrived?.Invoke(this);
        DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
    }

    public static event Action<MonsterBehaviour> OnInitialize;
    public event Action OnIsDamaged;
    [SerializeField] float hitFlashDuration = 0.2f;
    /// <summary>
    /// Call this method when the monster is attacked.
    /// </summary>
    /// <param name="attackPower">The damage monster will take.</param>
    /// <param name="isByRole">Is damaged by the Role?</param>
    public void TakeDamage(int attackPower, bool isByRole)
    {
        Health -= attackPower;
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill(); // To kill the color animation.
            spriteRenderer.color = Color.red; // Set its color to red.
            spriteRenderer.DOColor(Color.white, hitFlashDuration); // Then reset to default.
        }
        else
            Debug.LogError($"{gameObject.name} is missing SpriteRenderer!");

        OnIsDamaged?.Invoke();
        if (Health <= 0)
        {
            DOTween.Kill(this); //To kill all animations.
            animator.SetTrigger(AnimatorParams.Die);
            OnDead?.Invoke(this, isByRole);
        }

    }
    void DestroyItself() //Called by the animation event.
    {
        DungeonManager.Instance.RecyclePoolController.RecycleOneObject(this.gameObject);
    }
    public static event Action<MonsterBehaviour, bool> OnDead;
    public static event Action<MonsterBehaviour> OnArrived;
}
