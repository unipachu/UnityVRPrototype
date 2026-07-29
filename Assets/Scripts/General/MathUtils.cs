using UnityEngine;

/// <summary>
/// Utility methods for mathematical calculations and spatial transformations.
/// </summary>
public static class MathUtils {
    /// <summary>
    /// Calculates the parent pose that aligns the child with the target world pose.
    /// </summary>
    public static (Vector3, Quaternion) AlignChildWithTgtPose(
        Transform parentTrf,
        Transform childTrf,
        Vector3 tgtWorldPos,
        Quaternion tgtWorldRot
    ) {
        // Compute the child's current local offset (position + rotation) relative to the parent
        Vector3 childParentSpcPos = UnscaledInvrsTrfPt(parentTrf, childTrf.position);
        Quaternion childParentSpcRot = RotFromWorldToTrfSpace(parentTrf, childTrf.rotation);
        // Compute the desired rigidbody transform that would make the child match the target
        Vector3 desiredRbPos = tgtWorldPos - (tgtWorldRot * (Quaternion.Inverse(childParentSpcRot) * childParentSpcPos));
        Quaternion desiredRbRot = tgtWorldRot * Quaternion.Inverse(childParentSpcRot);
        return (desiredRbPos, desiredRbRot);
    }

    // TODO: Perhaps expand this so that parameter takes in the axis and pivot pos and rot instead of
    // TODO C: the transform. Also write the direction of the rotation.
    /// <summary>
    /// Returns world pos and rot of an object when rotated around the right-axis of a pivot object. 
    /// </summary>
    public static (Vector3, Quaternion) ComputeNewPoseByRotAroundPivTrfXAxis(
        Transform movedTrf,
        Transform pivTrf,
        float rotAroundAxis,
        float rotMult = 1
    ) {
        // NOTE: rotMult is used here to rotate the object slightly further.
        float dXAng = rotAroundAxis * rotMult;
        //Debug.Log("delta x angle: " + deltaXAngle);
        Quaternion dRotAroundPivRight = Quaternion.AngleAxis(dXAng, pivTrf.right);
        Vector3 movedTrfPosInPivSpace = UnscaledInvrsTrfPt(pivTrf, movedTrf.position);
        //Vector3 movedTrfPosInPivSpace =
        //  Quaternion.Inverse(pivTrf.rotation) * (movedTrf.position - pivTrf.position);
        Quaternion movedTrfRotInPivSpace = RotFromWorldToTrfSpace(pivTrf, movedTrf.rotation);
        //Quaternion movedTrfRotInPivSpace = Quaternion.Inverse(pivTrf.rotation) * movedTrf.rotation;
        Quaternion pivFutureRot = dRotAroundPivRight * pivTrf.rotation;
        Vector3 movedTrfNextWorldPos = TrfPt(pivTrf.position, pivFutureRot, movedTrfPosInPivSpace);
        //Vector3 movedTrfNextPos = pivTrf.position + pivFutureRot * movedTrfPosInPivSpace;
        Quaternion movedTrfNextRot = pivFutureRot * movedTrfRotInPivSpace;
        return (movedTrfNextWorldPos, movedTrfNextRot);
    }

    /// <summary>
    /// Returns signed angle around an axis (in radians).<br/>
    /// Can be used to e.g. see how a hand quaternion rotation affects the rotation of an (axis-locked)
    /// key in a key hole.
    /// </summary>
    public static float ExtractSignedTwistAng(Quaternion rot, Vector3 axis) {
        // The dot product requires normalized axis.
        axis.Normalize();
        // Ensure equivalent quaternions are represented consistently. This prevents discontinuities where
        // the same rotation can appear as two different quaternions.
        if (rot.w < 0f)
            rot = new Quaternion(
                -rot.x,
                -rot.y,
                -rot.z,
                -rot.w
            );
        // Quaternion is projected onto the axis vector to only keep the part of the rotation
        // around the axis.
        Vector3 projected = Vector3.Project(new Vector3(rot.x, rot.y, rot.z), axis);
        Quaternion twist = new Quaternion(projected.x, projected.y, projected.z, rot.w);
        // Valid quaternion needs to have length 1.
        twist.Normalize();
        // NOTE: It is unintuitive that any orientation can be represented by angle axis.
        // TODO: Are 180 degree rotations undefined?
        twist.ToAngleAxis(out float angleDeg, out Vector3 twistAxis);
        // Make sure the original axis and twist axis point in the same direction.
        if (Vector3.Dot(twistAxis, axis) < 0f)
            angleDeg = -angleDeg;
        return angleDeg * Mathf.Deg2Rad;
    }

    /// <summary>
    /// Interpolates rb's pose with rb.Move to align the specified child transform with a target pose.<br/>
    /// NOTE: Call this in FixedUpdate().
    /// </summary>
    /// <returns>Returns wether the interpolation is finished.</returns>
    public static void InterpToAlignChildWithTgt(Rigidbody rb, Transform child, Vector3 tgtPos, Quaternion tgtRot, float t) {
        var targetPose = AlignChildWithTgtPose(rb.transform, child, tgtPos, tgtRot);
        t = Mathf.Clamp01(t);
        Vector3 newPos = Vector3.Lerp(rb.position, targetPose.Item1, t);
        Quaternion newRot = Quaternion.Slerp(rb.rotation, targetPose.Item2, t);
        rb.Move(newPos, newRot);
    }

    public static bool IsInRange(float x, float greaterThanOrEqualTo, float lessThanOrEqualTo) {
        Debug.Assert(
            greaterThanOrEqualTo < lessThanOrEqualTo,
            $"Invalid range: lower bound ({greaterThanOrEqualTo}) must be less than or equal to " +
            $"upper bound ({lessThanOrEqualTo})."
        );
        return x >= greaterThanOrEqualTo && x <= lessThanOrEqualTo;
    }

    /// <summary>
    /// Normalizes angle to 0-360 degrees.
    /// </summary>
    public static float Nrm360(float ang) {
        ang %= 360f;
        if (ang < 0f) ang += 360f;
        return ang;
    }

    /// <summary>
    /// Transforms a point from world space to unscaled Rigidbody local space,
    /// using the Rigidbody's position and rotation.
    /// </summary>
    public static Vector3 RbUnscaledInvrsTrfPt(Rigidbody rb, Vector3 ptInWorldSpace) {
        return Quaternion.Inverse(rb.rotation) * (ptInWorldSpace - rb.position);
    }

    /// <summary>
    /// Transforms a point from unscaled Rigidbody local space to world space,
    /// using the Rigidbody's position and rotation.
    /// </summary>
    public static Vector3 RbUnscaledTrfPt(Rigidbody rb, Vector3 ptInRbSpace) {
        return rb.rotation * ptInRbSpace + rb.position;
    }

    /// <summary>
    /// Converts a rigidbody's local space rotation into world space rotation.
    /// </summary>
    public static Quaternion RotFromRbSpaceToWorld(Rigidbody rb, Quaternion rotInRbSpace) {
        return rb.rotation * rotInRbSpace;
    }

    /// <summary>
    /// <summary>
    /// Converts a transforms's local space rotation into world space rotation.
    /// </summary>
    public static Quaternion RotFromTrfSpaceToWorld(Transform trf, Quaternion rotInTrfSpace) {
        return trf.rotation * rotInTrfSpace;
    }

    /// Converts a world space rotation into the rigidbody's local space rotation.
    /// </summary>
    public static Quaternion RotFromWorldToRbSpace(Rigidbody rb, Quaternion rotInWorldSpace) {
        return Quaternion.Inverse(rb.rotation) * rotInWorldSpace;
    }

    /// <summary>
    /// Converts a world space rotation into the transform's local space rotation.
    /// </summary>
    public static Quaternion RotFromWorldToTrfSpace(Transform trf, Quaternion rotInWorldSpace) {
        return Quaternion.Inverse(trf.rotation) * rotInWorldSpace;
    }

    /// <summary>
    /// Transforms a point from local space to world space using the specified
    /// origin and rotation.
    /// </summary>
    public static Vector3 TrfPt(Vector3 origin, Quaternion rotation, Vector3 localPt) {
        return rotation * localPt + origin;
    }

    /// <summary>
    /// Transforms a point from world space to unscaled local space,
    /// ignoring the transform's scale (unlike Transform.InverseTransformPoint).
    /// </summary>
    public static Vector3 UnscaledInvrsTrfPt(Transform trf, Vector3 pttInWorldSpace) {
        return Quaternion.Inverse(trf.rotation) * (pttInWorldSpace - trf.position);
    }

    /// <summary>
    /// Transforms a point from unscaled local space to world space,
    /// ignoring the transform's scale (unlike Transform.TransformPoint).
    /// </summary>
    public static Vector3 UnscaledTrfPt(Transform trf, Vector3 ptInTrfSpace) {
        return trf.rotation * ptInTrfSpace + trf.position;
    }
}