using UnityEngine;

// TODO: Give this a better name. And a summary.
public class GrblJntDriven_GrblJntSt_DblGrb_AxisRot : IFsmSt {
    IGrbl grbl;

    public GrblJntDriven_GrblJntSt_DblGrb_AxisRot(IGrbl grbl) {
        this.grbl = grbl;
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
            grbl.Grbs
        );
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grbl.Grbs[0], grbl.Grbs[1], grbl.GrbJnt);
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
    public void UpdateJnt(Grb grb0, Grb grb1, ConfigurableJoint grbJnt, float rotHand0Wt = 0.5f, float posHand0Wt = 0.5f) {
        float rotHand1Wt = 1f - rotHand0Wt;
        float posHand1Wt = 1f - posHand0Wt;
        // Initial grab positions in grabbable local space.
        Vector3 initLocalPos0 = grb0.initPhysHandPosInGrblLocalSpace;
        Vector3 initLocalPos1 = grb1.initPhysHandPosInGrblLocalSpace;
        // Current hand follow targets.
        Transform followTgt0 = grb0.physHand.followTgtTrf;
        Transform followTgt1 = grb1.physHand.followTgtTrf;
        // Current line between hands.
        Vector3 tgtWorldLine = followTgt1.position - followTgt0.position;
        // This is a faster and more rounding safe way to check if vector magnitude is 0.
        if (tgtWorldLine.sqrMagnitude < 1e-8f)
            return;
        tgtWorldLine.Normalize();
        // Initial line between grab points.
        Vector3 initLine = initLocalPos1 - initLocalPos0;
        if (initLine.sqrMagnitude < 1e-8f)
            return;
        initLine.Normalize();
        // Align the initial grab line with the current grab line.
        // TODO: How does this handle 180 rotations?
        Quaternion lineAlignRot = Quaternion.FromToRotation(initLine, tgtWorldLine);
        // Rotation from grab on the grabble to the corresponding follow target.
        Quaternion desiredRot0 =
            followTgt0.rotation * Quaternion.Inverse(grb0.initRotFromGrblToPhysHand);
        Quaternion desiredRot1 =
            followTgt1.rotation * Quaternion.Inverse(grb1.initRotFromGrblToPhysHand);
        // This is a bit hard to understand but below equation makes it so that also:
        // desiredRot (the hand wants to rotate the grabbable) = twistResidual * lineAlignRot
        // Line align rot is only the rotation that aligns the object with tgtWorldLine,
        // then twistResidual is the remaining rotation to reach hand desired rot.
        // TODO: I do not understand this.
        Quaternion twistResidual0 = desiredRot0 * Quaternion.Inverse(lineAlignRot);
        Quaternion twistResidual1 = desiredRot1 * Quaternion.Inverse(lineAlignRot);
        float twistAngle1 = MathUtils.ExtractSignedTwistAng(twistResidual0, tgtWorldLine);
        float twistAngle2 = MathUtils.ExtractSignedTwistAng(twistResidual1, tgtWorldLine);
        // Twist on the unit circle.
        float avgTwistRad = Mathf.Atan2(
            rotHand0Wt * Mathf.Sin(twistAngle1) +
            rotHand1Wt * Mathf.Sin(twistAngle2),
            rotHand0Wt * Mathf.Cos(twistAngle1) +
            rotHand1Wt * Mathf.Cos(twistAngle2)
        );
        Quaternion avgTwist = Quaternion.AngleAxis(avgTwistRad * Mathf.Rad2Deg, tgtWorldLine);
        Quaternion newTgtWorldRot = avgTwist * lineAlignRot;
        //// Compute target world position from both grab points and average.
        //Vector3 posFromGrab1 = followTgt1.position - newTgtWorldRot * initLocalPos1;
        //Vector3 posFromGrab2 = followTgt2.position - newTgtWorldRot * initLocalPos2;
        //Vector3 newTgtWorldPos =
        //    rotHand1Wt * posFromGrab1 + hand2Wt * posFromGrab2;
        // Compute the world position implied by each grab point.
        Vector3 posFromGrab0 =
            followTgt0.position - newTgtWorldRot * initLocalPos0;
        Vector3 posFromGrab1 =
            followTgt1.position - newTgtWorldRot * initLocalPos1;
        // Blend between the two positions independently from rotation weighting.
        Vector3 newTgtWorldPos =
            posHand0Wt * posFromGrab0 +
            posHand1Wt * posFromGrab1;
        grbJnt.targetPosition = newTgtWorldPos;
        grbJnt.targetRotation = newTgtWorldRot;
    }
}
