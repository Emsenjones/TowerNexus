using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
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
   /// Call this method when the role gets exp.
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

   Rigidbody2D _rigidbody2D;
   Animator _animator;
   SpriteRenderer _spriteRenderer;
   /// <summary>
   /// Check if the role can attack enemies. 
   /// </summary>
   bool _isMoving;
   public void Initialize()
   {
      _levelIndex = 0;
      _exp = 0;
      _rigidbody2D = GetComponent<Rigidbody2D>();
      _animator = GetComponent<Animator>();
      _spriteRenderer = GetComponent<SpriteRenderer>();
      _isMoving = false;
      _timmer = 0f;
      if (monsterDetectorTransform != null)
      {
         _monsterDetector = monsterDetectorTransform.AddComponent<MonsterDetector>();
         _monsterDetector.Initiate();
      }
     
      else
         Debug.LogError($"{gameObject.name} is missing a Transform!");
      
   }
   /// <summary>
   /// To control  movement of the role.
   /// </summary>
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

      // Check if the role has moved in this frame.
      _isMoving = moveDelta.sqrMagnitude > 0.001f;

      // 设置动画状态
      if (_animator != null)
         _animator.SetBool(AnimatorParams.IsMoving, _isMoving);
      else
         Debug.LogError($"{gameObject.name} is missing Animator!");

      // 设置朝向（左右翻转）
      if (_spriteRenderer != null && _isMoving)
         _spriteRenderer.flipX = moveDelta.x < 0;

      else if (_spriteRenderer == null)
         Debug.LogError($"{gameObject.name} is missing SpriteRenderer!");
   }

   float _timmer;

   [Serializable]
   class MonsterDetector : MonoBehaviour
   {
      List<MonsterBehaviour> _monsterList = new List<MonsterBehaviour>();
      Collider2D _detectTrigger;

      /// <summary>
      /// Get the nearest monster.
      /// </summary>
      public MonsterBehaviour GetNearestMonster()
      {
         _monsterList.RemoveAll(m => m == null);//To clean empty references.
         if (_monsterList.Count <= 0) return null;
         else
         {
            MonsterBehaviour nearestMonster = null;
            var minDistance = float.MaxValue;

            foreach (var monster in _monsterList)
            {
               if (monster == null) continue;
               float dist = Vector3.Distance(monster.transform.position, transform.position);
               if (dist < minDistance)
               {
                  minDistance = dist;
                  nearestMonster = monster;
               }
            }

            return nearestMonster;
         }
      }
      
      public void Initiate()
      {
         _monsterList = new List<MonsterBehaviour>();
         _detectTrigger = GetComponent<Collider2D>();
         if(_detectTrigger != null)
            _detectTrigger.isTrigger = true;
         else
            Debug.LogError($"{gameObject.name} is missing Collider2D!");
         
      }

      void OnTriggerEnter2D(Collider2D other)
      {
         _monsterList.RemoveAll(m => m == null); //To clean empty monsters.
         
         var monster = other.GetComponent<MonsterBehaviour>();
         if (monster != null && !_monsterList.Contains(monster))
         {
            _monsterList.Add(monster);
            Debug.Log($"Find a monster：{monster.name}");
         }
      }

      void OnTriggerExit2D(Collider2D other)
         {
            _monsterList.RemoveAll(m => m == null);//To clean empty monsters.
            
            var monsterBehaviour = other.GetComponent<MonsterBehaviour>();
            if (monsterBehaviour != null && _monsterList.Contains(monsterBehaviour))
            {
               _monsterList.Remove(monsterBehaviour);
               Debug.Log($"A monster has leaved the range：{monsterBehaviour.name}");
            }
         }
      void OnDrawGizmosSelected()
      {
         if (_detectTrigger is CircleCollider2D circle)
         {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(circle.transform.position, circle.radius);
         }
      }
   }

   MonsterDetector _monsterDetector;

   [InfoBox("The child object to detect monsters.")]
   [SerializeField]
   Transform monsterDetectorTransform;

   [SerializeField][InfoBox("The fire speed of the role.")]
   float fireSpeed = 1f;

   [InfoBox("Choose an attack strategy.")]
   [SerializeReference]
   IAttack iAttack;

   void Update()
   {
      // To get the nearest enemy.
      MonsterBehaviour nearestMonster = _monsterDetector.GetNearestMonster();
      
      // To check if RoleBehaviour can attack: 
      // 1. _timmer <= 0f;
      // 2. _isMoving = false;
      // 3. nearestMonster != null;
      bool canAttack = _timmer <= 0f && !_isMoving && nearestMonster != null;

      if (canAttack)
      {
         _timmer = fireSpeed;

         // Flip the roleBehaviour base on the transform of nearestMonster.
         if (_spriteRenderer == null)
         {
            Debug.LogError($"{gameObject.name} is missing a SpriteRenderer!");
            return;
         }

         float deltaX = nearestMonster.transform.position.x - transform.position.x;
         if (Mathf.Abs(deltaX) > 0.01f)
            _spriteRenderer.flipX = deltaX < 0;
         

         if (_animator)
            _animator.SetTrigger(AnimatorParams.Attack);
         else Debug.LogError($"{gameObject.name} is missing an Animator!");
         
         // Attack(nearestMonster);
      }
      else _timmer -= Time.deltaTime;
      
   }
}
