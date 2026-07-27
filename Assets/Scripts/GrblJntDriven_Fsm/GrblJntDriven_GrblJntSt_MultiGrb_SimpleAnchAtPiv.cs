using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uses avg target pose based on all grabbing hands.<br/> 
/// NOTE: If you want highly stable movement, the grabbable pivot should equal to the center of
/// mass of the grabbable.
/// </summary>
public class GrblJntDriven_GrblJntSt_MultiGrb_SimpleAnchAtPiv : IFsmSt{
    IGrbl grbl;
    public GrblJntDriven_GrblJntSt_MultiGrb_SimpleAnchAtPiv(IGrbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt prevSt) {
        grbl.GrbJnt.anchor = Vector3.zero;
        PhysUtils.SetJntDrivesToAvgPhysHandsDflt(
            grbl.GrbJnt,
            grbl.Grbs
        );
        UpdateJnt(grbl.Grbs, grbl.GrbJnt);
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grbl.Grbs, grbl.GrbJnt);
    }

    public void Tick() {
    }

    // -----------------------------------------
    // Private Methods
    // -----------------------------------------

    public void UpdateJnt(List<Grb> grabs, ConfigurableJoint grabJnt) {
        Vector3 avgTgtWorldPos = Vector3.zero;
        Vector4 cumulative = Vector4.zero;
        // Use the first target rotation as the hemisphere reference.
        // TODO: How does this work? Is there a better way to get the hemisphere reference?
        Quaternion refRot = Quaternion.identity;
        bool first = true;
        foreach (Grb grab in grabs) {
            Transform physHandFollowTgt = grab.physHand.followTgtTrf;
            Quaternion tgtWorldRot =
                physHandFollowTgt.rotation *
                Quaternion.Inverse(grab.initRotFromGrblToPhysHand);
            Vector3 targetWorldPos =
                physHandFollowTgt.position -
                tgtWorldRot * grab.initPhysHandPosInGrblLocalSpace;
            avgTgtWorldPos += targetWorldPos;
            if (first) {
                refRot = tgtWorldRot;
                first = false;
            }
            if (Quaternion.Dot(tgtWorldRot, refRot) < 0f) {
                tgtWorldRot = new Quaternion(
                    -tgtWorldRot.x,
                    -tgtWorldRot.y,
                    -tgtWorldRot.z,
                    -tgtWorldRot.w
                );
            }
            cumulative.x += tgtWorldRot.x;
            cumulative.y += tgtWorldRot.y;
            cumulative.z += tgtWorldRot.z;
            cumulative.w += tgtWorldRot.w;
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
