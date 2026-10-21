// Managed boundary doubles. Native event buffering is verified in Unity separately.
namespace UnityEngine.InputSystem
{
    public enum TouchPhase { None, Began, Moved, Stationary, Ended, Canceled }
    public class InputDevice { public bool added = true; }
    public class Mouse : InputDevice
    {
        public static Mouse current = new Mouse();
        public Controls.ButtonControl leftButton = new Controls.ButtonControl();
        public Controls.ButtonControl rightButton = new Controls.ButtonControl();
        public Controls.ValueControl<UnityEngine.Vector3> position = new Controls.ValueControl<UnityEngine.Vector3>();
    }
    public class Touchscreen : InputDevice
    {
        public static Touchscreen current;
        public Controls.TouchControl primaryTouch;
        public Controls.TouchControl[] touches = new Controls.TouchControl[0];
    }
}
namespace UnityEngine.InputSystem.Controls
{
    public class ValueControl<T> { public T value; public T ReadValue() => value; }
    public class ButtonControl { public bool isPressed, wasPressedThisFrame, wasReleasedThisFrame; }
    public class TouchControl
    {
        public UnityEngine.InputSystem.InputDevice device;
        public ValueControl<int> touchId = new ValueControl<int>();
        public ValueControl<UnityEngine.Vector3> position = new ValueControl<UnityEngine.Vector3>();
        public ValueControl<UnityEngine.InputSystem.TouchPhase> phase = new ValueControl<UnityEngine.InputSystem.TouchPhase>();
        public ButtonControl press = new ButtonControl();
    }
}
namespace UnityEngine.InputSystem.EnhancedTouch
{
    public static class EnhancedTouchSupport
    {
        public static int leases;
        public static void Enable() { leases++; }
        public static void Disable() { leases--; }
    }
    public struct Touch
    {
        public int touchId;
        public UnityEngine.Vector3 screenPosition;
        public UnityEngine.InputSystem.TouchPhase phase;
        public static System.Collections.Generic.IReadOnlyList<Touch> activeTouches = new Touch[0];
    }
}
namespace UnityEngine.InputSystem.UI
{
    public class ExtendedPointerEventData : UnityEngine.EventSystems.PointerEventData
    {
        public ExtendedPointerEventData() : base(null) {}
        public UnityEngine.InputSystem.InputDevice device;
        public int touchId;
    }
}
