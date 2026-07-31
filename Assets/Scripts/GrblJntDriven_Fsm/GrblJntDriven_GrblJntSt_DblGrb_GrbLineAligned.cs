using UnityEngine;

/// <summary>
/// Orients grabbable according to a line between the positions two grabbing hands and then uses
/// hands' rotation to determine twist along the grab line.
/// </summary>
public class GrblJntDriven_GrblJntSt_DblGrb_GrbLineAligned : IFsmSt {
    IGrblJntDriven_Grbl grbl;
    IDblGrb_GrbLineAligned dblGrb_GrbLineAligned;

    public GrblJntDriven_GrblJntSt_DblGrb_GrbLineAligned(IGrblJntDriven_Grbl grbl, IDblGrb_GrbLineAligned dblGrb_GrbLineAligned) {
        this.grbl = grbl;
        this.dblGrb_GrbLineAligned = dblGrb_GrbLineAligned;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt prevSt) {
        grbl.GrbJnt.anchor = Vector3.zero;
        // NOTE: A better system would be to use different hand drives to every frame calculate
        // NOTE C: weights for how much each hand affects the grab joint target pose, but this
        // NOTE C: is just a "simple" multi grab system.
        PhysUtils.SetJntDrivesToAvgPhysHandsDflt(
            grbl.GrbJnt,
            grbl.GrblCore.grbs
        );
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(
            grbl.GrblCore.grbs[0],
            grbl.GrblCore.grbs[1],
            grbl.GrbJnt,
            dblGrb_GrbLineAligned.GetPosHand0Wt(),
            dblGrb_GrbLineAligned.GetRotHand0Wt()
        );
    }

    public void Tick() {
    }

    // -----------------------------------------
    // Private Methods
    // -----------------------------------------

    // TODO: What does AngleAxis do with 180 angles?
    /// <summary>
    /// NOTE: hand1Wt should be 0-1. It decides how hands' rotation affects grabbable twist around the axis between the hands.
    /// </summary>
    public void UpdateJnt(GrblJntDriven_Grb grb0, GrblJntDriven_Grb grb1, ConfigurableJoint grbJnt, float posHand0Wt = 0.5f, float rotHand0Wt = 0.5f) {
        float posHand1Wt = 1f - posHand0Wt;
        float rotHand1Wt = 1f - rotHand0Wt;
        // Initial grab positions in grabbable local space.
        Vector3 initLocalPos0 = grb0.gnrGrb.initPhysHandPosInGrblSpc;
        Vector3 initLocalPos1 = grb1.gnrGrb.initPhysHandPosInGrblSpc;
        // Current hand follow targets.
        Transform followTgt0 = grb0.physHand.followTgtTrf;
        Transform followTgt1 = grb1.physHand.followTgtTrf;
        // Follow targets (hand controllers') grab point.
        Vector3 followTgtGrabPtWorld0 = MathUtils.TrfPtUnscaled(followTgt0, grb0.gnrGrb.theoInitGrbPtInFolTgtSpc);
        Vector3 followTgtGrabPtWorld1 = MathUtils.TrfPtUnscaled(followTgt1, grb1.gnrGrb.theoInitGrbPtInFolTgtSpc);
        // Current line between hands.
        Vector3 tgtWldLine = followTgtGrabPtWorld1 - followTgtGrabPtWorld0;
        // This is a faster and more rounding safe way to check if vector magnitude is 0.
        if (tgtWldLine.sqrMagnitude < 1e-8f)
            return;
        tgtWldLine.Normalize();
        // Initial line between grab points.
        Vector3 initLine = initLocalPos1 - initLocalPos0;
        if (initLine.sqrMagnitude < 1e-8f)
            return;
        initLine.Normalize();
        // Align the initial grab line with the current grab line.
        // TODO: How does this handle 180 rotations?
        Quaternion lineAlignRot = Quaternion.FromToRotation(initLine, tgtWldLine);
        // Rotation from grab on the grabble to the corresponding follow target.
        Quaternion desiredRot0 = MathUtils.DeltaRot(grb0.gnrGrb.initRotFromGrblToPhysHand, followTgt0.rotation);
        Quaternion desiredRot1 = MathUtils.DeltaRot(grb1.gnrGrb.initRotFromGrblToPhysHand, followTgt1.rotation);
        float twistAngle1 = MathUtils.ExtractSignedTwistAng(lineAlignRot, desiredRot0, tgtWldLine);
        float twistAngle2 = MathUtils.ExtractSignedTwistAng(lineAlignRot, desiredRot1, tgtWldLine);
        // Twist on the unit circle.
        float avgTwistRad = MathUtils.AvgAngRad(twistAngle1, rotHand0Wt, twistAngle2, rotHand1Wt);
        Quaternion avgTwist = Quaternion.AngleAxis(avgTwistRad * Mathf.Rad2Deg, tgtWldLine);
        Quaternion newTgtWorldRot = MathUtils.AddRotOfs(lineAlignRot, avgTwist);        
        // Compute the world position implied by each grab point.
        Vector3 posFromGrab0 =
            MathUtils.AlignLclPtToWldPt(followTgtGrabPtWorld0, newTgtWorldRot, initLocalPos0);
        Vector3 posFromGrab1 =
            MathUtils.AlignLclPtToWldPt(followTgtGrabPtWorld1, newTgtWorldRot, initLocalPos1);
        // Blend between the two positions independently from rotation weighting.
        Vector3 newTgtWorldPos = posHand0Wt * posFromGrab0 + posHand1Wt * posFromGrab1;
        grbJnt.targetPosition = newTgtWorldPos;
        grbJnt.targetRotation = newTgtWorldRot;
    }
}
