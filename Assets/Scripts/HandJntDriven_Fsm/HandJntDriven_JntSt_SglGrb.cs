using UnityEngine;

public class HandJntDriven_JntSt_SglGrb : IFsmSt {
    IHandJntDriven_Grbl grbl;

    public HandJntDriven_JntSt_SglGrb(IHandJntDriven_Grbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt previousState) {
        ConfigurableJoint handWldJnt = grbl.Grbs[0].physHand.wldJnt;
        //grbl.GrbJnt.anchor = new Vector3(
        //    grbl.Grbs[0].gnrGrb.initPhysHandPosInGrblSpc.x / grbl.Rb.transform.lossyScale.x,
        //    grbl.Grbs[0].gnrGrb.initPhysHandPosInGrblSpc.y / grbl.Rb.transform.transform.lossyScale.y,
        //    grbl.Grbs[0].gnrGrb.initPhysHandPosInGrblSpc.z / grbl.Rb.transform.transform.lossyScale.z
        //);
        PhysUtils.SetJntDrivesToDflt(
            handWldJnt,
            grbl.Grbs[0].physHand.wldJntData
        );
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grbl.Grbs[0], grbl.Grbs[0].physHand.wldJnt);
    }

    public void Tick() {
    }

    // -----------------------------------------
    // Private Methods
    // -----------------------------------------

    void UpdateJnt(HandJntDriven_Grb grb, ConfigurableJoint wldJnt) {
        Transform physHandFollowTgt = grb.physHand.followTgtTrf;
        wldJnt.targetPosition = physHandFollowTgt.position;
        wldJnt.targetRotation = physHandFollowTgt.rotation;
    }
}
