using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Uses avg target pose based on all grabbing hands.<br/> 
/// NOTE: If you want highly stable movement, the grabbable pivot should equal to the center of
/// mass of the grabbable.
/// </summary>
public class GrblJntDriven_GrblJntSt_MultiGrb_SimpleAnchAtPiv : IFsmSt{
    IGrblJntDriven_Grbl grbl;

    public GrblJntDriven_GrblJntSt_MultiGrb_SimpleAnchAtPiv(IGrblJntDriven_Grbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt prevSt) {
        grbl.GrblCore.grbJnt.anchor = Vector3.zero;
        // NOTE: A better system would be to use different hand drives to every frame calculate
        // NOTE C: weights for how much each hand affects the grab joint target pose, but this
        // NOTE C: is just a "simple" multi grab system.
        PhysUtils.SetJntDrivesToAvgPhysHandsDflt(
            grbl.GrblCore.grbJnt,
            grbl.GrblCore.grbs
        );
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grbl.GrblCore.grbs, grbl.GrblCore.grbJnt);
    }

    public void Tick() {
    }

    // -----------------------------------------
    // Private Methods
    // -----------------------------------------

    public void UpdateJnt(List<Grb> grbs, ConfigurableJoint grbJnt) {
        Vector3 avgTgtWorldPos = Vector3.zero;
        Vector4 cumulative = Vector4.zero;
        // Use the first target rotation as the hemisphere reference.
        // TODO: How does this work? Is there a better way to get the hemisphere reference?
        Quaternion refRot = Quaternion.identity;
        bool first = true;
        foreach (Grb grb in grbs) {
            //Transform physHandFollowTgt = grb.physHand.followTgtTrf;
            //Quaternion tgtWldRot =
            //    physHandFollowTgt.rotation * Quaternion.Inverse(grb.initRotFromGrblToPhysHand);
            //Vector3 tgtWldPos =
            //    physHandFollowTgt.position - tgtWldRot * grb.initPhysHandPosInGrblSpc;
            Quaternion tgtWldRot = GrblUtils.TheoFolTgtGrblRot(grb);
            Vector3 tgtWldPos = GrblUtils.TheoFolTgtGrblPos(grb, tgtWldRot);
            avgTgtWorldPos += tgtWldPos;
            if (first) {
                refRot = tgtWldRot;
                first = false;
            }
            if (Quaternion.Dot(tgtWldRot, refRot) < 0f) {
                tgtWldRot = new Quaternion(
                    -tgtWldRot.x,
                    -tgtWldRot.y,
                    -tgtWldRot.z,
                    -tgtWldRot.w
                );
            }
            cumulative.x += tgtWldRot.x;
            cumulative.y += tgtWldRot.y;
            cumulative.z += tgtWldRot.z;
            cumulative.w += tgtWldRot.w;
        }
        avgTgtWorldPos /= grbs.Count;
        Quaternion avgTgtWorldRot = new Quaternion(
            cumulative.x,
            cumulative.y,
            cumulative.z,
            cumulative.w
        ).normalized;
        grbJnt.targetPosition = avgTgtWorldPos;
        grbJnt.targetRotation = avgTgtWorldRot;
    }
}
