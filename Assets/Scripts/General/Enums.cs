// NOTE: All enums are public so they can be used by types and members of any accessibility.
// NOTE #2: All enums use "byte" storage type which is 1 byte and smaller than the default 4 bytes.

/// <summary>
/// Represents direction on a vertical plane.
/// </summary>
public enum Dir2D : byte {
    Left,
    Right,
    Down,
    Up,
}

public enum Dir3D : byte {
    Left,
    Right,
    Down,
    Up,
    Back,
    Forward,
}

public enum EcsGrblCanBeGrabbedMode : byte {
    AlwaysAllow,
    AllowMax1Grab,
    AllowMax2GrabbingHands,
}

public enum EcsGrblDistMode : byte {
    /// <summary>
    /// Use physics query's distance.
    /// </summary>
    ClosestOnCollider,
    /// <summary>
    /// Use distance from grab search sphere pos to grabbable pivot.
    /// </summary>
    ToPivot,
    /// <summary>
    /// Use distance to a grabbable's local point definied by grabbable component.
    /// </summary>
    ToLocalPoint
}

public enum GrblJntDriven_BasicGrblSglGrbJntT : byte {
    AnchAtGrblPiv,
    AnchAtPhysHandPos,
}

public enum GrblJntDriven_BasicGrblDblGrbJntT : byte{
    GrbLineAligned,
    SimpleAnchAtPiv,
}

public enum Keyhole_SnappedSnplSt : byte {
    Outside,
    InsideMiddle,
    InsideEnd,
}

public enum PhysHandState : byte{
    NotGrabbing,
    Grabbing,
    Resetting
}

public enum Side : byte {
    Left,
    Right,
}

public enum SnpTgtSt : byte{
    LookForSnpls,
    SnpInInterp,
    Snapped,
    SnpOutInterp,
}
