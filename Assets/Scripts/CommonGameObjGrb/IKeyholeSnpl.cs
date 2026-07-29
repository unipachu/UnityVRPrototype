using UnityEngine;

/// <summary>
/// An object that can snap to keyholes.
/// </summary>
public interface IKeyholeSnpl {
    GrblJntDriven_GrblCore GrblCore { get; }
    Transform SnpTrf { get; }
    bool CanSnp();
    /// <summary>
    /// After the snappable has been initialized for snap, it should have a kinematic rigidbody.
    /// </summary>
    void InitSnp(ISnapTgt snpTgt);
    void OnEndSnp();
}
