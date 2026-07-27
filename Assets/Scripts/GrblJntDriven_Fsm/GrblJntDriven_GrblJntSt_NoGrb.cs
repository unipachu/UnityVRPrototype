using UnityEngine;

public class GrblJntDriven_GrblJntSt_NoGrb : IFsmSt {
    IGrbl grbl;

    public GrblJntDriven_GrblJntSt_NoGrb(IGrbl grbl) {
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
