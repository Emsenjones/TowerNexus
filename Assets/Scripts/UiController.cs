using System;
using UnityEngine;

public class UiController : MonoBehaviour
{
    [SerializeField] Joystick joystick;
    public Vector2 DirectionInput
    {
        get { return new Vector2(joystick.Horizontal, joystick.Vertical); }
    }
    
}
