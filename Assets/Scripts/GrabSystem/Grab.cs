using UnityEngine;

/// <summary>
/// Represent one hand grabbing a grabbable.
/// </summary>
public sealed class Grab {
    /// <summary>
    /// Grabbing hand.
    /// </summary>
    public PhysHand physHand;
    /// <summary>
    /// Unscaled position of the hand in grabbable's local space when the grab was initialized.
    /// </summary>
    public Vector3 initGrabPtPosInGrabbableLocalSpace = Vector3.zero;
    /// <summary>
    /// Rotation from the grabbed object to the hand when the grab was initialized.
    /// </summary>
    public Quaternion initRotFromGrabbableToGrabPt = Quaternion.identity;

    public Grab(
        PhysHand physHand,
        Vector3 initialGrabPointPosInGrabbableLocalSpace,
        Quaternion initialRotFromGrabbableToGrabPoint
    ) {
        this.physHand = physHand;
        this.initGrabPtPosInGrabbableLocalSpace = initialGrabPointPosInGrabbableLocalSpace;
        this.initRotFromGrabbableToGrabPt = initialRotFromGrabbableToGrabPoint;
    }
}
