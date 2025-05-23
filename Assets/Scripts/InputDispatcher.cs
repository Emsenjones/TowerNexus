using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputDispatcher : MonoBehaviour
{
    [SerializeField] Joystick joystick;
    [SerializeField] float joystickTransparency = 0.5f;
    [SerializeField] new Camera camera;

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

        if (Input.GetMouseButton(0))
        {
            Vector2 screenPos = Input.mousePosition;
            Vector2 worldPos = camera.ScreenToWorldPoint(screenPos);

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            MonoBehaviour monoBehaviour = hit.collider?.GetComponent<MonoBehaviour>();
            if (monoBehaviour is TowerBehaviour)
            {
                Debug.Log("Hit Tower!");
            }




            bool isOverBehaviour = monoBehaviour is TowerBehaviour;
            //bool isOverTower = monoBehaviour is TowerBehaviour || monoBehaviour is MonsterBehaviour;
            SwitchJoystick(!isOverBehaviour);
        }

        
      

        
        
    }
    void SwitchJoystick(bool isOn)
    {
        if (joystick == null)
        {
            Debug.LogError($"{gameObject.name} is missing a Joystick!");
            return;
        }

        Image[] images = joystick.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if(img.gameObject ==joystick.gameObject) continue;
            
            Color color = img.color;
            color.a = isOn ? joystickTransparency : 0f;
            img.color = color;
            Debug.Log($"Color alpha: {color.a}");
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
