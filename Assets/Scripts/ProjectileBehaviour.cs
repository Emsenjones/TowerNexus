using Sirenix.OdinInspector;
using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
  [Title("Config")]
  [SerializeField] float speed;
  [SerializeField] float validRange;
  [SerializeReference] IProjectile iProjectile;
  public void Initialize(Transform targetTransform)
  {
    Rigidbody2D rigidbody2d = GetComponent<Rigidbody2D>();
    if (rigidbody2d == null)
      Debug.LogError($"{gameObject.name} is missing the rigidbody2D");
    else
      iProjectile.Launch(rigidbody2d, speed, targetTransform);
  }
}
