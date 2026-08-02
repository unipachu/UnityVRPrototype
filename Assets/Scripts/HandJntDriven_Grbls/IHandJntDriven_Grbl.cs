using System.Collections.Generic;

/// <summary>
/// In the hand-joint-driven grab system, represents grabbable than can be grabbed by a physics hand.
/// </summary>
public interface IHandJntDriven_Grbl : IGnrGrbl<HandJntDriven_PhysHand, IHandJntDriven_Grbl> {
    List<HandJntDriven_Grb> Grbs { get; }
}
