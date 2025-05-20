using UnityEngine;

public interface IProjectile
{
    /// <summary>
    /// To launch the projectile in multiple ways.
    /// </summary>
    /// <param name="rigidbody2d">The projectile's rigidbody.</param>
    /// <param name="speed">The speed of the projectile. </param>
    /// <param name="targetTransform">The target transform.</param>
    public void Launch(Rigidbody2D rigidbody2d,float speed, Transform targetTransform);
}
public class Arrow : IProjectile
{
    public void Launch(Rigidbody2D rigidbody2d, float speed, Transform targetTransform)
    {
        //To get the direction between the projectile and the target.
        Vector3 direction = (targetTransform.position
                             - rigidbody2d.transform.position).normalized; 
        //To make the projectile face toward the direction.
        rigidbody2d.transform.forward = direction; 
        //To set the velocity.
        rigidbody2d.linearVelocity = direction * speed; 
    }
}
