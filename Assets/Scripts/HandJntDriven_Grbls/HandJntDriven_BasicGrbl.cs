using System.Collections.Generic;
using UnityEngine;

public class HandJntDriven_BasicGrbl : MonoBehaviour, IHandJntDriven_Grbl, IDblGrb_GrbLineAlignable {
    [Header("Refs")]
    [Tooltip("Hand visual used to represent grabbing left hand. \n" +
    "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject lHandVisProxy;
    [Tooltip("Hand visual used to represent grabbing right hand. \n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject rHandVisProxy;
    [SerializeField] Rigidbody rb;

    public Fsm grbJntFsm = new();
    public St_NoOp jntSt_NoGrb;
    public HandJntDriven_JntSt_SglGrb jntSt_SglGrb;
    List<HandJntDriven_Grb> grbs = new(2);
    HandJntDriven_Grbs gnrGrbs;

    public List<HandJntDriven_Grb> Grbs => grbs;
    public IGnrGrbl GnrGrbl => this;
    public IGnrGrbsCtrl GnrGrbs => gnrGrbs;
    public Rigidbody Rb => rb;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    private void Awake() {
        //jntSt_DblGrb_GrbLineAligned = new(this, this);
        jntSt_NoGrb = new();
        jntSt_SglGrb = new(this);
        gnrGrbs = new(grbs);
    }
    void Start() {
        grbJntFsm.SwitchState(jntSt_NoGrb, this);
    }

    void FixedUpdate() {
        grbJntFsm.CurSt.PhysicsTick();
    }

    void OnDisable() {
        ReleaseAllGrbs();
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    public bool CanBeGrabbed(HandJntDriven_PhysHand physHand)
        // Max one hand per side
        => GrblUtils.SidedGrbCount<HandJntDriven_Grb, HandJntDriven_PhysHand>(
            grbs,
            physHand.handSide) == 0;

    public bool CanBeReleased(HandJntDriven_PhysHand physHand) => true;

    public float GetDistToGrbPt(Vector3 physHandWldGrbPt) 
        => Vector3.Distance(transform.position, physHandWldGrbPt);

    public float GetRotHand0Wt() => 0.5f;

    public float GetPosHand0Wt() => 0.5f;

    public void OnInitGrb(HandJntDriven_PhysHand physHand) {
        // NOTE: It might seem weird to set these here instead of in PhysHand, but these settings might vary
        // NOTE C: based on the grabbable so I think it's best to set them here.
        physHand.grbJnt.connectedBody = rb;
        // NOTE: Connected anchor is relative to connected rigidbody.
        physHand.grbJnt.connectedAnchor = rb.transform.InverseTransformPoint(physHand.grblSearchPos.position);
        PhysUtils.SetJntMotCstrsToLocked(physHand.grbJnt);
        grbs.Add(
            new HandJntDriven_Grb(
                physHand,
                new GnrGrbData(
                    physHand,
                    MathUtils.InvrsTrfPtUnscaled(transform, physHand.transform.position),
                    MathUtils.InvrsTrfRot(transform, physHand.transform.rotation),
                    // NOTE: Same as above, we use as the sphere cast postition as the grab joint connected anchor.
                    physHand.grblSearchPos.position
                )
            )
        );
        // Setup hand proxy visual.
        if (physHand.handSide == Side.Left)
            ObjUtils.ActivateNSetPose(
                lHandVisProxy,
                physHand.transform.position,
                physHand.transform.rotation
            );
        else
            ObjUtils.ActivateNSetPose(
                rHandVisProxy,
                physHand.transform.position,
                physHand.transform.rotation
            );
        SwitchJntStBasedOnGrbCount();
    }

    public void ReleaseGrb(HandJntDriven_PhysHand physHand) {
        GrblUtils.LRGrb_ReleaseGrb(this, physHand, lHandVisProxy, rHandVisProxy);
        SwitchJntStBasedOnGrbCount();
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void ReleaseAllGrbs() {
        GrblUtils.LRGrb_ReleaseAllGrbs(this, lHandVisProxy, rHandVisProxy);
        //SwitchJntStBasedOnGrbCount();
    }

    void SwitchJntStBasedOnGrbCount() {
        IFsmSt nextState = FindJntStBasedOnGrbCount();
        if (nextState != grbJntFsm.CurSt)
            grbJntFsm.SwitchState(nextState, this);
    }

    private IFsmSt FindJntStBasedOnGrbCount() {
        IFsmSt nextState = grbs.Count switch {
            0 => jntSt_NoGrb,
            1 => jntSt_SglGrb,
            //2 => jntSt_DblGrb_GrbLineAligned,
            _ => throw new System.ArgumentOutOfRangeException(nameof(grbs.Count))
        };
        return nextState;
    }
}
