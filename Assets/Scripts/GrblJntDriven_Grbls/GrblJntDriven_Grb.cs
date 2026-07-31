/// <summary>
/// Represent a grabbable joint driven grab - one physics hand grabbing a grabbable.
/// </summary>
public sealed class GrblJntDriven_Grb : IGnrGrbData {
    /// <summary>
    /// Grabbing hand.
    /// </summary>
    public GrblJntDriven_PhysHand physHand;
    /// <summary>
    /// Common grab data.
    /// </summary>
    public GnrGrbData gnrGrb;
    
    public GnrGrbData GnrGrbData => gnrGrb;

    public IGnrPhysHand PhysHand => physHand;

    public GrblJntDriven_Grb(GrblJntDriven_PhysHand physHand, GnrGrbData gnrGrb) {
        this.physHand = physHand;
        this.gnrGrb = gnrGrb;
    }
}
