using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    [Title("Config")]
    [SerializeField] float speed;
    [InfoBox("Selecting the way to launch the projectile.")]
    [SerializeReference] IProjectile iProjectile;
    Collider2D trigger;
    Vector2 targetPosition;
    void Awake()
    {
        trigger = GetComponent<Collider2D>();
        if (trigger == null)
        {
            Debug.LogError($"{gameObject.name} is missing a Collider2D");
            return;
        }
        trigger.enabled = false;
    }
    int damage;
    /// <summary>
    /// Call this method to start this script.
    /// </summary>
    /// <param name="targetTransform"></param>
    public void Initialize(Transform targetTransform, int damage)   
    {
        if (trigger == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing a collider.");
            return;
        }
        trigger.enabled = true;
        trigger.isTrigger = true;
        
        this.damage = damage;
        targetPosition = targetTransform.position;
        
        Rigidbody2D rigidbody2d = GetComponent<Rigidbody2D>();
        if (rigidbody2d == null)
            Debug.LogError($"{gameObject.name} is missing the rigidbody2D");
        else
            iProjectile.Launch(gameObject, speed, targetTransform);
        
        MonsterBehaviour.OnArrivedDestination += DestroyingItself; //To add listener.
    }
    void OnDisable()
    {
        MonsterBehaviour.OnArrivedDestination -= DestroyingItself;
    }
    void DestroyingItself(MonsterBehaviour monster)
    {
        DOTween.Kill(gameObject);
        DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject); 
    }
    [InfoBox("Selecting the effect of hitting monsters.")]
    [SerializeReference] IProjectileEffect iProjectileEffect;
    void OnTriggerEnter2D(Collider2D other)
    {
        MonsterBehaviour monster = other.GetComponent<MonsterBehaviour>();
        if (monster == null||monster.Health<=0) return;
        
        monster.TakeDamage(damage,false);
        iProjectileEffect.Apply(transform);
        DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
    }
    void Update()
    {
        //Destroying itself and apply the effect if the projectile has reached the target.
        float distance = Vector2.Distance(transform.position,targetPosition);
        if (distance <= 0.1f)
        {
            iProjectileEffect.Apply(transform);
            DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject);
        }
    }
}
