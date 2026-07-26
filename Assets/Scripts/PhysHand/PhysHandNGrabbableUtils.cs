using System.Collections.Generic;
using UnityEngine;

public static class PhysHandNGrabbableUtils {
    public static void SetWorldJntDrivesToDflt(ConfigurableJoint jnt, PhysHandConfigurableJntData data) {
        JointDrive jntDrive = new JointDrive();
        // Linear drives:
        jntDrive.positionSpring = data.dfltLinDrivePosSpring;
        jntDrive.positionDamper = data.dfltLinDrivePosDamper;
        jntDrive.maximumForce = data.dfltLinDriveMaxForce;
        jnt.xDrive = jntDrive;
        jnt.yDrive = jntDrive;
        jnt.zDrive = jntDrive;
        // Angular drive:
        jntDrive.positionSpring = data.dfltSlerpDrivePosSpring;
        jntDrive.positionDamper = data.dfltSlerpDriveDamper;
        jntDrive.maximumForce = data.defaultSlerpDriveMaxForce;
        jnt.slerpDrive = jntDrive;
    }

    /// <summary>
    /// Simple single grab grabbable grab joint update.<br/>
    /// NOTE: Joint anchor is always Vector3.zero (pivot of the grabbable).
    /// </summary>
    public static void GrabJntUpdate_SimpleSingleGrabWithAnchorAtPivot(Grab grab, ConfigurableJoint grabJnt) {
        // NOTE: We update anchor every frame. For this grab type we could update anchor only when
        // NOTE C: grab configuration changes. // TODO: Change this.
        grabJnt.anchor = Vector3.zero;
        Transform physHandFollowTgt = grab.physHand.followTgtTrf;
        Quaternion targetWorldRot =
            physHandFollowTgt.rotation * Quaternion.Inverse(grab.initRotFromGrabbableToPhysHand);
        Vector3 targetWorldPos =
            physHandFollowTgt.position - targetWorldRot * grab.initPhysHandPosInGrabbableLocalSpace;
        grabJnt.targetPosition = targetWorldPos;
        grabJnt.targetRotation = targetWorldRot;
        // NOTE: We update joint drives every frame. For this grab type we could update drives only
        // NOTE C: when grab configuration changes. // TODO: Change this.
        SetWorldJntDrivesToDflt(grabJnt, grab.physHand.jntData);
    }

    /// <summary>
    /// Simple single grab grabbable grab joint update.<br/>
    /// NOTE: Joint anchor is the local position of the grabbing <see cref="PhysHand"/>.
    /// </summary>
    public static void GrabJntUpdate_SimpleSingleGrabWithAnchorAtPhysHandPos(Grab grab, ConfigurableJoint grabJnt, Vector3 grabbableLossyScale) {
        // NOTE: We update anchor every frame. For this grab type we could update anchor only when
        // NOTE C: grab configuration changes. // TODO: Change this.
        grabJnt.anchor = new Vector3(
            grab.initPhysHandPosInGrabbableLocalSpace.x / grabbableLossyScale.x,
            grab.initPhysHandPosInGrabbableLocalSpace.y / grabbableLossyScale.y,
            grab.initPhysHandPosInGrabbableLocalSpace.z / grabbableLossyScale.z
        );
        Transform physHandFollowTgt = grab.physHand.followTgtTrf;
        Quaternion targetWorldRot =
            physHandFollowTgt.rotation * Quaternion.Inverse(grab.initRotFromGrabbableToPhysHand);
        //Vector3 targetWorldPos =
        //    physHandFollowTgt.position - targetWorldRot * grab.initPhysHandPosInGrabbableLocalSpace;
        Vector3 targetWorldPos = physHandFollowTgt.position;
        grabJnt.targetPosition = targetWorldPos;
        grabJnt.targetRotation = targetWorldRot;
        // NOTE: We update joint drives every frame. For this grab type we could update drives only
        // NOTE C: when grab configuration changes. // TODO: Change this.
        SetWorldJntDrivesToDflt(grabJnt, grab.physHand.jntData);
    }

    public static void GrabJntUpdate_UpdateMultiGrabJnt(List<Grab> grabs, ConfigurableJoint grabJnt) {
        // TODO
    }
}
