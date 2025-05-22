using System;
using Sirenix.OdinInspector;
using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    [Title("Config")]
    [SerializeField] float speed;
    [SerializeField] int damage;
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
    public void Initialize(Transform targetTransform)
    {
        if (trigger == null)
        {
            Debug.LogWarning($"{gameObject.name} is missing a collider.");
            return;
        }
        trigger.enabled = true;
        trigger.isTrigger = true;
        
        targetPosition = targetTransform.position;
        
        Rigidbody2D rigidbody2d = GetComponent<Rigidbody2D>();
        if (rigidbody2d == null)
            Debug.LogError($"{gameObject.name} is missing the rigidbody2D");
        else
            iProjectile.Launch(gameObject, speed, targetTransform);
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
