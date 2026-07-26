using UnityEngine;

enum PhysHandState {
    NotGrabbing,
    Grabbing,
    Resetting
}

/// <summary>
/// Controller for one physics hand. 
/// </summary>
public class PhysHand : MonoBehaviour {
    [Header("Read Only Data")]
    [field: SerializeField] public PhysHandConfigurableJntData jntData { get; private set; }
    [field: SerializeField] public GrabberData grabberData { get; private set; }

    [Header("Refs")]
    [Tooltip("Transform of the corresponding VR controller.")]
    [field: SerializeField] public Transform ctrlTrf { get; private set; }
    [field: SerializeField] public Rigidbody rb { get; private set; }
    [field: SerializeField] public PlrCtrl plrCtrl { get; private set; }
    [Tooltip("Position for grab overlap sphere.")]
    [field: SerializeField] public Transform grabPoint { get; private set; }
    [Tooltip("Used to move the phys hand (and follow the corresponding VR controller).\n" +
    "NOTE: Joint's connected body should be null, since the hand should be connected to the 'world'.")]
    [field: SerializeField] public ConfigurableJoint worldJnt { get; private set; }
    [Tooltip("Game object containing hand visuals.")]
    [field: SerializeField] public GameObject vis { get; private set; }
    [field: SerializeField] public Collider[] cols { get; private set; }


    PhysHandState physHandState;
    IGrabbable grabbedGrabbable = null;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

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
        PhysHandNGrabbableUtils.SetWorldJntDrivesToDflt(worldJnt, jntData);
    }

    void FixedUpdate() {
        // Set joint target pose to controller pose.
        worldJnt.targetPosition = ctrlTrf.position;
        worldJnt.targetRotation = ctrlTrf.rotation;
    }

    private void Update() {
        switch (physHandState) {
            case PhysHandState.NotGrabbing:
                if (plrCtrl.TryConsumeGrabPressed()) {
                    if (TryGrabbing()) {
                        EnterGrabState();
                        break;
                    }
                }
                break;
            case PhysHandState.Grabbing:
                // TODO: Should probably do nothing.
                if (!plrCtrl.GrabButtonHeld()) {
                    grabbedGrabbable.ReleaseGrab(this);
                    EnterNotGrabbingState();
                }
                break;
            case PhysHandState.Resetting:
                // TODO: If after hand pose reset, or grab release the new pose of the hand would be blocked,
                // TODO C: enter state where colliders are disabled until they are not overlapping with anything.
                // TODO C: Use a red, translucent shader to communicate reset state.
                break;
            default:
                Debug.LogError("Switch defaulted.", this);
                break;
        }
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void EnterGrabState() {
        physHandState = PhysHandState.Grabbing;
        vis.SetActive(false);
        rb.isKinematic = true;
        foreach (Collider col in cols)
            col.enabled = false;
    }

    void EnterNotGrabbingState() {
        physHandState = PhysHandState.NotGrabbing;
        Debug.Assert(vis != null, "What the hell?", this);
        vis.SetActive(true);
        rb.isKinematic = false;
        rb.position = ctrlTrf.position;
        rb.rotation = ctrlTrf.rotation;
        // TODO: Set velocity to VR controller velocity.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        foreach (Collider col in cols)
            col.enabled = true;
    }

    // TODO: Maybe you don't need default drives at all if you never change them.
    // TODO C: Actually you might want them for the grab joint calculations.
    //void SetWorldJntDrivesToDflt() {
    //    JointDrive jntDrive = new JointDrive();
    //    // Linear drives:
    //    jntDrive.positionSpring = physHandConfigurableJntData.dfltLinDrivePosSpring;
    //    jntDrive.positionDamper = physHandConfigurableJntData.dfltLinDrivePosDamper;
    //    jntDrive.maximumForce = physHandConfigurableJntData.dfltLinDriveMaxForce;
    //    worldJnt.xDrive = jntDrive;
    //    worldJnt.yDrive = jntDrive;
    //    worldJnt.zDrive = jntDrive;
    //    // Angular drive:
    //    jntDrive.positionSpring = physHandConfigurableJntData.dfltSlerpDrivePosSpring;
    //    jntDrive.positionDamper = physHandConfigurableJntData.dfltSlerpDriveDamper;
    //    jntDrive.maximumForce = physHandConfigurableJntData.defaultSlerpDriveMaxForce;
    //    worldJnt.slerpDrive = jntDrive;
    //}

    /// <summary>
    /// Tries to find an <see cref="IGrabbable"/> implemented by a component on the collider's attached Rigidbody.
    /// Returns <see langword="null"/> if no <see cref="IGrabbable"/> is found.<br/>
    /// NOTE: The <see cref="IGrabbable"/> implementation must be on the same GameObject as the collider's attached Rigidbody.
    /// </summary>
    IGrabbable TryGetPhysicsHandGrabbableObject(Collider otherCollider) {
        IGrabbable grabbable = null;
        Rigidbody otherRb = otherCollider.attachedRigidbody;
        if (otherRb) {
            grabbable = otherRb.GetComponent<IGrabbable>();
        }
        return grabbable;
    }

    /// <summary>
    /// Searches for nearby objects with OverlapSphere and checks if any are eligible for grabbing. If so, grabs the closest grabbable object and returns true, otherwise returns false.
    /// </summary>
    bool TryGrabbing() {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            grabPoint.position,
            grabberData.chkSphereR,
            grabberData.grabLayers,
            QueryTriggerInteraction.Ignore
        );
        if (nearbyColliders.Length == 0)
            return false;
        IGrabbable closestGrabbable = null;
        float distanceToClosestGrabbable = 0;
        // Find closest grabbable object.
        foreach (Collider collider in nearbyColliders) {
            IGrabbable grabbable = TryGetPhysicsHandGrabbableObject(collider);
            if (grabbable == null)
                continue;
            if (!grabbable.CanBeGrabbed(this))
                continue;
            if (closestGrabbable == null) {
                closestGrabbable = grabbable;
                continue;
            }
            //Vector3 closestPointOnCollider = collider.ClosestPoint(grabPoint.position);
            //float grabbableDistance = Vector3.Distance(grabPoint.position, closestPointOnCollider);
            float grabbableDistance = grabbable.GetDistanceToGrabPoint(grabPoint.position);
            if (grabbableDistance < distanceToClosestGrabbable) {
                closestGrabbable = grabbable;
                distanceToClosestGrabbable = grabbableDistance;
            }
        }
        if (closestGrabbable == null)
            return false;
        // Found closest grabbable that can be grabbed!
        closestGrabbable.InitiateGrab(this);
        grabbedGrabbable = closestGrabbable;
        return true;
    }
}
