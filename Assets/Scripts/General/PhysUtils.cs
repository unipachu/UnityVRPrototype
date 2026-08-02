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
}
