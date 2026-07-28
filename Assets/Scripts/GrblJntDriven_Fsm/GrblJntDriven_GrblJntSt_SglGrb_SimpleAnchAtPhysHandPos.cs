using UnityEngine;

/// <summary>
/// Sets the grab joint anchor at the position where the <see cref="GrblJntDriven_PhysHand"/>
/// originally grabbed the grabbable.
/// </summary>
public class GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPhysHandPos : IFsmSt {
    IGrbl grbl;

    public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPhysHandPos(IGrbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt previousState) {
        grbl.GrbJnt.anchor = new Vector3(
            grbl.Grbs[0].initPhysHandPosInGrblLocalSpace.x / grbl.GrblGameObj.transform.lossyScale.x,
            grbl.Grbs[0].initPhysHandPosInGrblLocalSpace.y / grbl.GrblGameObj.transform.lossyScale.y,
            grbl.Grbs[0].initPhysHandPosInGrblLocalSpace.z / grbl.GrblGameObj.transform.lossyScale.z
        );
        PhysUtils.SetJntDrivesToDflt(
            grbl.GrbJnt,
            grbl.Grbs[0].physHand.jntData
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

    void UpdateJnt(Grb grb, ConfigurableJoint grbJnt) {
        Transform physHandFollowTgt = grb.physHand.followTgtTrf;
        Quaternion tgtWorldRot =
            physHandFollowTgt.rotation * Quaternion.Inverse(grb.initRotFromGrblToPhysHand);
        Vector3 targetWorldPos = physHandFollowTgt.position;
        grbJnt.targetPosition = targetWorldPos;
        grbJnt.targetRotation = tgtWorldRot;
    }
}
