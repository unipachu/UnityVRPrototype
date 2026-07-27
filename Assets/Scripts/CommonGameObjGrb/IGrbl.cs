using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents grabbable than can be grabbed by a physics hand.
/// </summary>
public interface IGrbl {
    GameObject GrblGameObj { get; }
    ConfigurableJoint GrbJnt { get; }
    List<Grb> Grbs { get; }
    Rigidbody Rb { get; }

    bool CanBeGrabbed(GrblJntDriven_PhysHand physHand);
    
    bool CanBeReleased(GrblJntDriven_PhysHand physHand);
    
    public Grb FindGrb(GrblJntDriven_PhysHand physHand);

    float GetDistToGrbPt(Vector3 physHandWorldGrbPt);
    
    /// <summary>
    /// Forces a grab between <see cref="GrblJntDriven_PhysHand"/> and this <see cref="IGrbl"/>.<br/>
    /// NOTE: <see cref="GrblJntDriven_PhysHand"/> should check if the <see cref="IGrbl"/> CanBeGrabbed first!
    /// </summary>
    void InitiateGrb(GrblJntDriven_PhysHand physHand);
    
    /// <summary>
    /// Forces the <see cref="GrblJntDriven_PhysHand"/> to release the <see cref="IGrbl"/>.<br/>
    /// NOTE: <see cref="GrblJntDriven_PhysHand"/> should check if the <see cref="IGrbl"/> CanBeReleased first!
    /// </summary>
    void ReleaseGrb(GrblJntDriven_PhysHand physHand);
}
