using UnityEngine;

/// <summary>
/// Sets the grab joint anchor at the position where the <see cref="GrblJntDriven_PhysHand"/>
/// originally grabbed the grabbable.
/// </summary>
public class GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPhysHandPos : IFsmSt {
    IGrblJntDriven_Grbl grbl;

    public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPhysHandPos(IGrblJntDriven_Grbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt previousState) {
        grbl.GrbJnt.anchor = new Vector3(
            grbl.Grbs[0].gnrGrb.initPhysHandPosInGrblSpc.x / grbl.Rb.transform.lossyScale.x,
            grbl.Grbs[0].gnrGrb.initPhysHandPosInGrblSpc.y / grbl.Rb.transform.transform.lossyScale.y,
            grbl.Grbs[0].gnrGrb.initPhysHandPosInGrblSpc.z / grbl.Rb.transform.transform.lossyScale.z
        );
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
        Transform physHandFollowTgt = grb.physHand.followTgtTrf;
        Quaternion tgtWorldRot =
            physHandFollowTgt.rotation * Quaternion.Inverse(grb.gnrGrb.initRotFromGrblToPhysHand);
        Vector3 targetWorldPos = physHandFollowTgt.position;
        grbJnt.targetPosition = targetWorldPos;
        grbJnt.targetRotation = tgtWorldRot;
    }
}
