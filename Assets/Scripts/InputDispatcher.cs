using UnityEngine;
public class InputDispatcher : MonoBehaviour
{
    [SerializeField] FloatingJoystick joystick;
    [SerializeField] new Camera camera;

    MonoBehaviour mouseDownMonoBehaviour;
    void Awake()
    {
        joystick.enabled = false;
    }
    public void Initialize()
    {
        if (joystick == null)
            Debug.LogError($"{gameObject.name} is missing a Joystick!");
        else
            joystick.enabled = true;
    }
    public void Dispose()
    {
        if (joystick == null)
            Debug.LogError($"{gameObject.name} is missing a Joystick!");
        else
            joystick.enabled = false;

    }
    void Update()
    {
        if (joystick == null)
        {
            Debug.LogError($"{gameObject.name} is missing a Joystick!");
            return;
        }
        if (!joystick.enabled) return;
        if (camera == null)
        {
            Debug.LogError($"{gameObject.name} is missing a camera!");
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            bool isJoystickVisible = true;

            Vector2 screenPos = Input.mousePosition;
            Vector2 worldPos = camera.ScreenToWorldPoint(screenPos);
            RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
            foreach (RaycastHit2D hit in hits)
            {
                MonoBehaviour monoBehaviour = hit.collider?.GetComponent<MonoBehaviour>();
                if (monoBehaviour is TowerBehaviour tower)
                {
                    mouseDownMonoBehaviour = monoBehaviour;

                    isJoystickVisible = false;
                    tower.IsClicked(true);
                }
                else if (monoBehaviour is RoleBehaviour role)
                {
                    mouseDownMonoBehaviour = monoBehaviour;
                    isJoystickVisible = false;
                    role.IsClicked(true);
                }
                // ...
                // ...

            }
            joystick.SetVisible(isJoystickVisible);
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (mouseDownMonoBehaviour is TowerBehaviour tower)
            {
                mouseDownMonoBehaviour = null;

                tower.IsClicked(false);
            }
            else if (mouseDownMonoBehaviour is RoleBehaviour role)
            {
                mouseDownMonoBehaviour = null;

                role.IsClicked(false);
            }
            // else if (selectedMonoBehaviour is RoleBehaviour role)
            // {
            //     selectedMonoBehaviour = null;
            //     //
            // }
        }






    }

    /// <summary>
    ///     Call this function to get Joystick input.
    /// </summary>
    /// <returns>return a Vector2.</returns>
    public Vector2 GetJoystickInput()
    {
        if (joystick is null || !joystick.enabled) return Vector2.zero;
        return joystick.Direction;
    }

//     bool IsClickOnUI()
//     {
// #if UNITY_ANDROID || UNITY_IOS
//         if (Input.touchCount > 0)
//             return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
// #endif
//         return EventSystem.current.IsPointerOverGameObject();
//     }
}
