using UnityEngine;

/// <summary>
/// Represents a generic grabbable - any grabbable in game object based grab systems.
/// </summary>
public interface IGnrGrbl {
    Rigidbody Rb { get; }
    IGnrGrbs GnrGrbs { get; }
}
