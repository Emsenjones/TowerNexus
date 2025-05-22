using System;
using UnityEngine;

public interface IProjectileEffect
{
    public void Apply(Transform centerTransform);
}
public class DefaultEffect : IProjectileEffect
{
    /// <summary>
    /// To apply the effect of this projectile.
    /// </summary>
    /// <param name="centerTransform">The current transform of the projectile. </param>
    public void Apply(Transform centerTransform) { }
}
[Serializable] public class ExplosionEffect : IProjectileEffect
{
    [SerializeField] float radius;
    [SerializeField] int explodeDamage;
    [SerializeField] LayerMask monsterLayer;
    [SerializeField] GameObject explosionEffectPrefab;
    /// <summary>
    /// "new Collider2D[20]" can be changed base on the maximum count of monsters.
    /// </summary>
    static readonly Collider2D[] HitBuffer = new Collider2D[20];
    public void Apply(Transform centerTransform)
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(monsterLayer);
        contactFilter.useTriggers = true;

        #region playing an explode effect.

        if (explosionEffectPrefab == null)
        {
            Debug.LogError($"{GetType().Name} is missing an explosionEffectPrefab!");
            return;
        }
        RecyclePoolController recyclePoolController = DungeonManager.Instance.RecyclePoolController;
        GameObject explosionEffect = recyclePoolController.GenerateOneObject(explosionEffectPrefab);
        explosionEffect.transform.position = centerTransform.position;
        var animationEventController = explosionEffect.GetComponent<AnimationEventController>();
        if (animationEventController == null)
        {
            Debug.LogError($"{explosionEffect.name} is missing an animationEventController!");
            return;
        }
        animationEventController.OnTriggerEvent01 += () => 
            recyclePoolController.RecycleOneObject(explosionEffect);

        #endregion

        int hitCount = Physics2D.OverlapCircle(centerTransform.position, radius, contactFilter, HitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            MonsterBehaviour monster = HitBuffer[i].GetComponent<MonsterBehaviour>();
            if (monster != null)
                monster.TakeDamage(explodeDamage, false);
        }
    }

}
public class PoisonEffect { }
public class SlowEffect { }
public class WeakenedEffect { }
