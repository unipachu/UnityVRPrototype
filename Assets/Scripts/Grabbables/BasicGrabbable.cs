using System.Collections.Generic;
using UnityEngine;

public enum BasicGrabbableSingleGrabJntT {
    AnchorAtGrabbablePivot,
    AnchorAtPhysHandPos,
}

public class BasicGrabbable : MonoBehaviour, IGrabbable {
    [Header("Settings")]
    [SerializeField] BasicGrabbableSingleGrabJntT singleGrabJntT = BasicGrabbableSingleGrabJntT.AnchorAtPhysHandPos;

    [Header("Other Refs")]
    [SerializeField] ConfigurableJoint grabJnt;
    [SerializeField] Rigidbody rb;
    [Tooltip("Hand visual used to represent grabbing hand. \n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject lHandVisProxy;
    
    [HideInInspector] public Fsm grabJntFsm = new();
    [HideInInspector] public GrabbableJntSt_NoGrab jntSt_NoGrab;
    [HideInInspector] public GrabbableJntSt_SimpleSingleGrabWithAnchorAtPhysHandPos jntSt_simpleSingleGrabWithAnchorAtPhysHandPos;
    [HideInInspector] public GrabbableJntSt_SimpleSingleGrabWithAnchorAtPivot jntSt_simpleSingleGrabWithAnchorAtCom;

    readonly List<Grab> grabs = new(2);

    public GameObject GrabbableGameObj => gameObject;
    public ConfigurableJoint GrabJnt => grabJnt;
    public List<Grab> Grabs => grabs;
    public Rigidbody Rb => rb;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    private void Awake() {
        // Initialize FSM states.
        jntSt_NoGrab = new(this);
        jntSt_simpleSingleGrabWithAnchorAtPhysHandPos = new(this);
        jntSt_simpleSingleGrabWithAnchorAtCom = new(this);
    }

    private void Start() {
        grabJntFsm.SwitchState(jntSt_NoGrab, this);
    }

    void FixedUpdate() {
        grabJntFsm.CurrentState.PhysicsTick();
    }

    void OnDrawGizmos() {
        if (grabJnt == null)
            return;
        Gizmos.color = Color.yellow;
        Vector3 worldAnchorPos = grabJnt.transform.TransformPoint(grabJnt.anchor);
        Gizmos.DrawWireSphere(worldAnchorPos, 0.02f);
    }

    void OnDisable() {
        ReleaseAllGrabs();
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    public bool CanBeGrabbed(PhysHand physHand) {
        return true;
    }

    public bool CanBeReleased(PhysHand physHand) {
        return true;
    }

    /// <summary>
    /// Finds <see cref="Grab"/> by the <see cref="PhysHand"/>.
    /// If <see cref="PhysHand"/> is not grabbing this, returns null.
    /// </summary>
    public Grab FindGrab(PhysHand physHand) {
        for (int i = 0; i < grabs.Count; i++) {
            if (grabs[i].physHand == physHand)
                return grabs[i];
        }
        Debug.LogWarning($"{physHand.name} was not grabbing {gameObject.name}!", this);
        return null;
    }

    public float GetDistanceToGrabPoint(Vector3 physHandWorldGrabPoint) {
        return Vector3.Distance(transform.position, physHandWorldGrabPoint);
    }

    public void InitiateGrab(PhysHand physHand) {
        // NOTE: Phys hand should check if the grabbable can be grabbed before calling this method.
        //if (!CanBeGrabbed(physHand))
        //    return false;
        var newGrab = new Grab(
            physHand, 
            GeneralUtils.UnscaledInvrsTrft(transform, physHand.transform.position),
            GeneralUtils.RotFromWorldToTrfSpace(transform, physHand.transform.rotation)
        );
        grabs.Add(newGrab);
        // Setup hand proxy visual.
        lHandVisProxy.SetActive(true);
        lHandVisProxy.transform.position = physHand.transform.position;
        lHandVisProxy.transform.rotation = physHand.transform.rotation;
        SwitchStateBasedOnGrabCount();
    }

    public void ReleaseGrab(PhysHand physHand) {
        Grab grab = FindGrab(physHand);
        ReleaseGrab(grab);
        lHandVisProxy.SetActive(false);
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void ReleaseAllGrabs() {
        foreach (Grab grab in grabs)
            grab.physHand.OnGrabReleased(
                GeneralUtils.UnscaledTrfPt(transform, grab.initPhysHandPosInGrabbableLocalSpace),
                GeneralUtils.RotFromTrfSpaceToWorld(transform, grab.initRotFromGrabbableToPhysHand)
            );
        grabs.Clear();
        SwitchStateBasedOnGrabCount();
    }

    void ReleaseGrab(Grab grab) {
        grab.physHand.OnGrabReleased(
            GeneralUtils.UnscaledTrfPt(transform, grab.initPhysHandPosInGrabbableLocalSpace),
            GeneralUtils.RotFromTrfSpaceToWorld(transform, grab.initRotFromGrabbableToPhysHand)
        );
        grabs.Remove(grab);
        SwitchStateBasedOnGrabCount();
    }

    void SwitchStateBasedOnGrabCount() {
        IFsmSt nextState = grabs.Count switch {
            0 => jntSt_NoGrab,
            1 => singleGrabJntT switch {
                BasicGrabbableSingleGrabJntT.AnchorAtGrabbablePivot =>
                    jntSt_simpleSingleGrabWithAnchorAtCom,
                BasicGrabbableSingleGrabJntT.AnchorAtPhysHandPos =>
                    jntSt_simpleSingleGrabWithAnchorAtPhysHandPos,
                _ => throw new System.ArgumentOutOfRangeException(nameof(singleGrabJntT))
            },
            _ => null // TODO: Multigrab
        };
        if (nextState != grabJntFsm.CurrentState)
            grabJntFsm.SwitchState(nextState, this);
    }
}
