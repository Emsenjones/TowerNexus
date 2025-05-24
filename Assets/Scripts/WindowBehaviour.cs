using UnityEngine;
public abstract class WindowBehaviour : MonoBehaviour
{
    public virtual void Initialize(object data = null) { }
    public void Close() { DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject); }
}
