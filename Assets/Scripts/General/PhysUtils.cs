using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility methods using PhysX's features and the physics grab system.
/// </summary>
public static class PhysUtils {
    public static void SetJntDrivesToDflt(ConfigurableJoint jnt, DfltConfigJntData data) {
        JointDrive jntDrive = new JointDrive();
        // Linear drives:
        jntDrive.positionSpring = data.dfltLinDrvPosSpring;
        jntDrive.positionDamper = data.dfltLinDrvPosDamper;
        jntDrive.maximumForce = data.dfltLinDrvMaxForce;
        jnt.xDrive = jntDrive;
        jnt.yDrive = jntDrive;
        jnt.zDrive = jntDrive;
        // Angular drive:
        jntDrive.positionSpring = data.dfltSlerpDrvPosSpring;
        jntDrive.positionDamper = data.dfltSlerpDrvDamper;
        jntDrive.maximumForce = data.dfltSlerpDrvMaxForce;
        jnt.slerpDrive = jntDrive;
    }

    // TODO: Rename to generic. Write summary.
    public static void SetJntDrivesToAvgPhysHandsDflt(ConfigurableJoint jnt, List<GrblJntDriven_Grb> grabs) {
        // Calculate avg drives.
        float avgDfltLinDrivePosSpring = 0;
        float avgDfltLinDrivePosDamper = 0;
        float avgDfltLinDriveMaxForce = 0;
        float avgDfltSlerpDrivePosSpring = 0;
        float avgDfltSlerpDriveDamper = 0;
        float avgDefaultSlerpDriveMaxForce = 0;
        foreach (GrblJntDriven_Grb grab in grabs) {
            DfltConfigJntData data = grab.physHand.wldJntData;
            // Linear drive
            avgDfltLinDrivePosSpring += data.dfltLinDrvPosSpring;
            avgDfltLinDrivePosDamper += data.dfltLinDrvPosDamper;
            avgDfltLinDriveMaxForce += data.dfltLinDrvMaxForce;
            // Slerp drive
            avgDfltSlerpDrivePosSpring += data.dfltSlerpDrvPosSpring;
            avgDfltSlerpDriveDamper += data.dfltSlerpDrvDamper;
            avgDefaultSlerpDriveMaxForce += data.dfltSlerpDrvMaxForce;
        }
        float invGrabCount = 1f / grabs.Count;
        // Linear drive
        avgDfltLinDrivePosSpring *= invGrabCount;
        avgDfltLinDrivePosDamper *= invGrabCount;
        avgDfltLinDriveMaxForce *= invGrabCount;
        // Slerp drive
        avgDfltSlerpDrivePosSpring *= invGrabCount;
        avgDfltSlerpDriveDamper *= invGrabCount;
        avgDefaultSlerpDriveMaxForce *= invGrabCount;
        // Set drives.
        JointDrive jntDrive = new JointDrive();
        // Linear drives:
        jntDrive.positionSpring = avgDfltLinDrivePosSpring;
        jntDrive.positionDamper = avgDfltLinDrivePosDamper;
        jntDrive.maximumForce = avgDfltLinDriveMaxForce;
        jnt.xDrive = jntDrive;
        jnt.yDrive = jntDrive;
        jnt.zDrive = jntDrive;
        // Angular drive:
        jntDrive.positionSpring = avgDfltSlerpDrivePosSpring;
        jntDrive.positionDamper = avgDfltSlerpDriveDamper;
        jntDrive.maximumForce = avgDefaultSlerpDriveMaxForce;
        jnt.slerpDrive = jntDrive;
    }

    public static void SetJntDrivesToZero(ConfigurableJoint jnt) {
        JointDrive jntDrive = new JointDrive();
        // Linear drives:
        jntDrive.positionSpring = 0;
        jntDrive.positionDamper = 0;
        jntDrive.maximumForce = 0;
        jnt.xDrive = jntDrive;
        jnt.yDrive = jntDrive;
        jnt.zDrive = jntDrive;
        // Angular drive:
        jntDrive.positionSpring = 0;
        jntDrive.positionDamper = 0;
        jntDrive.maximumForce = 0;
        jnt.slerpDrive = jntDrive;
    }

    /// <summary>
    /// Set configurable joint motion constraints to free.
    /// </summary>
    public static void SetJntMotCstrsToFree(ConfigurableJoint jnt) {
        jnt.xMotion = ConfigurableJointMotion.Free;
        jnt.yMotion = ConfigurableJointMotion.Free;
        jnt.zMotion = ConfigurableJointMotion.Free;
        jnt.angularXMotion = ConfigurableJointMotion.Free;
        jnt.angularYMotion = ConfigurableJointMotion.Free;
        jnt.angularZMotion = ConfigurableJointMotion.Free;
    }

    /// <summary>
    /// Set configurable joint motion constraints to locked.
    /// </summary>
    public static void SetJntMotCstrsToLocked(ConfigurableJoint jnt) {
        jnt.xMotion = ConfigurableJointMotion.Locked;
        jnt.yMotion = ConfigurableJointMotion.Locked;
        jnt.zMotion = ConfigurableJointMotion.Locked;
        jnt.angularXMotion = ConfigurableJointMotion.Locked;
        jnt.angularYMotion = ConfigurableJointMotion.Locked;
        jnt.angularZMotion = ConfigurableJointMotion.Locked;
    }

    /// <summary>
    /// Teleports a world-joint-controlled rigidbody to target pose defined by <paramref name="tgtTrf"/>.
    /// </summary>
    public static void TeleportWldJntCtrldRb(Transform trf, Rigidbody rb, Transform tgtTrf, ConfigurableJoint wldJnt, DfltConfigJntData configJntData) {
        // We move the hand to the pose of the controller.
        trf.position = tgtTrf.position;
        trf.rotation = tgtTrf.rotation;
        rb.position = tgtTrf.position;
        rb.rotation = tgtTrf.rotation;
        // Set world joint targets.
        wldJnt.targetPosition = tgtTrf.position;
        wldJnt.targetRotation = tgtTrf.rotation;
        SetJntDrivesToDflt(wldJnt, configJntData);
    }

    /// <summary>
    /// Teleports a world-joint-controlled rigidbody to target pose defined by <paramref name="tgtTrf"/>.
    /// </summary>
    public static void TeleportWldJntCtrldRb(Transform trf, Rigidbody rb, Vector3 tgtWldPos, Quaternion tgtWldRot, ConfigurableJoint wldJnt, DfltConfigJntData configJntData) {
        // We move the hand to the pose of the controller.
        trf.position = tgtWldPos;
        trf.rotation = tgtWldRot;
        rb.position = tgtWldPos;
        rb.rotation = tgtWldRot;
        // Set world joint targets.
        wldJnt.targetPosition = tgtWldPos;
        wldJnt.targetRotation = tgtWldRot;
        SetJntDrivesToDflt(wldJnt, configJntData);
    }
}
