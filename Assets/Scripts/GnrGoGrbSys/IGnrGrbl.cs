using UnityEngine;

/// <summary>
/// Grabbable with generic grabbable data, i.e. any grabbable in game object based grab systems.
/// </summary>
public interface IGnrGrbl<TPhysHand, TGrbl> : IGnrGrbl
    where TPhysHand : IGnrPhysHand<TGrbl, TPhysHand>
    where TGrbl : IGnrGrbl<TPhysHand, TGrbl> 
{
    bool CanBeGrabbed(TPhysHand physHand);

    bool CanBeReleased(TPhysHand physHand);

    /// <summary>
    /// Forces a grab between phys hand and this grabbable.<br/>
    /// NOTE: Phys hand should check if the grabbable <see cref="CanBeGrabbed"/> first!
    /// </summary>
    void OnInitGrb(TPhysHand physHand);

    /// <summary>
    /// Forces the <see cref="IGnrPhysHand"/> to release the grabbable.<br/>
    /// NOTE: <see cref="IGnrPhysHand"/> should check if the grabbable <see cref="CanBeReleased"/> first!
    /// </summary>
    void ReleaseGrb(TPhysHand physHand);
}

public interface IGnrGrbl{
    Rigidbody Rb { get; }
    IGnrGrbsCtrl GnrGrbs { get; }

    float GetDistToGrbPt(Vector3 physHandWldGrbPt);
}