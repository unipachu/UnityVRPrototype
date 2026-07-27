using UnityEngine;

/// <summary>
/// Represent a grab - one physics hand grabbing a grabbable.
/// </summary>
public sealed class Grb {
    /// <summary>
    /// Grabbing hand.
    /// </summary>
    public GrblJntDriven_PhysHand physHand;
    /// <summary>
    /// Unscaled position of the hand in grabbable's local space when the grab was initialized.
    /// </summary>
    public Vector3 initPhysHandPosInGrblLocalSpace = Vector3.zero;
    /// <summary>
    /// Rotation from the grabbed object to the hand when the grab was initialized.
    /// </summary>
    public Quaternion initRotFromGrblToPhysHand = Quaternion.identity;

    public Grb(
        GrblJntDriven_PhysHand physHand,
        Vector3 initPhysHandPosInGrblLocalSpace,
        Quaternion initRotFromGrblToPhysHand
    ) {
        this.physHand = physHand;
        this.initPhysHandPosInGrblLocalSpace = initPhysHandPosInGrblLocalSpace;
        this.initRotFromGrblToPhysHand = initRotFromGrblToPhysHand;
    }
}
