using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Represents a generic physics hand, i.e. any game object-based physics hand.
/// </summary>
public interface IGnrPhysHand<TGrbl, TPhysHand> : IGnrPhysHand
    where TGrbl : IGnrGrbl<TPhysHand, TGrbl>
    where TPhysHand : IGnrPhysHand<TGrbl, TPhysHand> 
{
    void InitGrab(TGrbl grbl);
}

public interface IGnrPhysHand{
    public HapticImpulsePlayer CtrlHapticImpPlr { get; }
    public Transform FollowTgtTrf { get; }
    public Side HandSide { get; }
    public Transform Trf { get; }
    public ConfigurableJoint WldJnt { get; }

    /// <summary>
    /// Should be called by grabbable when it releases the grab (since grabbables are responsible for grab
    /// release). Parameters represent the proxy hand world pose at the moment of grab release.
    /// </summary>
    void OnReleaseGrb(Vector3 grbReleaseWldPos, Quaternion grbReleaseWldRot);
}