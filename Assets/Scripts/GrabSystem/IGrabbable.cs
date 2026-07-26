using UnityEngine;

/// <summary>
/// Represents grabbable than can be grabbed by a physics hand.
/// </summary>
public interface IGrabbable {
    
    bool CanBeGrabbed(PhysHand physHand);
    
    bool CanBeReleased(PhysHand physHand);
    
    public Grab FindGrab(PhysHand physHand);

    float GetDistanceToGrabPoint(Vector3 physHandWorldGrabPoint);
    
    /// <summary>
    /// Forces a grab between <see cref="PhysHand"/> and this <see cref="IGrabbable"/>.<br/>
    /// NOTE: <see cref="PhysHand"/> should check if the <see cref="IGrabbable"/> CanBeGrabbed first!
    /// </summary>
    void InitiateGrab(PhysHand physHand);
    
    /// <summary>
    /// Forces the <see cref="PhysHand"/> to release the <see cref="IGrabbable"/>.<br/>
    /// NOTE: <see cref="PhysHand"/> should check if the <see cref="IGrabbable"/> CanBeReleased first!
    /// </summary>
    void ReleaseGrab(PhysHand physHand);
}
