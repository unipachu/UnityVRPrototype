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
        grbl.GrbJnt.anchor = Vector3.zero;
        PhysUtils.SetJntDrivesToDflt(
            grbl.GrbJnt, 
            grbl.Grbs[0].physHand.wldJntData
        );
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grbl.Grbs[0], grbl.GrbJnt);
    }

    public void Tick() {
    }

    // -----------------------------------------
    // Private Methods
    // -----------------------------------------

    void UpdateJnt(GrblJntDriven_Grb grb, ConfigurableJoint grbJnt) {
        // Current hand follow targets.
        Vector3 followTgtGrabPtWorld = MathUtils.TrfPtUnscaled(grb.physHand.followTgtTrf, grb.gnrGrb.theoInitGrbPtInFolTgtSpc);
        Transform physHandFollowTgt = grb.physHand.followTgtTrf;
        Quaternion tgtWorldRot =
            physHandFollowTgt.rotation * Quaternion.Inverse(grb.gnrGrb.initRotFromGrblToPhysHand);
        Vector3 targetWorldPos = followTgtGrabPtWorld - tgtWorldRot * grb.gnrGrb.initPhysHandPosInGrblSpc;
        grbJnt.targetPosition = targetWorldPos;
        grbJnt.targetRotation = tgtWorldRot;
    }
}
