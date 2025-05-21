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
    /// <summary>
    /// "new Collider2D[20]" can be changed base on the maximum count of monsters.
    /// </summary>
    static readonly Collider2D[] HitBuffer = new Collider2D[20];
    public void Apply(Transform centerTransform)
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(monsterLayer);
        contactFilter.useTriggers = true;

        int hitCount = Physics2D.OverlapCircle(centerTransform.position, radius, contactFilter, HitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            MonsterBehaviour monster = HitBuffer[i].GetComponent<MonsterBehaviour>();
            if (monster != null)
                monster.TakeDamage(explodeDamage, false);
        }

        // Trigger an explode effect...


    }

}
public class PoisonEffect { }
public class SlowEffect { }
public class WeakenedEffect { }
