using UnityEngine;

public enum Side {
    Left,
    Right,
}

public enum Directions2D {
    Left,
    Right,
    Up,
    Down,
}

public enum Directions3D {
    Left,
    Right,
    Up,
    Down,
    Forward,
    Backward,
}

public static class GeneralUtils {
    /// <summary>
    /// Normalizes angle to 0-360 degrees.
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static float Normalize360(float angle) {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    public static bool IsInRange(float x, float greaterThanOrEqualTo, float lessThanOrEqualTo) {
        return x >= greaterThanOrEqualTo && x <= lessThanOrEqualTo;
    }

    // TODO: Perhaps expand this so that parameter takes in the axis instead of the transform. Also write the direction of the rotation.
    /// <summary>
    /// Returns world pos and rot of an object when rotated around the right-axis of a pivot object. 
    /// </summary>
    public static (Vector3, Quaternion) ComputeNewPosRotByRotationAroundPivotXAxis(Transform movedObject, Transform pivotObject, float rotationAroundAxis, float rotationMult = 1) {
        // NOTE: rotationMult is used here to rotate the object slightly further.
        float deltaXAngle = rotationAroundAxis * rotationMult;

        //Debug.Log("delta x angle: " + deltaXAngle);

        Quaternion deltaRotAroundPivotRight = Quaternion.AngleAxis(deltaXAngle, pivotObject.right);
        Vector3 movedObjectPosInPivotSpace = Quaternion.Inverse(pivotObject.rotation) * (movedObject.position - pivotObject.position);
        Quaternion movedObjectRotInPivotSpace = Quaternion.Inverse(pivotObject.rotation) * movedObject.rotation;

        Quaternion pivotFutureRot = deltaRotAroundPivotRight * pivotObject.rotation;
        Vector3 movedObjectNextPos = pivotObject.position + pivotFutureRot * movedObjectPosInPivotSpace;
        Quaternion movedObjectNextRot = pivotFutureRot * movedObjectRotInPivotSpace;

        return (movedObjectNextPos, movedObjectNextRot);
    }

    /// <summary>
    /// Transforms point from rigidbody's local space to world space using rb.position.
    /// Does not scale the point, in other words: ignores transform.localScale unlike transform.TransformPoint.
    /// </summary>
    public static Vector3 RigidbodyUnscaledTransformPoint(Rigidbody rb, Vector3 pointInRbSpace) {
        return rb.rotation * pointInRbSpace + rb.position;
    }

    /// <summary>
    /// Transforms point from world space to rb's local space using rb.position.
    /// Does not scale the point, in other words: ignores transform.localScale unlike transform.InverseTransformPoint.
    /// </summary>
    public static Vector3 RbUnscaledInvrsTrfPt(Rigidbody rb, Vector3 pointInWorldSpace) {
        return Quaternion.Inverse(rb.rotation) * (pointInWorldSpace - rb.position);
    }

    /// <summary>
    /// Converts a world space rotation into the rigidbody's local space rotation.
    /// </summary>
    public static Quaternion RotFromWorldToRbSpace(Rigidbody rb, Quaternion rotationInWorldSpace) {
        return Quaternion.Inverse(rb.rotation) * rotationInWorldSpace;
    }

    /// <summary>
    /// Converts a rigidbody's local space rotation into world space rotation.
    /// </summary>
    public static Quaternion RotationFromRbSpaceToWorld(Rigidbody rb, Quaternion rotationInRbSpace) {
        return rb.rotation * rotationInRbSpace;
    }

    /// <summary>
    /// Transforms point from transform's local space to world space.
    /// Does not scale the point, in other words: ignores transform.localScale unlike transform.TransformPoint.
    /// </summary>
    public static Vector3 UnscaledTransformPoint(Transform transform, Vector3 pointInTransformSpace) {
        return transform.rotation * pointInTransformSpace + transform.position;
    }

    /// <summary>
    /// Transforms point from world space to transform's local space.
    /// Does not scale the point, in other words: ignores transform.localScale unlike transform.InverseTransformPoint.
    /// </summary>
    public static Vector3 UnscaledInverseTransformPoint(Transform transform, Vector3 pointInWorldSpace) {
        return Quaternion.Inverse(transform.rotation) * (pointInWorldSpace - transform.position);
    }

    /// <summary>
    /// Converts a world space rotation into the transform's local space rotation.
    /// </summary>
    public static Quaternion RotationFromWorldToTransformSpace(Transform transform, Quaternion rotationInWorldSpace) {
        return Quaternion.Inverse(transform.rotation) * rotationInWorldSpace;
    }

    /// <summary>
    /// Converts a transforms's local space rotation into world space rotation.
    /// </summary>
    public static Quaternion RotationFromTransformSpaceToWorld(Transform transform, Quaternion rotationInTransformSpace) {
        return transform.rotation * rotationInTransformSpace;
    }
}