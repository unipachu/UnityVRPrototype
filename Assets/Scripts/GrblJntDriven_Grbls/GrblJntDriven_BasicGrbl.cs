using UnityEngine;

public enum GrblJntDriven_BasicGrblSglGrbJntT {
    AnchAtGrblPiv,
    AnchAtPhysHandPos,
}

public enum GrblJntDriven_BasicGrblDblGrbJntT {
    GrbLineAligned,
    SimpleAnchAtPiv,
}

public class GrblJntDriven_BasicGrbl : MonoBehaviour, IGrblJntDriven_Grbl, IDblGrb_GrbLineAligned {
    [Header("Settings")]
    [SerializeField] GrblJntDriven_BasicGrblSglGrbJntT sglGrbJntT = GrblJntDriven_BasicGrblSglGrbJntT.AnchAtGrblPiv;
    [SerializeField] GrblJntDriven_BasicGrblDblGrbJntT dblGrbJntT = GrblJntDriven_BasicGrblDblGrbJntT.GrbLineAligned;

    [Header("Refs")]
    [SerializeField] GrblJntDriven_GrblCore grblCore;
    [Tooltip("Hand visual used to represent grabbing left hand. \n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject lHandVisProxy;
    [Tooltip("Hand visual used to represent grabbing right hand. \n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject rHandVisProxy;

    // Finite state machine
    [HideInInspector] public Fsm grbJntFsm = new();
    [HideInInspector] public GrblJntDriven_GrblJntSt_DblGrb_GrbLineAligned jntSt_DblGrb_GrbLineAligned;
    [HideInInspector] public GrblJntDriven_GrblJntSt_MultiGrb_SimpleAnchAtPiv jntSt_MultiGrb_SimpleAnchAtPiv;
    [HideInInspector] public GrblJntDriven_GrblJntSt_NoGrb jntSt_NoGrb;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPhysHandPos jntSt_SglGrb_SimpleAnchAtPhysHandPos;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv jntSt_SglGrb_SimpleAnchAtPiv;

    public GrblJntDriven_GrblCore GrblCore => grblCore;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Awake() {
        // Initialize FSM states.
        jntSt_DblGrb_GrbLineAligned = new(this, this);
        jntSt_MultiGrb_SimpleAnchAtPiv = new(this);
        jntSt_NoGrb = new(this);
        jntSt_SglGrb_SimpleAnchAtPhysHandPos = new(this);
        jntSt_SglGrb_SimpleAnchAtPiv = new(this);
    }

    void Start() {
        grbJntFsm.SwitchState(jntSt_NoGrb, this);
    }

    void FixedUpdate() {
        grbJntFsm.CurrentState.PhysicsTick();
    }

    void OnDrawGizmos() {
        GrblUtils.OnDrawGizmos_DrawGrbJntAnchor(this);
    }

    void OnDisable() {
        ReleaseAllGrbs();
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    public bool CanBeGrabbed(GrblJntDriven_PhysHand physHand) {
        // Can be grabbed by up to one left hand and one right hand simultaneously.
        return grblCore.GrbCount(physHand.side) == 0;
    }

    public bool CanBeReleased(GrblJntDriven_PhysHand physHand) {
        return true;
    }

    public float GetDistToGrbPt(Vector3 physHandWorldGrabPoint) {
        return Vector3.Distance(transform.position, physHandWorldGrabPoint);
    }

    public float GetRotHand0Wt() => 0.5f;

    public float GetPosHand0Wt() => 0.5f;

    public void InitiateGrb(GrblJntDriven_PhysHand physHand) {
        // NOTE: Grab point is set to Vector3, because the grab point offset doesn't meaningfully
        // NOTE C: affect the grabbale pose.
        var newGrb = new Grb(
            physHand, 
            MathUtils.UnscaledInvrsTrfPt(transform, physHand.transform.position),
            MathUtils.RotFromWorldToTrfSpace(transform, physHand.transform.rotation),
            Vector3.zero
        );
        grblCore.grbs.Add(newGrb);
        // Setup hand proxy visual.
        if (physHand.side == Side.Left)
            GrblUtils.EnableObjNSetPose(
                lHandVisProxy,
                physHand.transform.position,
                physHand.transform.rotation
            );
        else
            GrblUtils.EnableObjNSetPose(
                rHandVisProxy,
                physHand.transform.position,
                physHand.transform.rotation
            );
        SwitchJntStBasedOnGrbCount();
    }

    public void ReleaseGrb(GrblJntDriven_PhysHand physHand) {
        GrblUtils.LRGrb_ReleaseGrb(this, physHand, lHandVisProxy, rHandVisProxy);
        SwitchJntStBasedOnGrbCount();
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void ReleaseAllGrbs() {
        GrblUtils.LRGrab_ReleaseAllGrbs(this, lHandVisProxy, rHandVisProxy);
        SwitchJntStBasedOnGrbCount();
    }
    
    void SwitchJntStBasedOnGrbCount() {
        IFsmSt nextState = grblCore.grbs.Count switch {
            0 => jntSt_NoGrb,
            1 => sglGrbJntT switch {
                GrblJntDriven_BasicGrblSglGrbJntT.AnchAtGrblPiv =>
                    jntSt_SglGrb_SimpleAnchAtPiv,
                GrblJntDriven_BasicGrblSglGrbJntT.AnchAtPhysHandPos =>
                    jntSt_SglGrb_SimpleAnchAtPhysHandPos,
                _ => throw new System.ArgumentOutOfRangeException(nameof(sglGrbJntT))
            },
            2 => dblGrbJntT switch {
                GrblJntDriven_BasicGrblDblGrbJntT.GrbLineAligned =>
                    jntSt_DblGrb_GrbLineAligned,
                GrblJntDriven_BasicGrblDblGrbJntT.SimpleAnchAtPiv =>
                    jntSt_MultiGrb_SimpleAnchAtPiv,
                _ => throw new System.ArgumentOutOfRangeException(nameof(dblGrbJntT))
            },
            _ => throw new System.ArgumentOutOfRangeException(nameof(grblCore.grbs.Count))
        };
        if (nextState != grbJntFsm.CurrentState)
            grbJntFsm.SwitchState(nextState, this);
    }
}
