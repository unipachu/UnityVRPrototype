using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// DOTS NOTE: Burst-compatible helper methods for CustomPhysicsJoint.
/// DOTS NOTE: Uses Unity.Mathematics structures like Unity.Mathematics.float3 (instead of UnityEngine.Vector3).
/// </summary>
public static class CustomPhysUtils {
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
    /// Returns the world-space position of a local anchor.
    /// </summary>
    public static float3 GetWorldAnchor(in LocalTransform trf, float3 localAnchor){
        return trf.Position + math.rotate(trf.Rotation, localAnchor);
    }

    /// <summary>
    /// Returns the velocity of a point on the rigidbody.
    /// </summary>
    public static float3 GetPointVelocity(
        float3 linVel,
        float3 angVel,
        float3 point,
        float3 worldCenterOfMass
    ) {
        float3 r = point - worldCenterOfMass;
        // NOTE: Velocity caused by rotation: v = angular velocity X vector from center.
        return linVel + math.cross(angVel, r);
    }

    /// <summary>
    /// Computes a spring-damper force.<br/>
    /// NOTE: Damping force is only affects spring force direction.
    /// </summary>
    public static float3 CalculateLinForce(
        float3 tgtPos,
        float3 currPos,
        float3 currVel,
        float spring,
        //float damper,
        float maxForce
        //bool preventDampingOvershoot
    ) {
        float3 displacement = tgtPos - currPos;
        float dist = math.length(displacement);
        if (dist < 0.0001f)
            return float3.zero;
        float3 springDir = displacement / dist;
        // Spring force
        float3 springForce = displacement * spring;
        // Only damp velocity along the spring axis.
        float velAlongSpring = math.dot(currVel, springDir);
        //float3 dampingForce = -springDir * velAlongSpring * damper;
        //if (preventDampingOvershoot) {
        //    float springForceMag = math.length(springForce);
        //    float dampingForceMag = math.length(dampingForce);
        //    if (dampingForceMag > springForceMag)
        //        dampingForce = -springDir * springForceMag;
        //}
        float3 force = springForce; //+ dampingForce;
        // Clamp maximum force
        float mag = math.length(force);
        if (mag > maxForce)
            force *= maxForce / mag;
        return force;
    }

    /// <summary>
    /// Computes a spring-damper torque.
    /// </summary>
    public static float3 CalculateAngTq(
        quaternion currRot,
        quaternion tgtRot,
        float3 angVel,
        float spring,
        //float damper,
        float maxTq
        //bool preventDampingOvershoot
    ) {
        float3 rotError = GetRotErr(currRot, tgtRot);
        float errorAngle = math.length(rotError);
        if (errorAngle < 0.0001f)
            return float3.zero;
        float3 springAxis = rotError / errorAngle;
        // Spring torque
        float springTqScalar = errorAngle * spring;
        float3 springTq = springAxis * springTqScalar;
        // Damping torque. Only damp angular velocity around the spring axis.
        float angVelAlongAxis = math.dot(angVel, springAxis);
        //float dampingTqScalar = -angVelAlongAxis * damper;
        //if (preventDampingOvershoot && dampingTqScalar < -springTqScalar)
        //    dampingTqScalar = -springTqScalar;
        //float3 dampingTq = springAxis * dampingTqScalar;
        // Final torque
        float3 tq = springTq; //+ dampingTq;
        // Clamp maximum torque
        float mag = math.length(tq);
        if (mag > maxTq && mag > 0f)
            tq *= maxTq / mag;
        return tq;
       
    }

    /// <summary>
    /// Returns the shortest rotation vector (axis * angle in radians)
    /// that rotates currentRotation into targetRotation.
    /// </summary>
    public static float3 GetRotErr(
        quaternion currRot,
        quaternion tgtRot
    ) {
        quaternion dRot = math.normalize(math.mul(tgtRot, math.inverse(currRot)));
        // Ensure shortest path. NOTE: I do not understand how this quaternion black magic works.
        if (dRot.value.w < 0f)
            dRot.value *= -1f;
        float w = math.clamp(dRot.value.w, -1f, 1f);
        float ang = 2f * math.acos(w);
        float sinHalfAng = math.sqrt(math.max(1f - w * w, 0f));
        if (sinHalfAng < 0.0001f)
            return float3.zero;
        float3 axis = dRot.value.xyz / sinHalfAng;
        return axis * ang;
    }
}
