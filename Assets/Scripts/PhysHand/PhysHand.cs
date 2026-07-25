using UnityEngine;

enum PhysHandState {
    NotGrabbing,
    Grabbing
}

/// <summary>
/// Controller for one physics hand. 
/// </summary>
public class PhysHand : MonoBehaviour {
    [Header("Read Only Data")]
    [SerializeField] PhysHandConfigurableJntData physHandData;

    [Header("Refs")]
    [Tooltip("Used to move the phys hand (and follow the corresponding VR controller).\n" +
        "NOTE: Joint's connected body should be null, since the hand should be connected to the 'world'.")]
    [SerializeField] ConfigurableJoint worldJnt;
    [Tooltip("Transform of the corresponding VR controller.")]
    [SerializeField] Transform ctrlTrf;
    [SerializeField] Rigidbody rb;
    [SerializeField] PlrCtrl plrCtrl;

    PhysHandState physHandState;

    void Start() {
        // NOTE: These default values should be set from Configurable Joint's inspector and they are here just as an example.
        // We want the connected anchor to be in the world origin so that the target pose matches world coodrinates.
        //worldJnt.autoConfigureConnectedAnchor = false;
        // We want the connected anchor to be in the world origin so that the target pose matches world coodrinates.
        //worldJnt.connectedAnchor = Vector3.zero;
        // Quaternion Slerp mode avoids problems with Euler Angles.
        //worldJnt.rotationDriveMode = RotationDriveMode.Slerp;
        // When swap bodies is set to true, joint target pose is interpreted relative to the connected body's anchor's space
        // instead of this rb's anchor's space. In this case it makes the target position equivalent to world space pose.
        //worldJnt.swapBodies = true;

        // We move the hand to the pose of the controller.
        // TODO: Test if moving the transform pose is enough.
        transform.position = ctrlTrf.position;
        transform.rotation = ctrlTrf.rotation;
        rb.position = ctrlTrf.position;
        rb.rotation = ctrlTrf.rotation;
        // Set world joint targets.
        worldJnt.targetPosition = ctrlTrf.position;
        worldJnt.targetRotation = ctrlTrf.rotation;
        SetWorldJntDrivesToDflt();
    }

    void FixedUpdate() {
        // Set joint target pose to controller pose.
        worldJnt.targetPosition = ctrlTrf.position;
        worldJnt.targetRotation = ctrlTrf.rotation;
    }

    private void Update() {
        switch (physHandState) {
            case PhysHandState.NotGrabbing:
                if(plrCtrl.TryConsumeGrabPressed()) {
                    // TODO: Try grab.
                }
                break;
            case PhysHandState.Grabbing:
                break;
            default:
                Debug.LogError("Switch defaulted.", this);
                break;
        }
    }

    void SetWorldJntDrivesToDflt() {
        JointDrive jntDrive = new JointDrive();
        // Linear drives:
        jntDrive.positionSpring = physHandData.dfltLinDrivePosSpring;
        jntDrive.positionDamper = physHandData.dfltLinDrivePosDamper;
        jntDrive.maximumForce = physHandData.dfltLinDriveMaxForce;
        worldJnt.xDrive = jntDrive;
        worldJnt.yDrive = jntDrive;
        worldJnt.zDrive = jntDrive;
        // Angular drive:
        jntDrive.positionSpring = physHandData.dfltSlerpDrivePosSpring;
        jntDrive.positionDamper = physHandData.dfltSlerpDriveDamper;
        jntDrive.maximumForce = physHandData.defaultSlerpDriveMaxForce;
        worldJnt.slerpDrive = jntDrive;
    }
}
