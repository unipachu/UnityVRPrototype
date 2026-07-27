using UnityEngine;

/// <summary>
/// NOTE: If you want highly stable movement, the grabbable pivot should equal to the center of
/// mass of the grabbable.
/// </summary>
public class GrabbableJntSt_SingleGrab_SimpleAnchorAtPivot : IFsmSt {
    IGrabbable grabbable;

    public GrabbableJntSt_SingleGrab_SimpleAnchorAtPivot(IGrabbable grabbable) {
        this.grabbable = grabbable;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt previousState) {
        grabbable.GrabJnt.anchor = Vector3.zero;
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
        Vector3 targetWorldPos =
            physHandFollowTgt.position - targetWorldRot * grab.initPhysHandPosInGrabbableLocalSpace;
        grabJnt.targetPosition = targetWorldPos;
        grabJnt.targetRotation = targetWorldRot;
    }
}
