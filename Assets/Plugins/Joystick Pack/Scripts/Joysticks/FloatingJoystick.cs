using UnityEngine.EventSystems;
public class FloatingJoystick : Joystick
{
    
    public override void OnPointerDown(PointerEventData eventData)
    {
        background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        background.gameObject.SetActive(true);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
    }
    public void SetVisible(bool isVisible)
    {
        background.gameObject.SetActive(isVisible);
    }



}
