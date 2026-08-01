using UnityEngine;

/// <summary>
/// A keyhole-snappable grabbable, i.e. a key that can snap to keyholes.
/// </summary>
public interface IKeyholeSnpl {
    /// <summary>
    /// NOTE: Grbl rb's orientation should match with the keyhole
    /// (rb forward towards the keyhole insertion direction).
    /// </summary>
    IGnrGrblData GnrGrblData { get; }
    /// <summary>
    /// Position representing the center of the tip of the key in grbl rb space.
    /// </summary>
    Vector3 KeyTipLclPos { get; }
    /// <summary>
    /// Is the grabbable available for snapping to keyhole? 
    /// </summary>
    bool CanSnp();
    /// <summary>
    /// After the snappable has been initialized for snap, it should have a kinematic rigidbody.
    /// </summary>
    void InitSnp(ISnapTgt snpTgt);
    /// <summary>
    /// Should be called by the keyhole when it decides to end the snap.
    /// </summary>
    void OnEndSnp();
}
