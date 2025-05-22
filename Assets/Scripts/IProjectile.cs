using System;
using DG.Tweening;
using UnityEngine;

public interface IProjectile
{
    /// <summary>
    /// To launch the projectile.
    /// </summary>
    /// <param name="projectile">The projectile.</param>
    /// <param name="speed">The speed of the projectile. </param>
    /// <param name="targetTransform">The target transform.</param>
    public void Launch(GameObject projectile, float speed, Transform targetTransform);
    //如果是抛射的话，通过rigidbody2d找到collider2d然后把collider2.enable = false。
}
/// <summary>
/// To launch projectiles straightly.
/// </summary>
public class StraightProjectile : IProjectile
{
    public void Launch(GameObject projectile, float speed, Transform targetTransform)
    {
        #region To reset the Collider2D.

        Collider2D trigger = projectile.GetComponent<Collider2D>();
        if (trigger is not null)
        {
            trigger.enabled = true;
            trigger.isTrigger = true;
        }
        #endregion
        //To get the direction between the projectile and the target.
        Vector3 direction = (targetTransform.position - projectile.transform.position).normalized;
        //To make the projectile face toward the direction.
        projectile.transform.right = direction;
        //To set the velocity.
        Rigidbody2D rigidbody = projectile.GetComponent<Rigidbody2D>();
        if (rigidbody == null)
        {
            Debug.LogError($"{projectile.name} is missing the Rigidbody2D!");
            return;
        }
        rigidbody.linearVelocity = direction * speed;
    }
}
/// <summary>
/// To launch projectiles in a parabola.
/// </summary>
[Serializable] public class ArcProjectile : IProjectile
{
    [SerializeField] float distanceAndHeightRatio = 0.25f;
    [SerializeField] float rotateSpeed = 1f;
    public void Launch(GameObject projectile, float speed, Transform targetTransform)
    {
        #region To reset the Collider2D.

        Collider2D trigger = projectile.GetComponent<Collider2D>();
        if (trigger != null) trigger.enabled = false;

        #endregion

        float distance = Vector2.Distance
            (projectile.transform.position, targetTransform.position);
        float duration = distance / speed;
        float arcHeight = distance*distanceAndHeightRatio;
        DOTween.Kill(projectile.transform);
        //The projectile moves with a parabola.
        projectile.transform.DOJump(targetTransform.position,
            arcHeight, 1, duration).SetEase(Ease.Linear);
        //The projectile rotates by itself.
        projectile.transform.DORotate(
            new Vector3(0, 0, 360f * duration * rotateSpeed), // 旋转角度取决于飞行时长
            duration,
            RotateMode.FastBeyond360
        ).SetEase(Ease.Linear);
    }
}
/// <summary>
/// To launch projectiles which can track monsters.
/// </summary>
public class TrackingProjectile { }
