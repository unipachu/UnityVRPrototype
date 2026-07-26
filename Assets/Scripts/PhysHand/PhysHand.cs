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
    [field: Header("README: \n" +
        "Template readme text\n" +
        "The End.")]

    [field: Header("Read Only Data")]
    [field: SerializeField] public PhysHandConfigurableJntData jntData { get; private set; }
    [field: SerializeField] public GrabberData grabberData { get; private set; }

    [field: Header("Refs")]
    [Tooltip("Transform of the follow target of the corresponding VR controller.")]
    [field: SerializeField] public Transform followTgtTrf { get; private set; }
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
        transform.position = followTgtTrf.position;
        transform.rotation = followTgtTrf.rotation;
        rb.position = followTgtTrf.position;
        rb.rotation = followTgtTrf.rotation;
        // Set world joint targets.
        worldJnt.targetPosition = followTgtTrf.position;
        worldJnt.targetRotation = followTgtTrf.rotation;
        PhysHandNGrabbableUtils.SetWorldJntDrivesToDflt(worldJnt, jntData);
    }

    void FixedUpdate() {
        // Set joint target pose to controller pose.
        worldJnt.targetPosition = followTgtTrf.position;
        worldJnt.targetRotation = followTgtTrf.rotation;
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
                    if(grabbedGrabbable.CanBeReleased(this))
                        grabbedGrabbable.ReleaseGrab(this);
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

    void OnDrawGizmos() {
        if (grabPoint == null || grabberData == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(grabPoint.position, grabberData.chkSphereR);
    }

    private void OnDisable() {
        if(grabbedGrabbable != null)
            // NOTE: We force grab release here without checking if the grab can be released
            // NOTE C: because this hand is about to get disabled/destroyed.
            grabbedGrabbable.ReleaseGrab(this);
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    /// <summary>
    /// Called by <see cref="IGrabbable"/> when grab by THIS hand is released.
    /// </summary>
    public void OnGrabReleased(Vector3 grabReleaseWorldPos, Quaternion grabReleaseWorldRot) {
        grabbedGrabbable = null;
        Debug.Assert(vis != null, "What the hell?", this);
        vis.SetActive(true);
        rb.isKinematic = false;
        // NOTE: Setting only rb pose (and not transform pose) causes the object to jerk when grab is released 
        // NOTE C: (likely because rb movement is solved only during physics simulation steps and visuals are
        // NOTE C: interpolated) and setting only transform pose in some cases seems to teleport the object where 
        // NOTE C: the grab was first initialized - likely because the rb pose is frozen where the grab started and 
        // NOTE C: rb pose overrides the transform pose. So you need to set BOTH transform and rb pose.
        transform.position = grabReleaseWorldPos;
        transform.rotation = grabReleaseWorldRot;
        rb.position = grabReleaseWorldPos;
        rb.rotation = grabReleaseWorldRot;
        // TODO: Set velocity to VR controller follow target velocity.
        foreach (Collider col in cols)
            col.enabled = true;
        physHandState = PhysHandState.NotGrabbing;
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

    /// <summary>
    /// Tries to find an <see cref="IGrabbable"/> implemented by a component on the collider's attached Rigidbody.
    /// Returns <see langword="null"/> if no <see cref="IGrabbable"/> is found.<br/>
    /// NOTE: The <see cref="IGrabbable"/> implementation must be on the same GameObject as the collider's attached Rigidbody.
    /// </summary>
    IGrabbable TryGetPhysicsHandGrabbableObject(Collider otherCollider) {
        IGrabbable grabbable = null;
        Rigidbody otherRb = otherCollider.attachedRigidbody;
        if (otherRb)
            grabbable = otherRb.GetComponent<IGrabbable>();
        return grabbable;
    }

    /// <summary>
    /// Searches for nearby objects with OverlapSphere and checks if any are eligible for grabbing.
    /// If so, grabs the closest <see cref="IGrabbable"/> and returns true, otherwise returns false.
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
