using System.Collections.Generic;
using UnityEngine;

public class GrblJntDriven_KeyGrbl : MonoBehaviour, IGnrGrbl, IGrblJntDriven_Grbl, IKeyholeSnpl {
    [Header("Refs")]
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
    [SerializeField] ConfigurableJoint grbJnt;
    [SerializeField] Rigidbody rb;

    [HideInInspector] public Fsm grbJntFsm = new();
    [HideInInspector] public GrblJntDriven_GrblJntSt_NoGrb jntSt_NoGrb;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv jntSt_SglGrb_SimpleAnchAtPiv;

    readonly List<GrblJntDriven_Grb> grbs = new(2);
    GrblJntDriven_Grbs gnrGrbs;
    ISnpTgt snapped = null;
    /// <summary>
    /// We use snap cooldown since when we turn rigidbody from kinematic to dynamic,
    /// the rigidbody can trigger OnTriggerEntered from triggers it was already overlapping with.
    /// </summary>
    float snpCooldown = 0;

    public IGnrGrbl GnrGrbl => this;
    public IGnrGrbsCtrl GnrGrbs => gnrGrbs;
    public ConfigurableJoint GrbJnt => grbJnt;
    public IGnrGrbl GnrGrblData => this;
    public List<GrblJntDriven_Grb> Grbs => grbs;
    public Vector3 KeyTipLclPos => keyTipLclPos;
    public Rigidbody Rb => rb;
    public Transform Trf => transform;


    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Awake() {
        // Initialize FSM states.
        jntSt_NoGrb = new(this);
        jntSt_SglGrb_SimpleAnchAtPiv = new(this);
        gnrGrbs = new(grbs);
    }

    void Start() {
        grbJntFsm.SwitchState(jntSt_NoGrb, this);
    }

    void FixedUpdate() {
        if (snapped == null)
            grbJntFsm.CurSt.PhysicsTick();
    }

    void Update() {
        if (snpCooldown > 0)
            snpCooldown -= Time.deltaTime;
    }

    void OnDrawGizmos() {
        ObjUtils.OnDrawGizmos_DrawJntAnch(grbJnt);
    }

    void OnDisable() {
        ReleaseAllGrbs();
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    public bool CanBeGrabbed(GrblJntDriven_PhysHand physHand) => true;

    public bool CanBeReleased(GrblJntDriven_PhysHand physHand) => true;

    public bool CanSnp() {
        return 
            snapped == null && 
            snpCooldown <= 0 && 
            grbs.Count != 0;
    }

    public float GetDistToGrbPt(Vector3 physHandWorldGrbPt) {
        return Vector3.Distance(transform.position, physHandWorldGrbPt);
    }

    public void OnInitGrb(GrblJntDriven_PhysHand physHand) {
        // Only one grabber can grab this at a time. Thus release any previous grab.
        GrblUtils.LRGrb_ReleaseAllGrbs(this, lHandVisProxy, rHandVisProxy);
        var newGrb = new GrblJntDriven_Grb(
            physHand,
            new GnrGrbData(
                physHand,
                MathUtils.InvrsTrfPtUnscaled(transform, physHand.transform.position),
                MathUtils.InvrsTrfRot(transform, physHand.transform.rotation),
                Vector3.zero
            )
        );
        grbs.Add(newGrb);
        // Setup hand proxy visual.
        GameObject proxy = physHand.handSide == Side.Left ? lHandVisProxy : rHandVisProxy;
        ObjUtils.ActivateNSetPose(proxy, physHand.transform.position, physHand.transform.rotation);
        SwitchJntStBasedOnGrbCount();
    }

    public void InitSnp(ISnpTgt snpTgt) {
        snapped = snpTgt;
        rb.isKinematic = true;
    }

    public void OnEndSnp() {
        snapped = null;
        rb.isKinematic = false;
        snpCooldown = 0.3f;
    }

    public void ReleaseGrb(GrblJntDriven_PhysHand physHand) {
        GrblUtils.LRGrb_ReleaseGrb(this, physHand, lHandVisProxy, rHandVisProxy);
        SwitchJntStBasedOnGrbCount();
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void ReleaseAllGrbs() {
        GrblUtils.LRGrb_ReleaseAllGrbs(this, lHandVisProxy, rHandVisProxy);
        SwitchJntStBasedOnGrbCount();
    }

    void SwitchJntStBasedOnGrbCount() {
        IFsmSt nextState = grbs.Count switch {
            0 => jntSt_NoGrb,
            1 => jntSt_SglGrb_SimpleAnchAtPiv,
            _ => throw new System.ArgumentOutOfRangeException(nameof(grbs.Count))
        };
        if (nextState != grbJntFsm.CurSt)
            grbJntFsm.SwitchState(nextState, this);
    }
}
