using UnityEngine;

public class HandJntDriven_JntSt_DblGrb : IFsmSt {
    IHandJntDriven_Grbl grbl;

    public HandJntDriven_JntSt_DblGrb(IHandJntDriven_Grbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt previousState) {
        ConfigurableJoint handWldJnt = grbl.Grbs[0].physHand.wldJnt;
        PhysUtils.SetJntDrivesToDflt(handWldJnt, grbl.Grbs[0].physHand.wldJntData);
        handWldJnt = grbl.Grbs[1].physHand.wldJnt;
        PhysUtils.SetJntDrivesToDflt(handWldJnt, grbl.Grbs[1].physHand.wldJntData);
    }

    public void Exit() {
    }

    public void PhysicsTick() {
        UpdateJnt(grbl.Grbs[0], grbl.Grbs[0].physHand.wldJnt);
        UpdateJnt(grbl.Grbs[1], grbl.Grbs[1].physHand.wldJnt);
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
