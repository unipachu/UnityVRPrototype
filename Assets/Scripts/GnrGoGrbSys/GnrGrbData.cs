using UnityEngine;

/// <summary>
/// Generic grab data shared by all different game object grab systems.
/// </summary>
public sealed class GnrGrbData {
    /// <summary>
    /// Physics hand grabbing the grabbable.
    /// </summary>
    public IGnrPhysHand gnrPhysHand;
    /// <summary>
    /// Unscaled position of the hand in grabbable's local space when the grab was initialized.
    /// </summary>
    public Vector3 initPhysHandPosInGrblSpc = Vector3.zero;
    /// <summary>
    /// Rotation from the grabbed object to the hand when the grab was initialized.
    /// </summary>
    public Quaternion initRotFromGrblToPhysHand = Quaternion.identity;
    /// <summary>
    /// Theoretical grab point in the physics hand's follow target space, as if the
    /// follow target had grabbed the grabbable the same way as the physics hand.
    /// </summary>
    public Vector3 theoInitGrbPtInFolTgtSpc = Vector3.zero;

    public GnrGrbData(
        IGnrPhysHand gnrPhysHand,
        Vector3 initPhysHandPosInGrblSpc,
        Quaternion initRotFromGrblToPhysHand,
        Vector3 theoInitGrbPtInFolTgtSpc
    ) {
        this.gnrPhysHand = gnrPhysHand;
        this.initPhysHandPosInGrblSpc = initPhysHandPosInGrblSpc;
        this.initRotFromGrblToPhysHand = initRotFromGrblToPhysHand;
        this.theoInitGrbPtInFolTgtSpc = theoInitGrbPtInFolTgtSpc;
    }
}
