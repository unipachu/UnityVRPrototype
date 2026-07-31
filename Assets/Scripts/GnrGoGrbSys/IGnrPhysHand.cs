using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Represents a generic physics hand.
/// </summary>
public interface IGnrPhysHand{
    public HapticImpulsePlayer CtrlHapticImpPlr { get; }
    public Transform FollowTgtTrf { get; }
    public Side HandSide { get; }
    public Transform Trf { get; }

    /// <summary>
    /// Should be called by grabbable on release. Parameters represent the proxy hand wld pose
    /// at the moment of grab release.
    /// </summary>
    void OnGrabReleased(Vector3 grabReleaseWorldPos, Quaternion grabReleaseWorldRot);
}
