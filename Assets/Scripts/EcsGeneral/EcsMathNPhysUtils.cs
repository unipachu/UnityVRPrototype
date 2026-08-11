using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// DOTS NOTE: Burst-compatible helper methods for CustomPhysicsJoint.
/// DOTS NOTE: Uses Unity.Mathematics structures like Unity.Mathematics.float3 (instead of UnityEngine.Vector3).
/// </summary>
public static class EcsMathNPhysUtils {
    /// <summary>
    /// Calculates added world angular velocity caused by angular impulse to rigidbody.
    /// </summary>
    /// <param name="wldAngImp">Torque impulse in world space.</param>
    /// <param name="rot">Current rigidbody rotation.</param>
    /// <param name="invrsInertia">Local space inertia inversed.</param>
    public static float3 ApplyInverseInertia(
        float3 wldAngImp,
        quaternion rot,
        float3 invrsInertia
    ) {
        // Convert impulse into local space
        float3 localImpulse = math.rotate(math.inverse(rot), wldAngImp);
        // Apply inertia tensor
        float3 localAngVel = localImpulse * invrsInertia;
        // Convert back to world space
        return math.rotate(rot, localAngVel);
    }

    /// <summary>
    /// Computes Torque = springStiffness * rotErr - relativeAngVel * damper.
    /// </summary>
    public static float3 CalculateSpringAngTq(
        quaternion curRot,
        quaternion tgtRot,
        float3 relAngVel,
        float springStiffness,
        float damper,
        float maxTq,
        float rotDeadzone = 0.001f,
        float velDeadzone = 0.001f
    ) {
        float3 rotError = GetRotErr(curRot, tgtRot);
        if (
            math.lengthsq(rotError) < rotDeadzone * rotDeadzone &&
            math.lengthsq(relAngVel) < velDeadzone * velDeadzone
        )
            return float3.zero;
        float3 springTq = rotError * springStiffness;
        float3 dampingTq = -relAngVel * damper;
        float3 tq = springTq + dampingTq;
        float mag = math.length(tq);
        if (mag > maxTq)
            tq *= maxTq / mag;
        return tq;
    }

    /// <summary>
    /// Computes F = springStiffness * distToTgt - relativeVel * damper.<br/>
    /// </summary>
    /// <param name="relVel">This pos vel in tgt pos space.</param>
    public static float3 CalculateSpringLinForce(
        float3 tgtPos,
        float3 curPos,
        float3 relVel,
        float springStiffness,
        float damper,
        float maxForce,
        float posDeadzone = 0.001f,
        float velDeadzone = 0.001f
    ) {
        float3 displacement = tgtPos - curPos;
        // NOTE: lengthsq < deadzone * deadzone is more performant than
        // NOTE C: length < deadzone because it does not calculate squareroot.
        if (
            math.lengthsq(displacement) < posDeadzone * posDeadzone
            && math.lengthsq(relVel) < velDeadzone * velDeadzone
        )
            return float3.zero;
        float3 springForce = displacement * springStiffness;
        float3 dampingForce = -relVel * damper;
        float3 force = springForce + dampingForce;
        float mag = math.length(force);
        if (mag > maxForce)
            force *= maxForce / mag;
        return force;
    }

    /// <summary>
    /// Returns the world space velocity of a point on the rigidbody.
    /// </summary>
    public static float3 GetPointVelocity(
        float3 linVel,
        float3 angVel,
        float3 point,
        float3 wldCom
    ) {
        float3 r = point - wldCom;
        // NOTE: Standard point velocity equation: v = linearVel + cross(angVel, fromCenterOfMassToPoint)
        return linVel + math.cross(angVel, r);
    }

    /// <summary>
    /// Returns the shortest rotation vector (axis * angle in radians)
    /// that rotates currentRotation into targetRotation.
    /// </summary>
    public static float3 GetRotErr(
        quaternion currRot,
        quaternion tgtRot
    ) {
        // From current rot to target rot.
        quaternion dRot = math.normalize(math.mul(tgtRot, math.inverse(currRot)));
        // Ensure shortest path.
        // NOTE: This is supposed keep the quaternion on the same hemisphere, thus preventing > 180 degree rotation errors.
        if (dRot.value.w < 0f)
            dRot.value *= -1f;
        float w = math.clamp(dRot.value.w, -1f, 1f);
        // Quaternion to angle axis conversion.
        float ang = 2f * math.acos(w);
        float sinHalfAng = math.sqrt(math.max(1f - w * w, 0f));
        // Avoid dividing by 0.
        if (sinHalfAng < 0.0001f)
            return float3.zero;
        // Normalize axis.
        float3 axis = dRot.value.xyz / sinHalfAng;
        return axis * ang;
    }

    /// <summary>
    /// Transforms a world direction into local space using the rotation from local space to world space.
    /// </summary>
    public static float3 InvrsTrfDir(quaternion wldFromLcl, float3 wldVec) {
        return math.rotate(math.inverse(wldFromLcl), wldVec);
    }

    /// <summary>
    /// Transforms a world rotation into local space using the rotation from local space to world space.
    /// </summary>
    public static quaternion InvrsTrfRot(quaternion wldFromLcl, quaternion wldRot) {
        return math.mul(math.inverse(wldFromLcl), wldRot);
    }

    /// <summary>
    /// Moves a point toward a target position by a maximum distance.
    /// Returns the target if the remaining distance is smaller than maxDelta.
    /// </summary>
    public static float3 MoveTowards(float3 curPos, float3 tgtPos, float maxD) {
        float3 displacement = tgtPos - curPos;
        float mag = math.length(displacement);
        if (mag <= maxD || mag == 0f)
            return tgtPos;
        return curPos + displacement / mag * maxD;
    }

    /// <summary>
    /// Transforms a local direction into world space using the rotation from local space to world space.
    /// </summary>
    public static float3 TrfDir(quaternion wldFromLcl, float3 lclVec) {
        return math.rotate(wldFromLcl, lclVec);
    }

    /// <summary>
    /// Returns the world space location of a local-space point in transform space.
    /// NOTE: The local point is assumed to be in the transform's unscaled local space.
    /// </summary>
    public static float3 TrfPtUnscaled(in LocalTransform trf, float3 lclPt){
        return trf.Position + math.rotate(trf.Rotation, lclPt);
    }

    /// <summary>
    /// Transforms a local rotation into world space using the rotation from parent space to world space.
    /// </summary>
    public static quaternion TrfRot(quaternion wldFromParent, quaternion lclRot) {
        return math.mul(wldFromParent, lclRot);
    }

}
