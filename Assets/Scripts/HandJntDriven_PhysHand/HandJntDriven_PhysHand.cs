using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Controller for one physics hand. 
/// </summary>
public class HandJntDriven_PhysHand : MonoBehaviour, IGnrPhysHand {
    // TODO: Common phys hand fields could be made into a separate monobehaviour the specialized
    // TODO C: phys hands would have a reference to.
    [Header("Settings")]
    public Side handSide;

    [Header("Read Only Data")]
    public DfltConfigJntData wldJntData;
    public GrbrData grbrData;
    public FolTgtGhostShdrData ghostShdrData;

    [Header("XROrigin Refs")]
    [Tooltip("Transform of the follow target of the corresponding VR controller.")]
    public Transform followTgtTrf;
    public GhostShdrCtlr handGhostShaderCtrl;
    [Tooltip("HapticImpulsePlayer of the matching controller.")]
    public HapticImpulsePlayer ctrlHapticImpPlr;

    [Header("Player Input Refs")]
    public PlrCtrl plrCtrl;

    [Header("Other Refs")]
    public Rigidbody rb;
    [Tooltip("Position for grabbale search overlap sphere.")]
    public Transform grblSearchPos;
    [Tooltip("Used to move the phys hand (and follow the corresponding VR controller).\n" +
    "NOTE: Joint's connected body should be null, since the hand should be connected to the 'world'.")]
    public ConfigurableJoint worldJnt;
    [Tooltip("Game object containing hand visuals.")]
    public GameObject vis;
    public Collider[] cols;

    PhysHandState physHandSt = PhysHandState.NotGrabbing;
    IHandJntDriven_Grbl grabbedGrbl = null;

    public HapticImpulsePlayer CtrlHapticImpPlr => ctrlHapticImpPlr;
    public Transform FollowTgtTrf => followTgtTrf;
    public Side HandSide => handSide;
    public Transform Trf => transform;



    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Start() {
        PhysUtils.TeleportWldJntCtrldRb(transform, rb, followTgtTrf, worldJnt, wldJntData);
    }

    void FixedUpdate() {
        // Set joint target pose to controller pose.
        worldJnt.targetPosition = followTgtTrf.position;
        worldJnt.targetRotation = followTgtTrf.rotation;
    }
    void Update() {
        switch (physHandSt) {
            case PhysHandState.NotGrabbing:
                if (
                    handSide == Side.Left && plrCtrl.TryConsumeLGrabPressed() ||
                    handSide == Side.Right && plrCtrl.TryConsumeRGrabPressed()
                )
                    if (TryGrabbing()) 
                        break;
                PhysHandUtils.UpdateTgtGhostShader(handGhostShaderCtrl, transform.position, followTgtTrf.position, ghostShdrData);
                break;
            case PhysHandState.Grabbing:
                if (
                    handSide == Side.Left && !plrCtrl.LGrabButtonHeld ||
                    handSide == Side.Right && !plrCtrl.RGrabButtonHeld
                ) {
                    if (grabbedGrbl.CanBeReleased(this)) {
                        grabbedGrbl.ReleaseGrb(this);
                        return;
                    }
                }
                // TODO: Since grabbable now controls the phys hand visual proxy, it should
                // TODO C: call the UpdateTgtGhostShader with the correct distance.
                // TODO C: Then remove the line below:
                // TODO C: Or actually
                PhysHandUtils.UpdateTgtGhostShader(handGhostShaderCtrl, transform.position, new Vector3(99999, 99999, 99999), ghostShdrData);
                break;
            case PhysHandState.Resetting:
                // TODO: If after hand pose reset, or grab release the new pose of the hand would be blocked,
                // TODO C: enter state where colliders are disabled until they are not overlapping with anything.
                // TODO C: Use a red, translucent shader to communicate reset state.
                PhysHandUtils.UpdateTgtGhostShader(handGhostShaderCtrl, transform.position, followTgtTrf.position, ghostShdrData);
                break;
            default:
                Debug.LogError("Switch defaulted.", this);
                break;
        }
    }
    void OnDrawGizmos() {
        if (grblSearchPos == null || grbrData == null)
            return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(grblSearchPos.position, grbrData.overlapSphereR);
    }

    private void OnDisable() {
        if (grabbedGrbl != null)
            // NOTE: We force grab release here without checking if the grab can be released
            // NOTE C: because this hand is about to get disabled/destroyed.
            grabbedGrbl.ReleaseGrb(this);
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    public void OnGrabReleased(Vector3 grabReleaseWorldPos, Quaternion grabReleaseWorldRot) {
        throw new System.NotImplementedException();
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    /// <summary>
    /// NOTE: Hand joint initiates a grab, the grabbable initiates the END of the grab. Just like all my relationships.
    /// </summary>
    void InitGrab(IHandJntDriven_Grbl grbl) {
        grbl.OnInitGrb(this);
        grabbedGrbl = grbl;
        ctrlHapticImpPlr.SendHapticImpulse(0.5f, 0.1f);
        physHandSt = PhysHandState.Grabbing;
        vis.SetActive(false);
        rb.isKinematic = true;
        foreach (Collider col in cols)
            col.enabled = false;
    }

    /// <summary>
    /// Tries to find an <see cref="IHandJntDriven_Grbl"/> implemented by a component on the collider's attached Rigidbody.
    /// Returns <see langword="null"/> if no <see cref="IHandJntDriven_Grbl"/> is found.<br/>
    /// NOTE: The <see cref="IHandJntDriven_Grbl"/> implementation must be on the same GameObject as the collider's attached Rigidbody.
    /// </summary>
    IHandJntDriven_Grbl TryGetPhysicsHandGrabbableObject(Collider otherCollider) {
        IHandJntDriven_Grbl grabbable = null;
        Rigidbody otherRb = otherCollider.attachedRigidbody;
        if (otherRb)
            grabbable = otherRb.GetComponent<IHandJntDriven_Grbl>();
        return grabbable;
    }

    /// <summary>
    /// Searches for nearby objects with OverlapSphere and checks if any are eligible for grabbing.
    /// If so, grabs the closest <see cref="IHandJntDriven_Grbl"/> and returns true, otherwise returns false.
    /// </summary>
    bool TryGrabbing() {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            grblSearchPos.position,
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
        IHandJntDriven_Grbl closestGrabbable = null;
        float distanceToClosestGrabbable = 0;
        // Find closest grabbable object.
        foreach (Collider collider in nearbyColliders) {
            IHandJntDriven_Grbl grabbable = TryGetPhysicsHandGrabbableObject(collider);
            if (grabbable == null)
                continue;
            if (!grabbable.CanBeGrabbed(this))
                continue;
            if (closestGrabbable == null) {
                closestGrabbable = grabbable;
                continue;
            }
            float grabbableDistance = grabbable.GnrGrbl.GetDistToGrbPt(grblSearchPos.position);
            if (grabbableDistance < distanceToClosestGrabbable) {
                closestGrabbable = grabbable;
                distanceToClosestGrabbable = grabbableDistance;
            }
        }
        if (closestGrabbable == null)
            return false;
        // Found closest grabbable that can be grabbed!
        InitGrab(closestGrabbable);
        return true;
    }
}
