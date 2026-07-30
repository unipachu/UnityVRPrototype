using UnityEngine;

public class GrblJntDriven_KeyGrbl : MonoBehaviour, IGrblJntDriven_Grbl, IKeyholeSnpl {
    [Header("Refs")]
    [SerializeField] GrblJntDriven_GrblCore grblCore;
    [Tooltip("Hand visual used to represent grabbing left hand.\n" +
    "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject lHandVisProxy;
    [Tooltip("Hand visual used to represent grabbing right hand.\n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject rHandVisProxy;
    [Tooltip("Position used to orient key when snapped to keyhole.\n" +
        "NOTE: We use Vector3 instead of a Transform reference since Transform positions cannot be safely " +
        "used with Rigidbodies because they can get out of sync.")]
    [SerializeField] Vector3 keyTipLclPos;

    // Finite state machine
    [HideInInspector] public Fsm grbJntFsm = new();
    [HideInInspector] public GrblJntDriven_GrblJntSt_NoGrb jntSt_NoGrb;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv jntSt_SglGrb_SimpleAnchAtPiv;

    ISnapTgt snapped = null;
    /// <summary>
    /// We use snap cooldown since when we turn rigidbody from kinematic to dynamic,
    /// the rigidbody can trigger OnTriggerEntered from triggers it was already overlapping with.
    /// </summary>
    float snpCooldown = 0;

    public GrblJntDriven_GrblCore GrblCore => grblCore;
    public Vector3 KeyTipLclPos => keyTipLclPos;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Awake() {
        // Initialize FSM states.
        jntSt_NoGrb = new(this);
        jntSt_SglGrb_SimpleAnchAtPiv = new(this);
    }

    void Start() {
        grbJntFsm.SwitchState(jntSt_NoGrb, this);
    }

    void FixedUpdate() {
        if (snapped == null)
            grbJntFsm.CurrentState.PhysicsTick();
    }

    void Update() {
        if (snpCooldown > 0)
            snpCooldown -= Time.deltaTime;
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
        return true;
    }

    public bool CanBeReleased(GrblJntDriven_PhysHand physHand) {
        return true;
    }

    public bool CanSnp() {
        return 
            snapped == null && 
            snpCooldown <= 0 && 
            grblCore.grbs.Count != 0;
    }

    public void OnEndSnp() {
        snapped = null;
        GrblCore.rb.isKinematic = false;
        snpCooldown = 0.3f;
    }

    public float GetDistToGrbPt(Vector3 physHandWorldGrbPt) {
        return Vector3.Distance(transform.position, physHandWorldGrbPt);
    }

    public void InitiateGrb(GrblJntDriven_PhysHand physHand) {
        // Only one grabber can grab this at a time. Thus release any previous grab.
        GrblUtils.LRGrab_ReleaseAllGrbs(this, lHandVisProxy, rHandVisProxy);
        var newGrb = new Grb(
            physHand,
            MathUtils.InvrsTrfPtUnscaled(transform, physHand.transform.position),
            MathUtils.InvrsTrfRot(transform, physHand.transform.rotation),
            Vector3.zero
        );
        grblCore.grbs.Add(newGrb);
        // Setup hand proxy visual.
        GameObject proxy = physHand.side == Side.Left ? lHandVisProxy : rHandVisProxy;
        ObjUtils.ActivateNSetPose(proxy, physHand.transform.position, physHand.transform.rotation);
        SwitchJntStBasedOnGrbCount();
    }

    public void InitSnp(ISnapTgt snpTgt) {
        snapped = snpTgt;
        GrblCore.rb.isKinematic = true;
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
            1 => jntSt_SglGrb_SimpleAnchAtPiv,
            _ => throw new System.ArgumentOutOfRangeException(nameof(grblCore.grbs.Count))
        };
        if (nextState != grbJntFsm.CurrentState)
            grbJntFsm.SwitchState(nextState, this);
    }
}
