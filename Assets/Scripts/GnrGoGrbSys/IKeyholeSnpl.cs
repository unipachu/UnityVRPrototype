using UnityEngine;

/// <summary>
/// An object that can snap to keyholes, i.e. a key.
/// </summary>
public interface IKeyholeSnpl {
    /// <summary>
    /// NOTE: Rb's orientation should match with the keyhole
    /// (rb forward towards the keyhole insertion direction).
    /// </summary>
    IGnrGrbl Grbl { get; }
    Rigidbody Rb { get; }
    /// <summary>
    /// Position representing the center of the tip of the key in grbl rb space.
    /// </summary>
    Vector3 KeyTipLclPos { get; }
    bool CanSnp();
    /// <summary>
    /// After the snappable has been initialized for snap, it should have a kinematic rigidbody.
    /// </summary>
    void InitSnp(ISnapTgt snpTgt);
    void OnEndSnp();
}
