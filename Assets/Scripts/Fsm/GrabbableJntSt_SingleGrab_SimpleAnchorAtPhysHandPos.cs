using UnityEngine;

public class GrabbableJntSt_SingleGrab_SimpleAnchorAtPhysHandPos : IFsmSt {
    IGrabbable grabbable;

    public GrabbableJntSt_SingleGrab_SimpleAnchorAtPhysHandPos(IGrabbable grabbable) {
        this.grabbable = grabbable;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt previousState) {
        grabbable.GrabJnt.anchor = new Vector3(
            grabbable.Grabs[0].initPhysHandPosInGrabbableLocalSpace.x / grabbable.GrabbableGameObj.transform.lossyScale.x,
            grabbable.Grabs[0].initPhysHandPosInGrabbableLocalSpace.y / grabbable.GrabbableGameObj.transform.lossyScale.y,
            grabbable.Grabs[0].initPhysHandPosInGrabbableLocalSpace.z / grabbable.GrabbableGameObj.transform.lossyScale.z
        );
        PhysHandNGrabbableUtils.SetJntDrivesToDflt(
            grabbable.GrabJnt,
            grabbable.Grabs[0].physHand.jntData
        );
        UpdateJnt(grabbable.Grabs[0], grabbable.GrabJnt);
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grabbable.Grabs[0], grabbable.GrabJnt);
    }

    public void Tick() {
    }

    // -----------------------------------------
    // Private Methods
    // -----------------------------------------

    void UpdateJnt(Grab grab, ConfigurableJoint grabJnt) {
        Transform physHandFollowTgt = grab.physHand.followTgtTrf;
        Quaternion targetWorldRot =
            physHandFollowTgt.rotation * Quaternion.Inverse(grab.initRotFromGrabbableToPhysHand);
        Vector3 targetWorldPos = physHandFollowTgt.position;
        grabJnt.targetPosition = targetWorldPos;
        grabJnt.targetRotation = targetWorldRot;
    }
}
