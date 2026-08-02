using UnityEngine;

// TODO: Could you combine this state to the hand joint driven no grab?
public class GrblJntDriven_GrblJntSt_NoGrb : IFsmSt {
    IGrblJntDriven_Grbl grbl;

    public GrblJntDriven_GrblJntSt_NoGrb(IGrblJntDriven_Grbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt prevSt) {
        grbl.GrbJnt.anchor = Vector3.zero;
        PhysUtils.SetJntDrivesToZero(grbl.GrbJnt);
    }

    public void Exit() {
    }

    public void PhysicsTick() {
    }

    public void Tick() {
    }
}
