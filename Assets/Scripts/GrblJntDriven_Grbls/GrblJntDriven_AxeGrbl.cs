using UnityEngine;

public class GrblJntDriven_AxeGrbl : MonoBehaviour, IGrblJntDriven_Grbl, IDblGrb_GrbLineAligned {
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
    [HideInInspector] public GrblJntDriven_GrblJntSt_NoGrb jntSt_NoGrb;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv jntSt_SglGrb_SimpleAnchAtPiv;

    public GrblJntDriven_GrblCore GrblCore => grblCore;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Awake() {
        // Initialize FSM states.
        jntSt_DblGrb_GrbLineAligned = new(this, this);
        jntSt_NoGrb = new(this);
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
        return grblCore.GrbCount(physHand.side) < 2;
    }

    public bool CanBeReleased(GrblJntDriven_PhysHand physHand) {
        return true;
    }

    public float GetDistToGrbPt(Vector3 physHandWorldGrbPt) {
        return Vector3.Distance(transform.position, physHandWorldGrbPt);
    }

    public float GetPosHand0Wt() {
        // Check which initial is higher on local Y axis.
        float grb0LocalHght = grblCore.grbs[0].initPhysHandPosInGrblLocalSpace.y;
        float grb1LocalHght = grblCore.grbs[1].initPhysHandPosInGrblLocalSpace.y;
        if (grb0LocalHght < grb1LocalHght)
            return 1;
        return 0;
    }

    public float GetRotHand0Wt() => 0.5f;

    public void InitiateGrb(GrblJntDriven_PhysHand physHand) {
        Vector3 grabLocalPos = MathUtils.UnscaledInvrsTrfPt(transform, physHand.transform.position);
        // We want the grab to always align with the axe handle.
        // TODO: actually we want the grab point align with the handle and then use grab point
        // TODO C: to calculate everything. I made a mistake by using just the physicsHand position
        // TODO C: for grab calculations. Also grab point and grab overlapsphere pos should be separate.
        // TODO C: Except... different grabbables might want to attach to different points of the hand.
        // TODO C: Hmm... Maybe grabbables should just have some sort of hard coded offset for the
        // TODO C: grabbable-specific grab point.
        grabLocalPos = new Vector3(0, grabLocalPos.y, 0);
        var newGrb = new Grb(
            physHand,
            grabLocalPos,
            MathUtils.RotFromWorldToTrfSpace(transform, physHand.transform.rotation)
        );
        grblCore.grbs.Add(newGrb);
        // Setup hand proxy visual.
        if (physHand.side == Side.Left)
            GrblUtils.EnableProxyHand(
                lHandVisProxy,
                MathUtils.UnscaledTrfPt(transform, grabLocalPos),
                lHandVisProxy.transform.rotation
            );
        else
            GrblUtils.EnableProxyHand(
                rHandVisProxy,
                MathUtils.UnscaledTrfPt(transform, grabLocalPos),
                rHandVisProxy.transform.rotation
            );
        SwitchJntStBasedOnGrbCount();
    }

    void ReleaseAllGrbs() {
        GrblUtils.LRGrab_ReleaseAllGrbs(this, lHandVisProxy, rHandVisProxy);
        SwitchJntStBasedOnGrbCount();
    }

    public void ReleaseGrb(GrblJntDriven_PhysHand physHand) {
        GrblUtils.LRGrb_ReleaseGrb(this, physHand, lHandVisProxy, rHandVisProxy);
        SwitchJntStBasedOnGrbCount();
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------
    
    void SwitchJntStBasedOnGrbCount() {
        IFsmSt nextState = grblCore.grbs.Count switch {
            0 => jntSt_NoGrb,
            1 => jntSt_SglGrb_SimpleAnchAtPiv,
            2 => jntSt_DblGrb_GrbLineAligned,
            _ => throw new System.ArgumentOutOfRangeException(nameof(grblCore.grbs.Count))
        };
        if (nextState != grbJntFsm.CurrentState)
            grbJntFsm.SwitchState(nextState, this);
    }
}
