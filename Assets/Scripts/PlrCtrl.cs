using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player controller. Used to input device input based commands to game objects.
/// </summary>
    // TODO: You should set this script to run before any other scripts so that the input can be used the same frame.
public class PlrCtrl : MonoBehaviour {
    [SerializeField] InputActionProperty grabInputAction;

    /// <summary>
    /// Grab pressed this frame.
    /// </summary>
    bool grabButtonPressed = false;
    bool grabButtonHeld = false;

    void Update() {
        //grabButtonPressed = grabInputAction.action.ReadValue<float>() > 0.1f;
        //grabButtonPressed = grabInputAction.action.WasPressedThisFrame();
        grabButtonPressed = false;
        if (!grabButtonHeld && grabInputAction.action.ReadValue<float>() >= 0.5f) {
            grabButtonHeld = true;
            grabButtonPressed = true;
        }
        else if (grabInputAction.action.ReadValue<float>() < 0.5f)
            grabButtonHeld = false;
        //Debug.Log($"Grab button held: {grabButtonHeld}");
    }

    public bool GrabButtonHeld() => grabButtonHeld;

    public bool TryConsumeGrabPressed() {
        if (grabButtonPressed) {
            grabButtonPressed = false;
            return true;
        }
        return false;
    }

}
