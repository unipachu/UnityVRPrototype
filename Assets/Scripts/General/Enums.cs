// NOTE: All enums are public so they can be used by types and members of any accessibility.

/// <summary>
/// Represents direction on a vertical plane.
/// </summary>
public enum Dir2D {
    Left,
    Right,
    Down,
    Up,
}

public enum Dir3D {
    Left,
    Right,
    Down,
    Up,
    Back,
    Forward,
}

public enum GrblJntDriven_BasicGrblSglGrbJntT {
    AnchAtGrblPiv,
    AnchAtPhysHandPos,
}

public enum GrblJntDriven_BasicGrblDblGrbJntT {
    GrbLineAligned,
    SimpleAnchAtPiv,
}

public enum Keyhole_SnappedSnplSt {
    Outside,
    InsideMiddle,
    InsideEnd,
}

public enum PhysHandState {
    NotGrabbing,
    Grabbing,
    Resetting
}

public enum Side {
    Left,
    Right,
}

public enum SnpTgtSt {
    LookForSnpls,
    SnpInInterp,
    Snapped,
    SnpOutInterp,
}
