using UnityEngine;

/// <summary>
/// An object that can snap to keyholes, i.e. a key.
/// </summary>
public interface IKeyholeSnpl {
    GrblJntDriven_GrblCore GrblCore { get; }
    /// <summary>
    /// Transform representing the center of the tip of the key.
    /// Forward should be the keyhole insertion direction.
    /// </summary>
    Transform KeyTipTrf { get; }
    bool CanSnp();
    /// <summary>
    /// After the snappable has been initialized for snap, it should have a kinematic rigidbody.
    /// </summary>
    void InitSnp(ISnapTgt snpTgt);
    void OnEndSnp();
}
