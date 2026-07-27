/// <summary>
/// Finite state machine state.
/// </summary>
public interface IFsmSt {
    void Enter(IFsmSt previousState);
    void Exit();
    void PhysicsTick();
    void Tick();
}
