/// <summary>
/// Represent a hand-joint-driven grab - one physics hand grabbing a grabbable.
/// </summary>
// TODO: Could these classes be more generic since they are basically the same except for the
// TODO C: phys hand type? Maybe not.
public class HandJntDriven_Grb : IGnrGrbData {
    /// <summary>
    /// Grabbing hand.
    /// </summary>
    public HandJntDriven_PhysHand physHand;
    /// <summary>
    /// Common grab data.
    /// </summary>
    public GnrGrbData gnrGrb;

    public GnrGrbData GnrGrbData => gnrGrb;
    public IGnrPhysHand PhysHand => physHand;

    public HandJntDriven_Grb(HandJntDriven_PhysHand physHand, GnrGrbData gnrGrb) {
        this.physHand = physHand;
        this.gnrGrb = gnrGrb;
    }
}
