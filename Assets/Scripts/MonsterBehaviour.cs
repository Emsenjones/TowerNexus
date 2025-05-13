using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using Sequence = DG.Tweening.Sequence;

public class MonsterBehaviour : MonoBehaviour
{
    [Title("Configs")]
    [SerializeField]int defaultHealth;
    int _maxHealth;
    [SerializeField] float moveSpeed;
    [SerializeField]int exp;
    public int Exp
    {
        get { return exp; }
    }

    [SerializeField] int money;
    public int Money
    {
        get { return money; }
    }

    [SerializeField]int damage;
    int _health;
    BoxCollider2D _detectTrigger;
    Animator _animator;
    SpriteRenderer _spriteRenderer;
    /// <summary>
    /// Must call this method when instantiating this object.
    /// </summary>
    /// <param name="healthCoefficient">health coefficient parameter.</param>
    /// <param name="pathTransformList">the path of the monster to the target node.</param>
    public void Initialize(float healthCoefficient, List<Transform> pathTransformList)
    {
        _detectTrigger = GetComponent<BoxCollider2D>();
        if (_detectTrigger == null)
        {
            Debug.LogError($"{this.gameObject.name} is missing BoxCollider2D!");
            return;
        }
        _detectTrigger.isTrigger = true;
        _maxHealth = Mathf.RoundToInt(healthCoefficient*defaultHealth);
        _health = _maxHealth;
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError($"{gameObject.name} is missing animator!");
            return;
        }
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
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
            return Mathf.Round(_health) / Mathf.Round(_maxHealth);
        }
    }

    /// <summary>
    /// make the monster move to the target node.
    /// </summary>
    /// <param name="pathTransformList">the path of the monster to the target node.</param>
    public void FollowPath(List<Transform> pathTransformList)
    {
        DOTween.Kill(this); // 自动清理旧 tween
        if (_animator != null) _animator.SetBool(AnimatorParams.IsMoving,true);
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
                if (_spriteRenderer != null && dir.x < 0)
                    _spriteRenderer.flipX = true;  // 向左
                else if (_spriteRenderer != null && dir.x > 0)
                    _spriteRenderer.flipX = false ; // 向右
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
        if (_animator != null) _animator.SetBool(AnimatorParams.IsMoving,false);
        else Debug.LogError(gameObject.name + "cannot find animator!");
        Debug.Log($"{gameObject.name} has arrived target...");
        OnArrived?.Invoke(this);
    }

    public static event Action<MonsterBehaviour> OnInitialize; 
    public event Action OnIsDamaged;
    /// <summary>
    /// Call this method when the monster is damaged.
    /// </summary>
    /// <param name="attackPower">The damage monster will take.</param>
    /// <param name="isByRole">Is damaged by the Role?</param>
    public void IsDamaged(int attackPower, bool isByRole)
    {
        _health -= attackPower;
        OnIsDamaged?.Invoke();
        if (_health <= 0)
        {
            OnDead?.Invoke(this,isByRole);
            DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
        }
        
    }
    public static event Action<MonsterBehaviour,bool> OnDead;
    public static event Action<MonsterBehaviour> OnArrived;
}
