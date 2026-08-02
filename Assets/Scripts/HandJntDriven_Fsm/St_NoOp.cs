/// <summary>
/// No joints are connected to this grabbable, so this does nothing.
/// </summary>
public class St_NoOp : IFsmSt {

    public St_NoOp() {
    }

    // -----------------------------------------
    // IFsmSt Methods
    // -----------------------------------------

    public void Enter(IFsmSt prevSt) {
    }

    public void Exit() {
    }

    public void PhysicsTick() {
    }

    public void Tick() {
    }
}
