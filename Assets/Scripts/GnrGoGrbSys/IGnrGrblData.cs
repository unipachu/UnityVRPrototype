using UnityEngine;

/// <summary>
/// Grabbable with generic grabbable data, i.e. any grabbable in game object based grab systems.
/// </summary>
public interface IGnrGrblData {
    Rigidbody Rb { get; }
    IGnrGrbsCtrl GnrGrbs { get; }
}
