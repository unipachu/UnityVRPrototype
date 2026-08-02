using System.Collections.Generic;

/// <summary>
/// In the hand-joint-driven grab system, represents grabbable than can be grabbed by a physics hand.
/// </summary>
public interface IHandJntDriven_Grbl {
    List<HandJntDriven_Grb> Grbs { get; }

    IGnrGrbl GnrGrbl { get; }

    // TODO: These can maybe be part of the generic IGnrGrbl interface.
    bool CanBeGrabbed(HandJntDriven_PhysHand physHand);

    bool CanBeReleased(HandJntDriven_PhysHand physHand);

    /// <summary>
    /// Forces a grab between <see cref="HandJntDriven_PhysHand"/> and this <see cref="IHandJntDriven_Grbl"/>.<br/>
    /// NOTE: <see cref="HandJntDriven_PhysHand"/> should check if the <see cref="IHandJntDriven_Grbl"/> CanBeGrabbed first!
    /// </summary>
    void OnInitGrb(HandJntDriven_PhysHand physHand);

    /// <summary>
    /// Forces the <see cref="HandJntDriven_PhysHand"/> to release the <see cref="IHandJntDriven_Grbl"/>.<br/>
    /// NOTE: <see cref="HandJntDriven_PhysHand"/> should check if the <see cref="IHandJntDriven_Grbl"/> CanBeReleased first!
    /// </summary>
    void ReleaseGrb(HandJntDriven_PhysHand physHand);
}
