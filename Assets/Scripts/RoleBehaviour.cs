using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class RoleBehaviour : MonoBehaviour
{
   [Title("Configs")]
   [SerializeField]float moveSpeed;
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
   int _exp;
   /// <summary>
   /// Get level of this role.
   /// </summary>
   public int GetLevel()
   {
      int exp = _exp;
      foreach (LevelConfig levelConfig in levelConfigList)
      {
         if (exp >= levelConfig.RequiredExp)
            exp -= levelConfig.RequiredExp;
         else
            return levelConfig.Level;
      }
      return levelConfigList[^1].Level;
   }
   /// <summary>
   /// Call this method to get the Level ratio of exp.
   /// </summary>
   /// <returns>LevelRatio, which is between 0 and 1.</returns>
   public float LevelRatio()
   {
      int exp = _exp;
      foreach (LevelConfig levelConfig in levelConfigList)
      {
         if(exp>= levelConfig.RequiredExp)
            exp -= levelConfig.RequiredExp;
         else
         {
            float levelRatio = Mathf.Round(exp) / Mathf.Round(levelConfig.RequiredExp);
            return Mathf.Clamp01(levelRatio);
         }
      }
      Debug.LogError($"{this.gameObject.name} cannot get LevelRatio!");
      return 0f;
   }

   [FormerlySerializedAs("levelInfoList")]
   [SerializeField]
   List<LevelConfig> levelConfigList;
   /// <summary>
   /// Call this method when the role gets exp.
   /// </summary>
   public void GainExp(int gainedExp)
   {
      int level = GetLevel();
      _exp += gainedExp;
      int newLevel = GetLevel();
      if (newLevel> level)
      {
         //触发升级效果。
      }

   }
   Rigidbody2D _rigidbody2D;
   Animator _animator;
   SpriteRenderer _spriteRenderer;
   /// <summary>
   /// Check if the role can attack enemies. 
   /// </summary>
   bool _isMoving;
   public void Initialize()
   {
      _exp = 0;
      _rigidbody2D = GetComponent<Rigidbody2D>();
      _animator = GetComponent<Animator>();
      _spriteRenderer = GetComponent<SpriteRenderer>();
      _isMoving = false;
      _timmer = 0f;
      if (monsterDetectorTransform != null)
      {
         _monsterDetector = monsterDetectorTransform.AddComponent<MonsterDetector>();
         _monsterDetector.Initialize();
      }
      else
         Debug.LogError($"{gameObject.name} is missing a Transform!");
      
   }
   /// <summary>
   /// Moved by the Joystick.
   /// </summary>
   void FixedUpdate()
   {
      if (_rigidbody2D == null)
      {
         Debug.LogError($"{gameObject.name} is missing Rigidbody2D!");
         return;
      }

      Vector2 directionInput = DungeonManager.Instance.GetJoystickInput();
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
         
         MonsterBehaviour nearestMonster = null;
         var minDistance = float.MaxValue;

         foreach (var monster in _monsterList)
         {
            if (monster == null) continue;
            float dist = Vector3.Distance(monster.transform.position, this.transform.position);
            if (dist < minDistance)
            {
               minDistance = dist;
               nearestMonster = monster;
            }
         }

         return nearestMonster;
      }
      
      public void Initialize()
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
         var monster = other.GetComponent<MonsterBehaviour>();
         if (monster != null && !_monsterList.Contains(monster))
         {
            _monsterList.Add(monster);
            Debug.Log($"Find a monster：{monster.name}");
         }
      }

      void OnTriggerExit2D(Collider2D other)
         {
            var monsterBehaviour = other.GetComponent<MonsterBehaviour>();
            if (monsterBehaviour != null && _monsterList.Contains(monsterBehaviour))
            {
               _monsterList.Remove(monsterBehaviour);
               Debug.Log($"A monster has leaved the range：{monsterBehaviour.name}");
            }
         }
      // void OnDrawGizmosSelected()
      // {
      //    if (_detectTrigger is CircleCollider2D circle)
      //    {
      //       // 设置颜色：绿色 + 半透明
      //       Gizmos.color = new Color(1f, 0f, 0f, .5f); // RGBA
      //       Gizmos.DrawWireSphere(circle.bounds.center, circle.radius);
      //    }
      // }
   }

   MonsterDetector _monsterDetector;
   
   [SerializeField]
   Transform monsterDetectorTransform;
   [SerializeField] float fireSpeed = 1f;

   [InfoBox("Choose an attack strategy.")]
   [SerializeReference]
   IAttack iAttack;
   MonsterBehaviour _nearestMonster;
   /// <summary>
   /// Attack monsters.
   /// </summary>
   void Update()
   {
      // To get the nearest enemy.
      _nearestMonster = _monsterDetector.GetNearestMonster();
      
      // To check if RoleBehaviour can attack: 
      // 1. _timmer <= 0f;
      // 2. _isMoving = false;
      // 3. nearestMonster != null;
      bool canAttack = _timmer <= 0f && !_isMoving && _nearestMonster != null;

      if (canAttack)
      {
         _timmer = fireSpeed;

         // Flip the roleBehaviour base on the transform of nearestMonster.
         if (_spriteRenderer == null)
         {
            Debug.LogError($"{gameObject.name} is missing a SpriteRenderer!");
            return;
         }

         float deltaX = _nearestMonster.transform.position.x - transform.position.x;
         if (Mathf.Abs(deltaX) > 0.01f)
            _spriteRenderer.flipX = deltaX < 0;
         

         if (_animator)
            _animator.SetTrigger(AnimatorParams.Attack);
         else Debug.LogError($"{gameObject.name} is missing an Animator!");
         
         // Attack(nearestMonster);
      }
      else _timmer -= Time.deltaTime;
      
   }
   void Attack()
   {
      if (levelConfigList.Count > 0)
      {
         int attackPower = levelConfigList[GetLevel()].AttackPower;
         iAttack.Do(_nearestMonster, attackPower);
      }
      else
         Debug.LogError($"{this.gameObject.name} is missing levelInfoList!");
      
   }
}
