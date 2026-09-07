using UnityEngine;
using UnityEngine.Serialization;

public class ProgressAddController : MonoBehaviour
{
    [SerializeField] private PlayerSystem playerSystem;
    [FormerlySerializedAs("addedExp")]
    [SerializeField] private int addedProgress;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && playerSystem != null)
        {
            Debug.Log("Add progress!");
            playerSystem.DebugAddProgress(addedProgress);
        }
    }
}
