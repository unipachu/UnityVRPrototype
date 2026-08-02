using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Controller for one physics hand. 
/// </summary>
// TODO: Since the physics hands are so similar, you should've made just one physics hand with
// TODO C: the ability to grab any game object grabbable types. That way it would've been easier to
// TODO C: recycle code and you could've lost one phys hand abstraction layer. Oh well, some day I'll
// TODO C: refactor.
public class HandJntDriven_PhysHand :
    MonoBehaviour,
    IGnrPhysHand<IHandJntDriven_Grbl, HandJntDriven_PhysHand>
{
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
    // You could just put these in a generic game object phys hand data class which is then used by all go phys hands.
    public Rigidbody rb;
    [Tooltip("Position for grabbale search overlap sphere.")]
    public Transform grblSearchPos;
    [Tooltip("Used to move the phys hand (and follow the corresponding VR controller).\n" +
    "NOTE: Joint's connected body should be null, since the hand should be connected to the 'world'.")]
    public ConfigurableJoint wldJnt;
    [Tooltip("Game object containing hand visuals.")]
    public GameObject vis;
    public Collider[] cols;

    /// <summary>
    /// Connects phys hand to grabbables.
    /// </summary>
    [HideInInspector] public ConfigurableJoint grbJnt = null;

    PhysHandState physHandSt = PhysHandState.NotGrabbing;
    IHandJntDriven_Grbl grabbedGrbl = null;

    public HapticImpulsePlayer CtrlHapticImpPlr => ctrlHapticImpPlr;
    public Transform FolTgtTrf => followTgtTrf;
    public Side HandSide => handSide;
    public Transform Trf => transform;
    public ConfigurableJoint WldJnt => wldJnt;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Awake() {
        grbJnt = gameObject.AddComponent<ConfigurableJoint>();
        grbJnt.autoConfigureConnectedAnchor = false;
    }

    void Start() {
        PhysUtils.TeleportWldJntCtrldRb(transform, rb, followTgtTrf, wldJnt, wldJntData);
        // TODO: Set anchor based on grab.
        //grbJnt.anchor = transform.InverseTransformPoint(grbJnt.position);
    }

    void FixedUpdate() {
        // Set joint target pose to controller pose.
        wldJnt.targetPosition = followTgtTrf.position;
        wldJnt.targetRotation = followTgtTrf.rotation;
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
        if (grbJnt != null) {
            ObjUtils.OnDrawGizmos_DrawJntAnch(grbJnt);
            ObjUtils.OnDrawGizmos_DrawJntConnectedAnch(grbJnt);
        }
        if (grblSearchPos != null && grbrData != null) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(grblSearchPos.position, grbrData.overlapSphereR);
        }
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
  
    /// <summary>
    /// NOTE: Hand joint initiates a grab, the grabbable initiates the END of the grab.
    /// Just like all my relationships.
    /// </summary>
    public void InitGrab(IHandJntDriven_Grbl grbl) {
        grbl.OnInitGrb(this);
        grabbedGrbl = grbl;
        ctrlHapticImpPlr.SendHapticImpulse(0.5f, 0.1f);
        physHandSt = PhysHandState.Grabbing;
        foreach (Collider col in cols)
            col.enabled = false;
    }

    /// <summary>
    /// This should be called by the grabbable when in ends the grab (since grabbables are
    /// responsible for ending the grab).
    /// </summary>
    public void OnReleaseGrb() {
        PhysUtils.SetJntMotCstrsToFree(grbJnt);
        PhysUtils.SetJntDrivesToDflt(wldJnt, wldJntData);
        grbJnt.connectedBody = null;
        grabbedGrbl = null;
        foreach (Collider col in cols)
            col.enabled = true;
        physHandSt = PhysHandState.NotGrabbing;
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    /// <summary>
    /// Tries to find an <see cref="IHandJntDriven_Grbl"/> implemented by a component on
    /// the collider's attached Rigidbody.<br/>
    /// Returns <see langword="null"/> if no <see cref="IHandJntDriven_Grbl"/> is found.<br/>
    /// NOTE: The <see cref="IHandJntDriven_Grbl"/> implementation must be on the same
    /// GameObject as the collider's attached Rigidbody.
    /// </summary>
    IHandJntDriven_Grbl TryGetPhysicsHandGrabbableObject(Collider otherCollider) {
        IHandJntDriven_Grbl grabbable = null;
        Rigidbody otherRb = otherCollider.attachedRigidbody;
        if (otherRb)
            grabbable = otherRb.GetComponent<IHandJntDriven_Grbl>();
        return grabbable;
    }

    /// <summary>
    /// Searches for nearby objects with OverlapSphere and checks if any are eligible for
    /// grabbing. If so, grabs the closest <see cref="IHandJntDriven_Grbl"/> and returns
    /// true, otherwise returns false.
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
            float grabbableDistance = grabbable.GetDistToGrbPt(grblSearchPos.position);
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
