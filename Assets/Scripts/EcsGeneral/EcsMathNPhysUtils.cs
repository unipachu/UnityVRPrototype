using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// DOTS NOTE: Burst-compatible helper methods for CustomPhysicsJoint.
/// DOTS NOTE: Uses Unity.Mathematics structures like Unity.Mathematics.float3 (instead of UnityEngine.Vector3).
/// </summary>
public static class EcsMathNPhysUtils {
    /// <summary>
    /// Calculates added world angular velocity caused by angular impulse to rigidbody.
    /// </summary>
    /// <param name="worldAngImpulse">Torque impulse in world space.</param>
    /// <param name="rot">Current rigidbody rotation.</param>
    /// <param name="inverseInertia">Local space inertia inversed.</param>
    public static float3 ApplyInverseInertia(
        float3 worldAngImpulse,
        quaternion rot,
        float3 inverseInertia
    ) {
        // Convert impulse into local space
        float3 localImpulse = math.rotate(math.inverse(rot), worldAngImpulse);
        // Apply inertia tensor
        float3 localAngVel = localImpulse * inverseInertia;
        // Convert back to world space
        return math.rotate(rot, localAngVel);
    }

    /// <summary>
    /// Computes Torque = springStiffness * rotErr - relativeAngVel * damper.
    /// </summary>
    public static float3 CalculateSpringAngTq(
        quaternion currRot,
        quaternion tgtRot,
        float3 relativeAngVel,
        float springStiffness,
        float damper,
        float maxTq,
        float rotDeadzone = 0.001f,
        float velDeadzone = 0.001f
    ) {
        float3 rotError = GetRotErr(currRot, tgtRot);
        if (
            math.lengthsq(rotError) < rotDeadzone * rotDeadzone &&
            math.lengthsq(relativeAngVel) < velDeadzone * velDeadzone
        )
            return float3.zero;
        float3 springTq = rotError * springStiffness;
        float3 dampingTq = -relativeAngVel * damper;
        float3 tq = springTq + dampingTq;
        float mag = math.length(tq);
        if (mag > maxTq)
            tq *= maxTq / mag;
        return tq;
    }

    /// <summary>
    /// Computes F = springStiffness * distToTgt - relativeVel * damper.<br/>
    /// </summary>
    /// <param name="relativeVel">This pos vel in tgt pos space.</param>
    public static float3 CalculateSpringLinForce(
        float3 tgtPos,
        float3 currPos,
        float3 relativeVel,
        float springStiffness,
        float damper,
        float maxForce,
        float posDeadzone = 0.001f,
        float velDeadzone = 0.001f
    ) {
        float3 displacement = tgtPos - currPos;
        // NOTE: lengthsq < deadzone * deadzone is more performant than
        // NOTE C: length < deadzone because it does not calculate squareroot.
        if (
            math.lengthsq(displacement) < posDeadzone * posDeadzone &&
            math.lengthsq(relativeVel) < velDeadzone * velDeadzone
        )
            return float3.zero;
        float3 springForce = displacement * springStiffness;
        float3 dampingForce = -relativeVel * damper;
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
        float3 worldCenterOfMass
    ) {
        float3 r = point - worldCenterOfMass;
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
    /// Moves a point toward a target position by a maximum distance.
    /// Returns the target if the remaining distance is smaller than maxDelta.
    /// </summary>
    public static float3 MoveTowards(float3 current, float3 target, float maxDelta) {
        float3 displacement = target - current;
        float magnitude = math.length(displacement);
        if (magnitude <= maxDelta || magnitude == 0f)
            return target;
        return current + displacement / magnitude * maxDelta;
    }

    /// <summary>
    /// Returns the world space location of a local-space point in transform space.
    /// NOTE: The local point is assumed to be in the transform's unscaled local space.
    /// </summary>
    public static float3 TransformPointIgnoreScale(in LocalTransform trf, float3 localPoint){
        return trf.Position + math.rotate(trf.Rotation, localPoint);
    }
}
