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
    public Vector3 initPhysHandPosInGrblSpc = Vector3.zero;
    /// <summary>
    /// Rotation from the grabbed object to the hand when the grab was initialized.
    /// </summary>
    public Quaternion initRotFromGrblToPhysHand = Quaternion.identity;
    /// <summary>
    /// Initial point in phys hand's follow target space that corresponds to the
    /// grip point used for this grab.
    /// </summary>
    public Vector3 followTgtInitGrabPtInFollowTgtSpc = Vector3.zero;

    public Grb(
        GrblJntDriven_PhysHand physHand,
        Vector3 initPhysHandPosInGrblSpc,
        Quaternion initRotFromGrblToPhysHand,
        Vector3 followTgtInitGrabPtInFollowTgtSpc

    ) {
        this.physHand = physHand;
        this.initPhysHandPosInGrblSpc = initPhysHandPosInGrblSpc;
        this.initRotFromGrblToPhysHand = initRotFromGrblToPhysHand;
        this.followTgtInitGrabPtInFollowTgtSpc = followTgtInitGrabPtInFollowTgtSpc;
    }
}
