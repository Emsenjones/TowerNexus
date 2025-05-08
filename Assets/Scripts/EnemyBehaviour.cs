using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

public class EnemyBehaviour : MonoBehaviour
{

    [InfoBox("The unite id of Enemy.")] [SerializeField]
    string id;

    public string Id
    {
        get { return id; }
    }

    [SerializeField]int defaultHealth;
    int _maxHealth;
    [FormerlySerializedAs("speed")]
    [InfoBox("The moving speed of the Enemy")] [SerializeField] float moveSpeed;
    [InfoBox("The exp will get when the role kills this Enemy")][SerializeField]int exp;
    public int Exp
    {
        get { return exp; }
    }

    [InfoBox("The money will get when this enemy dead.")][SerializeField]
    int money;
    public int Money
    {
        get { return money; }
    }

    [InfoBox("The dungeon will get this amount of damage.")] [SerializeField]int damage;
    public int Damage
    {
        get { return damage; }
    }
    int _health;
    BoxCollider2D _detectTrigger;
    Animator _animator;
    SpriteRenderer _spriteRenderer;
    static readonly int WalkIndex = Animator.StringToHash("Walk");
    static readonly int DieIndex = Animator.StringToHash("Die");
    public void Initialize(float healthCoefficient, List<Transform> pathTransformList)
    {
        _detectTrigger = GetComponent<BoxCollider2D>();
        if(_detectTrigger) _detectTrigger.isTrigger = true;
        _maxHealth = Mathf.RoundToInt(healthCoefficient*defaultHealth);
        _health = _maxHealth;
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        FollowPath(pathTransformList);
    }

    void FollowPath(List<Transform> pathTransformList)
    {
        DOTween.Kill(this); // 自动清理旧 tween
        if (_animator) _animator.SetTrigger(WalkIndex);

        Sequence seq = DOTween.Sequence().SetId(this); // 加上 ID 更保险

        Vector3 currentPos = transform.position;

        foreach (Transform node in pathTransformList)
        {
            Vector3 nextPos = node.position;
            float duration = Vector3.Distance(currentPos, nextPos) / moveSpeed;

            // 1. 在移动前插入一个方向判断的回调
            Vector3 dir = nextPos - currentPos;
            seq.AppendCallback(() =>
            {
                if (_spriteRenderer && dir.x < 0)
                    _spriteRenderer.flipX = true;  // 向左
                else if (_spriteRenderer && dir.x > 0)
                    _spriteRenderer.flipX = false; // 向右
                // 如果 dir.x == 0，则不翻转
            });

            // 2. 移动 tween
            seq.Append(transform.DOMove(nextPos, duration).SetEase(Ease.Linear));

            currentPos = nextPos; // 更新当前位置
        }

        seq.OnComplete(PathComplete);
    }

    void PathComplete()
    {
        Debug.Log("Path movement complete!");
    }

}
