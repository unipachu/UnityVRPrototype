using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player controller. Used to input device input based commands to game objects.
/// </summary>
    // TODO: You should set this script to run before any other scripts so that the input can be used the same frame.
    // TODO: Event actions?
public class PlrCtrl : MonoBehaviour {
    [SerializeField] InputActionProperty lGrabInputAction;
    [SerializeField] InputActionProperty rGrabInputAction;
    [SerializeField] InputActionProperty lTriggerInputAction;
    [SerializeField] InputActionProperty rTriggerInputAction;

    /// <summary>
    /// Invoked when the left grab button is pressed.
    /// </summary>
    public event Action LGrabPressed;

    /// <summary>
    /// Invoked when the left grab button is released.
    /// </summary>
    public event Action LGrabReleased;

    /// <summary>
    /// Invoked when the right grab button is pressed.
    /// </summary>
    public event Action RGrabPressed;

    /// <summary>
    /// Invoked when the right grab button is released.
    /// </summary>
    public event Action RGrabReleased;


    /// <summary>
    /// Left grab pressed this frame.
    /// </summary>
    bool lGrabButtonPressed = false;
    bool lGrabButtonHeld = false;
    /// <summary>
    /// Right grab pressed this frame.
    /// </summary>
    bool rGrabButtonPressed = false;
    bool rGrabButtonHeld = false;

    public bool LGrabButtonHeld => lGrabButtonHeld;
    public bool RGrabButtonHeld => rGrabButtonHeld;
    
    void Update() {
        ReadButton(
            lGrabInputAction,
            ref lGrabButtonPressed,
            ref lGrabButtonHeld,
            LGrabPressed,
            LGrabReleased);

        ReadButton(
            rGrabInputAction,
            ref rGrabButtonPressed,
            ref rGrabButtonHeld,
            RGrabPressed,
            RGrabReleased);
    }

    public bool TryConsumeLGrabPressed() => TryConsumeButtonPress(ref lGrabButtonPressed);
    public bool TryConsumeRGrabPressed() => TryConsumeButtonPress(ref rGrabButtonPressed);

    private static void ReadButton(
        InputActionProperty ia,
        ref bool buttonPressed,
        ref bool buttonHeld,
        Action pressedEvent,
        Action releasedEvent
    ) {
        buttonPressed = false;
        if (!buttonHeld && ia.action.ReadValue<float>() >= 0.5f) {
            buttonHeld = true;
            buttonPressed = true;
            pressedEvent?.Invoke();
        }
        else if (ia.action.ReadValue<float>() < 0.5f) {
            buttonHeld = false;
            releasedEvent?.Invoke();
        }
    }

    private static bool TryConsumeButtonPress(ref bool buttonPressed) {
        if (buttonPressed) {
            buttonPressed = false;
            return true;
        }
        return false;
    }
}
