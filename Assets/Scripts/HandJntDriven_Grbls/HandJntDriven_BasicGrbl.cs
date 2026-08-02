using System.Collections.Generic;
using UnityEngine;

public class HandJntDriven_BasicGrbl : MonoBehaviour, IHandJntDriven_Grbl, IDblGrb_GrbLineAlignable {
    [Header("Refs")]
    [SerializeField] Rigidbody rb;

    Fsm grbJntFsm = new();
    St_NoOp jntSt_NoGrb;
    HandJntDriven_JntSt_SglGrb jntSt_SglGrb;
    HandJntDriven_JntSt_DblGrb jntSt_DblGrb;
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
        jntSt_DblGrb = new(this);
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
        physHand.grbJnt.connectedAnchor = MathUtils.InvrsTrfPtUnscaled(transform, physHand.transform.position);
        PhysUtils.SetJntMotCstrsToLocked(physHand.grbJnt);
        grbs.Add(
            new HandJntDriven_Grb(
                physHand,
                new GnrGrbData(
                    physHand,
                    MathUtils.InvrsTrfPtUnscaled(transform, physHand.transform.position),
                    MathUtils.InvrsTrfRot(transform, physHand.transform.rotation),
                    // NOTE: Same as above, we use as the sphere cast postition as the grab joint connected anchor.
                    Vector3.zero
                )
            )
        );
        SwitchJntStBasedOnGrbCount();
    }

    public void ReleaseGrb(HandJntDriven_PhysHand physHand) {
        GrblUtils.HandJntDriven_LRGrb_ReleaseGrb(this, physHand);
        SwitchJntStBasedOnGrbCount();
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void ReleaseAllGrbs() {
        GrblUtils.HandJntDriven_ReleaseAllGrbs(this);
        SwitchJntStBasedOnGrbCount();
    }

    void SwitchJntStBasedOnGrbCount() {
        IFsmSt nextState = FindJntStBasedOnGrbCount();
        if (nextState != grbJntFsm.CurSt)
            grbJntFsm.SwitchState(nextState, this);
    }

    private IFsmSt FindJntStBasedOnGrbCount() {
        return grbs.Count switch {
            0 => jntSt_NoGrb,
            1 => jntSt_SglGrb,
            2 => jntSt_DblGrb,
            _ => throw new System.ArgumentOutOfRangeException(nameof(grbs.Count))
        };
    }
}
