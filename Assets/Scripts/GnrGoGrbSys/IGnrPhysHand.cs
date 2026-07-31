using UnityEngine;

/// <summary>
/// Represents a generic physics hand.
/// </summary>
public interface IGnrPhysHand{
    public Transform FollowTgtTrf { get; }
    public Side HandSide { get; }
}
