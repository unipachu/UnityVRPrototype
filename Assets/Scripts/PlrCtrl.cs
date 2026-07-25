using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player controller. Used to input device input based commands to game objects.
/// </summary>
public class PlrCtrl : MonoBehaviour {
    [SerializeField] InputActionProperty grabInputAction;

    bool grabButtonPressed = false;

    // Update is called once per frame
    void Update() {
        //grabButtonPressed = grabInputAction.action.ReadValue<float>() > 0.1f;
        grabButtonPressed = grabInputAction.action.WasPressedThisFrame();
    }

    public bool TryConsumeGrabPressed() {
        if (grabButtonPressed) {
            grabButtonPressed = false;
            return true;
        }
        return false;
    }
}
