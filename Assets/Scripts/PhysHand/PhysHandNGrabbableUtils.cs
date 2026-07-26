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
}
