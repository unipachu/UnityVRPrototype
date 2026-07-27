using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uses avg target pose based on all grabbing hands.<br/> 
/// NOTE: If you want highly stable movement, the grabbable pivot should equal to the center of
/// mass of the grabbable.
/// </summary>
public class GrabbableJntSt_MultiGrab_SimpleAnchorAtPivot : IFsmSt{
    IGrabbable grabbable;
    public GrabbableJntSt_MultiGrab_SimpleAnchorAtPivot(IGrabbable grabbable) {
        this.grabbable = grabbable;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt previousState) {
        grabbable.GrabJnt.anchor = Vector3.zero;
        PhysHandNGrabbableUtils.SetJntDrivesToAvgPhysHandsDflt(
            grabbable.GrabJnt,
            grabbable.Grabs
        );
        UpdateJnt(grabbable.Grabs, grabbable.GrabJnt);
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grabbable.Grabs, grabbable.GrabJnt);
    }

    public void Tick() {
    }

    // -----------------------------------------
    // Private Methods
    // -----------------------------------------

    public void UpdateJnt(List<Grab> grabs, ConfigurableJoint grabJnt) {
        Vector3 avgTgtWorldPos = Vector3.zero;
        Vector4 cumulative = Vector4.zero;
        // Use the first target rotation as the hemisphere reference.
        Quaternion referenceRot = Quaternion.identity;
        bool first = true;
        foreach (Grab grab in grabs) {
            Transform physHandFollowTgt = grab.physHand.followTgtTrf;
            Quaternion targetWorldRot =
                physHandFollowTgt.rotation *
                Quaternion.Inverse(grab.initRotFromGrabbableToPhysHand);
            Vector3 targetWorldPos =
                physHandFollowTgt.position -
                targetWorldRot * grab.initPhysHandPosInGrabbableLocalSpace;
            avgTgtWorldPos += targetWorldPos;
            if (first) {
                referenceRot = targetWorldRot;
                first = false;
            }
            if (Quaternion.Dot(targetWorldRot, referenceRot) < 0f) {
                targetWorldRot = new Quaternion(
                    -targetWorldRot.x,
                    -targetWorldRot.y,
                    -targetWorldRot.z,
                    -targetWorldRot.w
                );
            }
            cumulative.x += targetWorldRot.x;
            cumulative.y += targetWorldRot.y;
            cumulative.z += targetWorldRot.z;
            cumulative.w += targetWorldRot.w;
        }
        avgTgtWorldPos /= grabs.Count;
        Quaternion avgTgtWorldRot = new Quaternion(
            cumulative.x,
            cumulative.y,
            cumulative.z,
            cumulative.w
        ).normalized;
        grabJnt.targetPosition = avgTgtWorldPos;
        grabJnt.targetRotation = avgTgtWorldRot;
    }
}
