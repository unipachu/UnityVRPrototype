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
    [SerializeField] GameObject rHandVisProxy;
    
    [HideInInspector] public Fsm grabJntFsm = new();
    [HideInInspector] public GrabbableJntSt_NoGrab jntSt_NoGrab;
    [HideInInspector] public GrabbableJntSt_SingleGrab_SimpleAnchorAtPhysHandPos jntSt_SingleGrab_SimpleAnchorAtPhysHandPos;
    [HideInInspector] public GrabbableJntSt_SingleGrab_SimpleAnchorAtPivot jntSt_SingleGrab_SimpleAnchorAtCom;
    [HideInInspector] public GrabbableJntSt_MultiGrab_SimpleAnchorAtPivot jntSt_MultiGrab_SimpleAnchorAtCom;

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
        jntSt_SingleGrab_SimpleAnchorAtPhysHandPos = new(this);
        jntSt_SingleGrab_SimpleAnchorAtCom = new(this);
        jntSt_MultiGrab_SimpleAnchorAtCom = new(this);
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
        // Can be grabbed by up to one left hand and one right hand simultaneously.
        return GrabCount(physHand.side) < 2;
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

    /// <summary>
    /// How many <see cref="PhysHand"/>s of the specified side are grabbing this?
    /// </summary>
    public int GrabCount(Side handSide) {
        int counter = 0;
        foreach (Grab grab in grabs) {
            if(grab.physHand.side == handSide)
                counter++;
        }
        return counter;
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
        if (physHand.side == Side.Left)
            EnableProxyHand(ref lHandVisProxy, physHand);
        else 
            EnableProxyHand(ref rHandVisProxy, physHand);
        SwitchJntStateBasedOnGrabCount();
    }

    public void ReleaseGrab(PhysHand physHand) {
        Grab grab = FindGrab(physHand);
        grab.physHand.OnGrabReleased(
            GeneralUtils.UnscaledTrfPt(transform, grab.initPhysHandPosInGrabbableLocalSpace),
            GeneralUtils.RotFromTrfSpaceToWorld(transform, grab.initRotFromGrabbableToPhysHand)
        );
        grabs.Remove(grab);
        if(physHand.side == Side.Left)
            lHandVisProxy.SetActive(false);
        else
            rHandVisProxy.SetActive(false);
        SwitchJntStateBasedOnGrabCount();
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
        lHandVisProxy.SetActive(false);
        rHandVisProxy.SetActive(false);
        SwitchJntStateBasedOnGrabCount();
    }

    void EnableProxyHand(ref GameObject handVisProxy, PhysHand physHand) {
        handVisProxy.SetActive(true);
        handVisProxy.transform.position = physHand.transform.position;
        handVisProxy.transform.rotation = physHand.transform.rotation;
    }
    
    void SwitchJntStateBasedOnGrabCount() {
        IFsmSt nextState = grabs.Count switch {
            0 => jntSt_NoGrab,
            1 => singleGrabJntT switch {
                BasicGrabbableSingleGrabJntT.AnchorAtGrabbablePivot =>
                    jntSt_SingleGrab_SimpleAnchorAtCom,
                BasicGrabbableSingleGrabJntT.AnchorAtPhysHandPos =>
                    jntSt_SingleGrab_SimpleAnchorAtPhysHandPos,
                _ => throw new System.ArgumentOutOfRangeException(nameof(singleGrabJntT))
            },
            _ => jntSt_MultiGrab_SimpleAnchorAtCom
        };
        if (nextState != grabJntFsm.CurrentState)
            grabJntFsm.SwitchState(nextState, this);
    }
}
