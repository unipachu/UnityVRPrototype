using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility methods using PhysX's features and the physics grab system.
/// </summary>
public static class PhysUtils {
    public static void SetJntDrivesToDflt(ConfigurableJoint jnt, PhysHandConfigurableJntData data) {
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

    public static void SetJntDrivesToAvgPhysHandsDflt(ConfigurableJoint jnt, List<GrblJntDriven_Grb> grabs) {
        // Calculate avg drives.
        float avgDfltLinDrivePosSpring = 0;
        float avgDfltLinDrivePosDamper = 0;
        float avgDfltLinDriveMaxForce = 0;
        float avgDfltSlerpDrivePosSpring = 0;
        float avgDfltSlerpDriveDamper = 0;
        float avgDefaultSlerpDriveMaxForce = 0;
        foreach (GrblJntDriven_Grb grab in grabs) {
            PhysHandConfigurableJntData data = grab.physHand.jntData;

            avgDfltLinDrivePosSpring += data.dfltLinDrivePosSpring;
            avgDfltLinDrivePosDamper += data.dfltLinDrivePosDamper;
            avgDfltLinDriveMaxForce += data.dfltLinDriveMaxForce;

            avgDfltSlerpDrivePosSpring += data.dfltSlerpDrivePosSpring;
            avgDfltSlerpDriveDamper += data.dfltSlerpDriveDamper;
            avgDefaultSlerpDriveMaxForce += data.defaultSlerpDriveMaxForce;
        }
        float invGrabCount = 1f / grabs.Count;
        avgDfltLinDrivePosSpring *= invGrabCount;
        avgDfltLinDrivePosDamper *= invGrabCount;
        avgDfltLinDriveMaxForce *= invGrabCount;
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
}
