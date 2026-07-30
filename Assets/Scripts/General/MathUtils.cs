using UnityEngine;

/// <summary>
/// Utility methods for mathematical calculations and spatial transformations.
/// </summary>
public static class MathUtils {
    /// <summary>
    /// Calculates the parent world pose that aligns the child with the target world pose.
    /// </summary>
    public static (Vector3, Quaternion) AlignChildToTgtPose(
        Transform parentTrf,
        Transform childTrf,
        Vector3 tgtWldPos,
        Quaternion tgtWldRot
    ) {
        return AlignChildToTgtPose(
            parentTrf.position,
            parentTrf.rotation,
            childTrf.position,
            childTrf.rotation,
            tgtWldPos,
            tgtWldRot
        );
    }

    /// <summary>
    /// Calculates the parent world pose that aligns the child with the target world pose.
    /// NOTE: Parameters use child WORLD position and rotation!
    /// </summary>
    public static (Vector3, Quaternion) AlignChildToTgtPose(
        Vector3 parentPos,
        Quaternion parentRot,
        Vector3 childWldPos,
        Quaternion childWldRot,
        Vector3 tgtWldPos,
        Quaternion tgtWldRot
    ) {
        // Compute the child's current local pose relative to the parent
        Vector3 childParentSpcPos = InvrsTrfPtUnscaled(parentPos, parentRot, childWldPos);
        Quaternion childParentSpcRot = InvrsTrfRot(parentRot, childWldRot);
        return AlignLclPoseToTgtPose(childParentSpcPos, childParentSpcRot, tgtWldPos, tgtWldRot);
    }

    /// <summary>
    /// Returns the transform origin whose local space point is at the given world space position.
    /// Basically, this finds a position for parent where its child (localPoint) is aligned with the worldPoint. 
    /// </summary>
    public static Vector3 AlignLclPtToWldPt(Vector3 wldPt, Quaternion trfRot, Vector3 lclPt) {
        return wldPt - trfRot * lclPt;
    }

    /// <summary>
    /// Returns the transform rotation whose local-space rotation matches the given world-space rotation.
    /// Basically, this finds a rotation for the parent where its child (localRot) is aligned with the worldRot.
    /// </summary>
    public static Quaternion AlignLclRotToWldRot(Quaternion worldRot, Quaternion localRot) {
        return worldRot * Quaternion.Inverse(localRot);
    }

    /// <summary>
    /// Calculates the world pose that aligns a local pose with the target world pose.
    /// </summary>
    public static (Vector3, Quaternion) AlignLclPoseToTgtPose(
        Vector3 lclPos,
        Quaternion lclRot,
        Vector3 tgtWldPos,
        Quaternion tgtWldRot
    ) {
        Quaternion desiredRot = AlignLclRotToWldRot(tgtWldRot, lclRot);
        Vector3 desiredPos = AlignLclPtToWldPt(tgtWldPos, desiredRot, lclPos);
        return (desiredPos, desiredRot);
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
        Vector3 movedTrfPosInPivSpace = InvrsTrfPtUnscaled(pivTrf, movedTrf.position);
        Quaternion movedTrfRotInPivSpace = InvrsTrfRot(pivTrf, movedTrf.rotation);
        Quaternion pivFutureRot = dRotAroundPivRight * pivTrf.rotation;
        Vector3 movedTrfNextWorldPos = TrfPt(pivTrf.position, pivFutureRot, movedTrfPosInPivSpace);
        Quaternion movedTrfNextRot = TrfRot(pivFutureRot, movedTrfRotInPivSpace);
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
    /// NOTE: Call this in FixedUpdate()!<br/>
    /// NOTE #2: Since rigidbodies and transforms can get out of sync, the child pose should be
    /// cached instead of just using a child Transform reference.
    /// </summary>
    /// <param name="rb">Rigidbody to be moved.</param>
    /// <param name="child">Child of the rigidbody we want to align with the target.</param>
    /// <param name="t">Lerp parameter (0-1).</param>
    public static void InterpRbSoChildAlignsWithTgtPose(
        Rigidbody rb,
        Vector3 childLclPos,
        Quaternion childLclRot,
        Vector3 tgtWldPos,
        Quaternion tgtWldRot,
        float t
    ) {
        var targetPose = AlignLclPoseToTgtPose(childLclPos, childLclRot, tgtWldPos, tgtWldRot);
        t = Mathf.Clamp01(t);
        Vector3 newPos = Vector3.Lerp(rb.position, targetPose.Item1, t);
        Quaternion newRot = Quaternion.Slerp(rb.rotation, targetPose.Item2, t);
        rb.Move(newPos, newRot);
    }

    /// <summary>
    /// Transforms a point from world space to unscaled local space,
    /// ignoring the transform's scale (unlike Transform.InverseTransformPoint).
    /// </summary>
    public static Vector3 InvrsTrfPtUnscaled(Transform trf, Vector3 ptInWldSpc) {
        return InvrsTrfPtUnscaled(trf.position, trf.rotation, ptInWldSpc);
    }

    /// <summary>
    /// Transforms a point from world space to unscaled Rigidbody local space,
    /// using the Rigidbody's position and rotation.
    /// </summary>
    public static Vector3 InvrsTrfPtUnscaled(Rigidbody rb, Vector3 ptInWldSpc) {
        return InvrsTrfPtUnscaled(rb.position, rb.rotation, ptInWldSpc);
    }

    /// <summary>
    /// Transforms a point from world space to frame pose's local space,
    /// using the Rigidbody's position and rotation.
    /// </summary>
    public static Vector3 InvrsTrfPtUnscaled(Vector3 framePos, Quaternion frameRot, Vector3 ptInWldSpc) {
        return Quaternion.Inverse(frameRot) * (ptInWldSpc - framePos);
    }

    /// <summary>
    /// Converts a world space rotation into the frame rotations's local space rotation.
    /// </summary>
    public static Quaternion InvrsTrfRot(Quaternion frameRot, Quaternion worldRot) {
        return Quaternion.Inverse(frameRot) * worldRot;
    }

    /// <summary>
    /// Converts a world space rotation into the rigidbody's local space rotation.
    /// </summary>
    public static Quaternion InvrsTrfRot(Rigidbody rb, Quaternion rotInWorldSpace) {
        return InvrsTrfRot(rb.rotation, rotInWorldSpace);
    }

    /// <summary>
    /// Converts a world space rotation into the transform's local space rotation.
    /// </summary>
    public static Quaternion InvrsTrfRot(Transform trf, Quaternion rotInWorldSpace) {
        return InvrsTrfRot(trf.rotation, rotInWorldSpace);
    }

    public static bool IsInRange(float x, float greaterThanOrEqualTo, float lessThanOrEqualTo) {
        Debug.Assert(
            greaterThanOrEqualTo <= lessThanOrEqualTo,
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
    /// Returns a transformed point from local space to world space using the specified
    /// origin and rotation.
    /// </summary>
    public static Vector3 TrfPt(Vector3 framePos, Quaternion frameRot, Vector3 lclPt) {
        return frameRot * lclPt + framePos;
    }

    /// <summary>
    /// Transforms a point from unscaled local space to world space,
    /// ignoring the transform's scale (unlike Transform.TransformPoint).
    /// </summary>
    public static Vector3 TrfPtUnscaled(Transform trf, Vector3 ptInTrfSpace) {
        return TrfPt(trf.position, trf.rotation, ptInTrfSpace);
    }

    /// <summary>
    /// Transforms a point from unscaled Rigidbody local space to world space,
    /// using the Rigidbody's position and rotation.
    /// </summary>
    public static Vector3 TrfPtUnscaled(Rigidbody rb, Vector3 ptInRbSpace) {
        return TrfPt(rb.position, rb.rotation, ptInRbSpace);
    }

    /// <summary>
    /// Converts a transforms's local space rotation into world space rotation.
    /// </summary>
    public static Quaternion TrfRot(Transform trf, Quaternion rotInTrfSpace) {
        return TrfRot(trf.rotation, rotInTrfSpace);
    }

    /// <summary>
    /// Transforms a local-space rotation into world space using the given frame rotation.
    /// </summary>
    public static Quaternion TrfRot(Quaternion frameWldRot, Quaternion lclRot) {
        return frameWldRot * lclRot;
    }

    /// <summary>
    /// Converts a rigidbody's local space rotation into world space rotation.
    /// </summary>
    public static Quaternion TrfRot(Rigidbody rb, Quaternion rotInRbSpace) {
        return TrfRot(rb.rotation, rotInRbSpace);
    }
}