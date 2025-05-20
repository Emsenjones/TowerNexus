using UnityEngine;

public interface IProjectile
{
    /// <summary>
    /// To launch the projectile.
    /// </summary>
    /// <param name="rigidbody2d">The projectile's rigidbody.</param>
    /// <param name="speed">The speed of the projectile. </param>
    /// <param name="targetTransform">The target transform.</param>
    public void Launch(Rigidbody2D rigidbody2d,float speed, Transform targetTransform);
    //如果是抛射的话，通过rigidbody2d找到collider2d然后把collider2.enable = false。
}
public class Arrow : IProjectile
{
    public void Launch(Rigidbody2D rigidbody2d, float speed, Transform targetTransform)
    {
        //To get the direction between the projectile and the target.
        Vector3 direction = (targetTransform.position
                             - rigidbody2d.transform.position).normalized; 
        //To make the projectile face toward the direction.
        rigidbody2d.transform.right = direction; 
        //To set the velocity.
        rigidbody2d.linearVelocity = direction * speed; 
    }
}
