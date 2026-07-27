using UnityEngine;

public enum Side {
    Left,
    Right,
}

public enum Directions2D {
    Left,
    Right,
    Down,
    Up,
}

public enum Directions3D {
    Left,
    Right,
    Down,
    Up,
    Back,
    Forward,
}

public static class GeneralUtils {
    /// <summary>
    /// Normalizes angle to 0-360 degrees.
    /// </summary>
    public static float Nrm360(float angle) {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    public static bool IsInRange(float x, float greaterThanOrEqualTo, float lessThanOrEqualTo) {
        return x >= greaterThanOrEqualTo && x <= lessThanOrEqualTo;
    }

    // TODO: Perhaps expand this so that parameter takes in the axis and pivot pos and rot instead of the transform. Also write the direction of the rotation.
    /// <summary>
    /// Returns world pos and rot of an object when rotated around the right-axis of a pivot object. 
    /// </summary>
    public static (Vector3, Quaternion) ComputeNewPoseByRotAroundPivotXAxis(
        Transform movedObject, Transform pivotObject,
        float rotationAroundAxis,
        float rotationMult = 1
    ) {
        // NOTE: rotationMult is used here to rotate the object slightly further.
        float deltaXAngle = rotationAroundAxis * rotationMult;
        //Debug.Log("delta x angle: " + deltaXAngle);
        Quaternion dRotAroundPivotRight = Quaternion.AngleAxis(deltaXAngle, pivotObject.right);
        Vector3 movedObjPosInPivotSpace =
            Quaternion.Inverse(pivotObject.rotation) * (movedObject.position - pivotObject.position);
        Quaternion movedObjRotInPivotSpace =
            Quaternion.Inverse(pivotObject.rotation) * movedObject.rotation;
        Quaternion pivotFutureRot = dRotAroundPivotRight * pivotObject.rotation;
        Vector3 movedObjNextPos = pivotObject.position + pivotFutureRot * movedObjPosInPivotSpace;
        Quaternion movedObjNextRot = pivotFutureRot * movedObjRotInPivotSpace;
        return (movedObjNextPos, movedObjNextRot);
    }

    /// <summary>
    /// Transforms point from rigidbody's local space to world space using rb.position.
    /// Does not scale the point, in other words: ignores transform.localScale unlike transform.TransformPoint.
    /// </summary>
    public static Vector3 RbUnscaledTrfPt(Rigidbody rb, Vector3 ptInRbSpace) {
        return rb.rotation * ptInRbSpace + rb.position;
    }

    /// <summary>
    /// Transforms point from world space to rb's local space using rb.position.
    /// Does not scale the point, in other words: ignores transform.localScale
    /// unlike transform.InverseTransformPoint.
    /// </summary>
    public static Vector3 RbUnscaledInvrsTrfPt(Rigidbody rb, Vector3 ptInWorldSpace) {
        return Quaternion.Inverse(rb.rotation) * (ptInWorldSpace - rb.position);
    }

    /// <summary>
    /// Converts a world space rotation into the rigidbody's local space rotation.
    /// </summary>
    public static Quaternion RotFromWorldToRbSpace(Rigidbody rb, Quaternion rotInWorldSpace) {
        return Quaternion.Inverse(rb.rotation) * rotInWorldSpace;
    }

    /// <summary>
    /// Converts a rigidbody's local space rotation into world space rotation.
    /// </summary>
    public static Quaternion RotFromRbSpaceToWorld(Rigidbody rb, Quaternion rotInRbSpace) {
        return rb.rotation * rotInRbSpace;
    }

    /// <summary>
    /// Transforms point from transform's local space to world space.
    /// Does not scale the point, in other words: ignores transform.localScale
    /// unlike transform.TransformPoint.
    /// </summary>
    public static Vector3 UnscaledTrfPt(Transform trf, Vector3 ptInTrfSpace) {
        return trf.rotation * ptInTrfSpace + trf.position;
    }

    /// <summary>
    /// Transforms point from world space to transform's local space.
    /// Does not scale the point, in other words: ignores transform.localScale
    /// unlike transform.InverseTransformPoint.
    /// </summary>
    public static Vector3 UnscaledInvrsTrft(Transform trf, Vector3 pttInWorldSpace) {
        return Quaternion.Inverse(trf.rotation) * (pttInWorldSpace - trf.position);
    }

    /// <summary>
    /// Converts a world space rotation into the transform's local space rotation.
    /// </summary>
    public static Quaternion RotFromWorldToTrfSpace(Transform trf, Quaternion rotInWorldSpace) {
        return Quaternion.Inverse(trf.rotation) * rotInWorldSpace;
    }

    /// <summary>
    /// Converts a transforms's local space rotation into world space rotation.
    /// </summary>
    public static Quaternion RotFromTrfSpaceToWorld(Transform trf, Quaternion rotInTrfSpace) {
        return trf.rotation * rotInTrfSpace;
    }

    /// <summary>
    /// Returns the position and rotation of this grabbable if its child was aligned with target
    /// pos and rot.
    /// </summary>
    public static (Vector3, Quaternion) AlignChildWithTargetPosRot(Transform parentTrf, Transform childTrf, Vector3 tgtWorldPos, Quaternion tgtWorldRot) {
        // Compute the child's current local offset (position + rotation) relative to the parent
        Vector3 childParentSpacePos = UnscaledInvrsTrft(parentTrf, childTrf.position);
        Quaternion childParentSpaceRot = RotFromWorldToTrfSpace(parentTrf, childTrf.rotation);
        // Compute the desired rigidbody transform that would make the child match the target
        Vector3 desiredRbPos = tgtWorldPos - (tgtWorldRot * (Quaternion.Inverse(childParentSpaceRot) * childParentSpacePos));
        Quaternion desiredRbRot = tgtWorldRot * Quaternion.Inverse(childParentSpaceRot);
        return (desiredRbPos, desiredRbRot);
    }
}