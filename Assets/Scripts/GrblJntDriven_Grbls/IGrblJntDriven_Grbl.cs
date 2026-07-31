using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents grabbable than can be grabbed by a physics hand.
/// </summary>
public interface IGrblJntDriven_Grbl {
    Transform Trf { get; }
    ConfigurableJoint GrbJnt { get; }
    List<GrblJntDriven_Grb> Grbs { get; }

    // TODO: These can maybe be part of the generic IGnrGrbl interface.
    bool CanBeGrabbed(GrblJntDriven_PhysHand physHand);
    
    bool CanBeReleased(GrblJntDriven_PhysHand physHand);
    
    float GetDistToGrbPt(Vector3 physHandWldGrbPt);
    
    /// <summary>
    /// Forces a grab between <see cref="GrblJntDriven_PhysHand"/> and this <see cref="IGrblJntDriven_Grbl"/>.<br/>
    /// NOTE: <see cref="GrblJntDriven_PhysHand"/> should check if the <see cref="IGrblJntDriven_Grbl"/> CanBeGrabbed first!
    /// </summary>
    void InitiateGrb(GrblJntDriven_PhysHand physHand);
    
    /// <summary>
    /// Forces the <see cref="GrblJntDriven_PhysHand"/> to release the <see cref="IGrblJntDriven_Grbl"/>.<br/>
    /// NOTE: <see cref="GrblJntDriven_PhysHand"/> should check if the <see cref="IGrblJntDriven_Grbl"/> CanBeReleased first!
    /// </summary>
    void ReleaseGrb(GrblJntDriven_PhysHand physHand);
}
