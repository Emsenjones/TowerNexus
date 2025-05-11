using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class RoleBehaviour : MonoBehaviour
{
   [Serializable]
   class LevelInfo
   {
      [InfoBox("Required exp to the next level.")]
      [SerializeField]int requiredExp;

      public int RequiredExp
      {
         get { return requiredExp; }
      }

      [SerializeField][InfoBox("Attack power coefficient in this level")]
      float attackPowerCoefficient;

      public float AttackPowerCoefficient
      {
         get { return attackPowerCoefficient; }
      }
   }
   
   [SerializeField]
   List<LevelInfo> levelInfoList;

   int _levelIndex;
   int _exp;
   /// <summary>
   /// Get level of this role.
   /// </summary>
   public int Level
   {
      get { return _levelIndex + 1; }
   }
   /// <summary>
   /// Call this method when the role get exp.
   /// </summary>
   public void GainExp(int gainedExp)
   {
      _exp += gainedExp;
      int tempExp = _exp;
      int levelIndex = 0;

      foreach (var levelInfo in levelInfoList)
      {
         int requiredExp = levelInfo.RequiredExp;
         if (tempExp >= requiredExp)
         {
            tempExp -= requiredExp;
            levelIndex++;
         }
         else
            break;
      }

      if (levelIndex > _levelIndex)
      {
         _levelIndex = levelIndex;
         //
         // 触发升级效果
         //
      }
   }

   /// <summary>
   /// Return exp slider bar value.
   /// </summary>
   /// <returns></returns>
   public float GetExpProgress()
   {
      var expInLevelIndex = _exp;
      foreach (LevelInfo levelInfo in levelInfoList)
      {
         var requiredExp = levelInfo.RequiredExp;
         if (expInLevelIndex >= requiredExp)
            expInLevelIndex -= requiredExp;
         else
            return Mathf.Round(expInLevelIndex)/Mathf.Round(requiredExp);
      }
      Debug.LogWarning("Cannot Get Exp progress");
      return 0;
   }

   [SerializeField] [InfoBox("The move speed of this role.")]
   
   float moveSpeed;

   interface IAttack
   {
      int BasicDamage { get; }

      public void Do(Transform targetTtrTransform)
      {
      }
   }
   [InfoBox("Launch projectiles to attack monsters.")]
   [Serializable] class RangeAttack:IAttack
   {
      [SerializeField]
      int basicDamage;

      public int BasicDamage
      {
         get { return basicDamage; }
      }
   }

   [Serializable][InfoBox("Wave Weapons to attack monster around it.")]
   class MeleeAttack : IAttack
   {
      [SerializeField]
      int basicDamage;
      public int BasicDamage
      {
         get { return basicDamage; }
      }
   }

   [ShowInInspector][SerializeReference]
   IAttack iAttack;

   Rigidbody2D _rigidbody2D;
   Animator _animator;
   SpriteRenderer _spriteRenderer;

   public void Initialize()
   {
      _levelIndex = 0;
      _exp = 0;
      _rigidbody2D = GetComponent<Rigidbody2D>();
      _animator = GetComponent<Animator>();
      _spriteRenderer = GetComponent<SpriteRenderer>();
   }
   void FixedUpdate()
   {
      if (_rigidbody2D == null)
      {
         Debug.LogError($"{gameObject.name} is missing Rigidbody2D!");
         return;
      }

      Vector2 directionInput = DungeonManager.Instance.GetDirectionInput();
      Vector2 moveDelta = directionInput.normalized * moveSpeed * Time.fixedDeltaTime;

      // move roleBehaviour.
      _rigidbody2D.MovePosition(_rigidbody2D.position + moveDelta);

      // Check if role has moved in this frame.
      bool isMoving = moveDelta.sqrMagnitude > 0.001f;

      // 设置动画状态
      if (_animator != null)
         _animator.SetBool(AnimatorParams.IsMoving, isMoving);
      else
         Debug.LogError($"{gameObject.name} is missing Animator!");
      
      // 设置朝向（左右翻转）
      if (_spriteRenderer != null && isMoving)
         _spriteRenderer.flipX = moveDelta.x < 0;
      
      else if (_spriteRenderer == null)
         Debug.LogError($"{gameObject.name} is missing SpriteRenderer!");
   }



   

   void OnTriggerEnter2D(Collider2D other)
   {
      throw new NotImplementedException();
   }


   void Start()
   {
      Initialize();
   }
}
