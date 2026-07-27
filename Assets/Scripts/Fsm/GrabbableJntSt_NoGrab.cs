using UnityEngine;

public class GrabbableJntSt_NoGrab : IFsmSt {
    IGrabbable grabbable;

    public GrabbableJntSt_NoGrab(IGrabbable grabbable) {
        this.grabbable = grabbable;
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt previousState) {
        grabbable.GrabJnt.anchor = Vector3.zero;
        PhysHandNGrabbableUtils.SetJntDrivesToZero(grabbable.GrabJnt);
    }

    public void Exit() {
    }

    public void PhysicsTick() {
    }

    public void Tick() {
    }
}
