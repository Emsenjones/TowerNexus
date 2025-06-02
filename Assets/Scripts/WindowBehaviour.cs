using UnityEngine;
using UnityEngine.UI;
public abstract class WindowBehaviour : MonoBehaviour
{
    [SerializeField] protected Button closeButton;
    public virtual void Initialize(object data = null) { }
    protected void Close() { DungeonManager.Instance.RecyclePoolController.RecycleOneObject(gameObject); }
}
