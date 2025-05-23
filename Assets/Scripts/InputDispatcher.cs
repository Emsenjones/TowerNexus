using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputDispatcher : MonoBehaviour
{
    [SerializeField] Joystick joystick;
    [SerializeField] float joystickTransparency = 0.5f;
    [SerializeField] new Camera camera;

    MonoBehaviour mouseDownMonoBehaviour;
    void Update()
    {
        if (joystick == null)
        {
            Debug.LogError($"{gameObject.name} is missing a Joystick!");
            return;
        }
        if (camera == null)
        {
            Debug.LogError($"{gameObject.name} is missing a camera!");
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 screenPos = Input.mousePosition;
            Vector2 worldPos = camera.ScreenToWorldPoint(screenPos);

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            MonoBehaviour monoBehaviour = hit.collider?.GetComponent<MonoBehaviour>();
            if (monoBehaviour is TowerBehaviour tower)
            {
                if(mouseDownMonoBehaviour == null)
                    mouseDownMonoBehaviour = monoBehaviour;
                
                SwitchJoystick(false);
                tower.OnMouse(true);
            }
            // else if (monoBehaviour is RoleBehaviour role)
            // {
            //     //Click roleBehaviour...
            //     if(selectedMonoBehaviour == null)
            //         selectedMonoBehaviour = monoBehaviour;
            // }
            // ...
            // ...
            
            else
                SwitchJoystick(true);
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (mouseDownMonoBehaviour is TowerBehaviour tower)
            {
                mouseDownMonoBehaviour = null;
                
                tower.OnMouse(false);
            }
            // else if (selectedMonoBehaviour is RoleBehaviour role)
            // {
            //     selectedMonoBehaviour = null;
            //     //
            // }
        }






    }
    void SwitchJoystick(bool isVisible)
    {
        if (joystick == null)
        {
            Debug.LogError($"{gameObject.name} is missing a Joystick!");
            return;
        }

        Image[] images = joystick.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img.gameObject == joystick.gameObject) continue;

            Color color = img.color;
            color.a = isVisible ? joystickTransparency : 0f;
            img.color = color;
            //Debug.Log($"Color alpha: {color.a}");
        }
    }

    /// <summary>
    /// Call this function to get Joystick input.
    /// </summary>
    /// <returns>return a Vector2.</returns>
    public Vector2 GetJoystickInput()
    {
        return joystick is not null
            ? new Vector2(joystick.Horizontal, joystick.Vertical)
            : Vector2.zero;
    }

    bool IsClickOnUI()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
        return EventSystem.current.IsPointerOverGameObject();
    }
}
