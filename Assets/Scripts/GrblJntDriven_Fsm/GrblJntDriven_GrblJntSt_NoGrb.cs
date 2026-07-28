using UnityEngine;

public class GrblJntDriven_GrblJntSt_NoGrb : IFsmSt {
    IGrblJntDriven_Grbl grbl;

    public GrblJntDriven_GrblJntSt_NoGrb(IGrblJntDriven_Grbl grbl) {
        this.grbl = grbl;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt prevSt) {
        grbl.GrblCore.grbJnt.anchor = Vector3.zero;
        PhysUtils.SetJntDrivesToZero(grbl.GrblCore.grbJnt);
    }

    public void Exit() {
    }

    public void PhysicsTick() {
    }

    public void Tick() {
    }
}
