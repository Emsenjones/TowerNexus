using UnityEngine;
public abstract class WindowBehaviour : MonoBehaviour
{
    public virtual void Initialize(object data = null) { }
    protected void Close() { DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject); }
}
