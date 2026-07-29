using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

enum PhysHandState {
    NotGrabbing,
    Grabbing,
    Resetting
}

/// <summary>
/// Controller for one physics hand. 
/// </summary>
public class GrblJntDriven_PhysHand : MonoBehaviour {
    [field: Header("Settings")]
    [field: SerializeField] public Side side { get; private set; }

    [field: Header("Read Only Data")]
    [field: SerializeField] public PhysHandConfigurableJntData jntData { get; private set; }
    [field: SerializeField] public GrbrData grbrData { get; private set; }

    [field: Header("XROrigin Refs")]
    [Tooltip("Transform of the follow target of the corresponding VR controller.")]
    [field: SerializeField] public Transform followTgtTrf { get; private set; }
    [field: SerializeField] public GhostShaderCtlr handGhostShaderCtrl { get; private set; }
    [Tooltip("HapticImpulsePlayer of the matching controller.")]
    public HapticImpulsePlayer controllerHapticImpulsePlayer;
    
    [field: Header("Player Input Refs")]
    [field: SerializeField] public PlrCtrl plrCtrl { get; private set; }
    [Tooltip("Position for grab overlap sphere.")]

    [field: Header("Other Refs")]
    [field: SerializeField] public Rigidbody rb { get; private set; }
    [field: SerializeField] public Transform grbPt { get; private set; }
    [Tooltip("Used to move the phys hand (and follow the corresponding VR controller).\n" +
    "NOTE: Joint's connected body should be null, since the hand should be connected to the 'world'.")]
    [field: SerializeField] public ConfigurableJoint worldJnt { get; private set; }
    [Tooltip("Game object containing hand visuals.")]
    [field: SerializeField] public GameObject vis { get; private set; }
    [field: SerializeField] public Collider[] cols { get; private set; }

    PhysHandState physHandSt = PhysHandState.NotGrabbing;
    IGrblJntDriven_Grbl grabbedGrbl = null;

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
        PhysUtils.SetJntDrivesToDflt(worldJnt, jntData);
    }

    void FixedUpdate() {
        // Set joint target pose to controller pose.
        worldJnt.targetPosition = followTgtTrf.position;
        worldJnt.targetRotation = followTgtTrf.rotation;
    }

    private void Update() {
        switch (physHandSt) {
            case PhysHandState.NotGrabbing:
                if (
                    side == Side.Left && plrCtrl.TryConsumeLGrabPressed() ||
                    side == Side.Right && plrCtrl.TryConsumeRGrabPressed()
                ) {
                    if (TryGrabbing()) {
                        EnterGrabState();
                        break;
                    }
                }
                UpdateTgtGhostShader(transform.position);
                break;
            case PhysHandState.Grabbing:
                if (
                    side == Side.Left && !plrCtrl.LGrabButtonHeld ||
                    side == Side.Right && !plrCtrl.RGrabButtonHeld
                ) {
                    if(grabbedGrbl.CanBeReleased(this)) {
                        grabbedGrbl.ReleaseGrb(this);
                        return;
                    }
                }
                // TODO: Since grabbable now controls the phys hand visual proxy, it should
                // TODO C: call the UpdateTgtGhostShader with the correct distance.
                // TODO C: Then remove the line below:
                // TODO C: Or actually
                UpdateTgtGhostShader(new Vector3(99999,99999,99999));
                break;
            case PhysHandState.Resetting:
                // TODO: If after hand pose reset, or grab release the new pose of the hand would be blocked,
                // TODO C: enter state where colliders are disabled until they are not overlapping with anything.
                // TODO C: Use a red, translucent shader to communicate reset state.
                UpdateTgtGhostShader(transform.position);
                break;
            default:
                Debug.LogError("Switch defaulted.", this);
                break;
        }
    }

    void OnDrawGizmos() {
        if (grbPt == null || grbrData == null)
            return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(grbPt.position, grbrData.overlapSphereR);
    }

    private void OnDisable() {
        if(grabbedGrbl != null)
            // NOTE: We force grab release here without checking if the grab can be released
            // NOTE C: because this hand is about to get disabled/destroyed.
            grabbedGrbl.ReleaseGrb(this);
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    /// <summary>
    /// Called by <see cref="IGrblJntDriven_Grbl"/> when grab by THIS hand is released.
    /// </summary>
    public void OnGrabReleased(Vector3 grabReleaseWorldPos, Quaternion grabReleaseWorldRot) {
        grabbedGrbl = null;
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
        physHandSt = PhysHandState.NotGrabbing;
    }

    public void UpdateTgtGhostShader(Vector3 physHandWorldPos) {
        float dist = Vector3.Distance(followTgtTrf.position, physHandWorldPos);
        float invisibleDist = 0.001f;
        float maxTransparencyDist = 0.1f;
        float maxTransparency = 0.9f;
        float t = Mathf.InverseLerp(invisibleDist, maxTransparencyDist, dist);
        float newTransparency = t * maxTransparency;
        handGhostShaderCtrl.SetTransparency(newTransparency);
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void EnterGrabState() {
        physHandSt = PhysHandState.Grabbing;
        vis.SetActive(false);
        rb.isKinematic = true;
        foreach (Collider col in cols)
            col.enabled = false;
    }

    /// <summary>
    /// Tries to find an <see cref="IGrblJntDriven_Grbl"/> implemented by a component on the collider's attached Rigidbody.
    /// Returns <see langword="null"/> if no <see cref="IGrblJntDriven_Grbl"/> is found.<br/>
    /// NOTE: The <see cref="IGrblJntDriven_Grbl"/> implementation must be on the same GameObject as the collider's attached Rigidbody.
    /// </summary>
    IGrblJntDriven_Grbl TryGetPhysicsHandGrabbableObject(Collider otherCollider) {
        IGrblJntDriven_Grbl grabbable = null;
        Rigidbody otherRb = otherCollider.attachedRigidbody;
        if (otherRb)
            grabbable = otherRb.GetComponent<IGrblJntDriven_Grbl>();
        return grabbable;
    }

    /// <summary>
    /// Searches for nearby objects with OverlapSphere and checks if any are eligible for grabbing.
    /// If so, grabs the closest <see cref="IGrblJntDriven_Grbl"/> and returns true, otherwise returns false.
    /// </summary>
    bool TryGrabbing() {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            grbPt.position,
            grbrData.overlapSphereR,
            grbrData.grbLayers,
            QueryTriggerInteraction.Ignore
        );
        if (nearbyColliders.Length == 0)
            return false;
        //Debug.Log(
        //    $"Found colliders ({nearbyColliders.Length}): " +
        //    string.Join(", ", Array.ConvertAll(nearbyColliders, c => c.name))
        //);
        IGrblJntDriven_Grbl closestGrabbable = null;
        float distanceToClosestGrabbable = 0;
        // Find closest grabbable object.
        foreach (Collider collider in nearbyColliders) {
            IGrblJntDriven_Grbl grabbable = TryGetPhysicsHandGrabbableObject(collider);
            if (grabbable == null)
                continue;
            if (!grabbable.CanBeGrabbed(this))
                continue;
            if (closestGrabbable == null) {
                closestGrabbable = grabbable;
                continue;
            }
            float grabbableDistance = grabbable.GetDistToGrbPt(grbPt.position);
            if (grabbableDistance < distanceToClosestGrabbable) {
                closestGrabbable = grabbable;
                distanceToClosestGrabbable = grabbableDistance;
            }
        }
        if (closestGrabbable == null)
            return false;
        // Found closest grabbable that can be grabbed!
        closestGrabbable.InitiateGrb(this);
        grabbedGrbl = closestGrabbable;
        controllerHapticImpulsePlayer.SendHapticImpulse(0.5f, 0.1f);
        return true;
    }
}
