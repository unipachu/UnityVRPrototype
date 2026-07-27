using UnityEngine;

/// <summary>
/// Reuseable FSM.
/// </summary>
public class Fsm {
    public bool IsSwitchingState { get; private set; }
    public IFsmSt CurrentState { get; private set; }

    /// <summary>
    /// Switches to a new <see cref="IFsmSt"/> by calling <see cref="IFsmSt.Exit"/> on the
    /// current state, then <see cref="IFsmSt.Enter"/> on the new state, passing the previous
    /// state as an argument. Finally sets the current state to the new state.
    /// </summary>
    /// <param name="ctx">Context used for Debug messages.</param>
    public void SwitchState(IFsmSt newSt, Object ctx = null) {
#if UNITY_EDITOR
        if(ctx != null) {
            Debug.Assert(!IsSwitchingState, "Already switching state!", ctx);
            Debug.Assert(CurrentState != newSt, "Tried to change to same state we are already in. " +
                "This can cause errors related to animation events overlapping during animation transition.", ctx);
        }
        else {
            Debug.Assert(!IsSwitchingState, "Already switching state!");
            Debug.Assert(CurrentState != newSt, "Tried to change to same state we are already in. " +
                "This can cause errors related to animation events overlapping during animation transition.");
        }
#endif
        IsSwitchingState = true;
        if (CurrentState != null)
            CurrentState.Exit();
        newSt.Enter(CurrentState);
        CurrentState = newSt;
        //Debug.Log("Switched to state: " + newSt.GetType().Name);
        IsSwitchingState = false;
    }
}
