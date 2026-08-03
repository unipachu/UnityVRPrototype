using UnityEngine;

/// <summary>
/// Motion state - linear and angular velocities.
/// </summary>
public struct MotSt {
    public Vector3 linVel;
    public Vector3 angVel;
    public MotSt(Vector3 linVel, Vector3 angVel) {
        this.linVel = linVel;
        this.angVel = angVel;
    }
}