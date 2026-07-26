using UnityEngine;

/// <summary>
/// Represents grabbable than can be grabbed by a physics hand.
/// </summary>
public interface IGrabbable {
    bool CanBeGrabbed(PhysHand physHand);
    float GetDistanceToGrabPoint(Vector3 physHandWorldGrabPoint);
    /// <summary>
    /// Should be called by a <see cref="PhysHand"/>.<br/>
    /// NOTE: <see cref="PhysHand"/> should check if the grabbable CanBeGrabbed first!
    /// </summary>
    void InitiateGrab(PhysHand physHand);
    void ReleaseGrab(PhysHand physHand);
}
