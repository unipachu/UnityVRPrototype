using UnityEngine;

/// <summary>
/// Keeps the grab joint anchor at the grabbable pivot.<br/>
/// NOTE: If you want highly stable movement, the grabbable pivot should equal to the center of
/// mass of the grabbable.
/// </summary>
public class GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv : IFsmSt {
    IGrblJntDriven_Grbl grbl;

    public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv(IGrblJntDriven_Grbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt prevSt) {
        // TODO: You could set the anchor to the grabbable COM but then for joint drive calculations
        // TODO C: you need to offset targets based on that.
        grbl.GrblCore.grbJnt.anchor = Vector3.zero;
        PhysUtils.SetJntDrivesToDflt(
            grbl.GrblCore.grbJnt, 
            grbl.GrblCore.grbs[0].physHand.jntData
        );
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grbl.GrblCore.grbs[0], grbl.GrblCore.grbJnt);
    }

    public void Tick() {
    }

    // -----------------------------------------
    // Private Methods
    // -----------------------------------------

    void UpdateJnt(Grb grb, ConfigurableJoint grbJnt) {
        // Current hand follow targets.
        Vector3 followTgtGrabPtWorld = MathUtils.TrfPtUnscaled(grb.physHand.followTgtTrf, grb.followTgtInitGrabPtInFollowTgtSpc);
        Transform physHandFollowTgt = grb.physHand.followTgtTrf;
        Quaternion tgtWorldRot =
            physHandFollowTgt.rotation * Quaternion.Inverse(grb.initRotFromGrblToPhysHand);
        Vector3 targetWorldPos = followTgtGrabPtWorld - tgtWorldRot * grb.initPhysHandPosInGrblSpc;
        grbJnt.targetPosition = targetWorldPos;
        grbJnt.targetRotation = tgtWorldRot;
    }
}
