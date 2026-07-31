/// <summary>
/// Represent a grabbable joint driven grab - one physics hand grabbing a grabbable.
/// </summary>
public sealed class GrblJntDriven_Grb : IGrb<GrblJntDriven_PhysHand> {
    /// <summary>
    /// Grabbing hand.
    /// </summary>
    public GrblJntDriven_PhysHand physHand;
    /// <summary>
    /// Common grab data.
    /// </summary>
    public GnrGrb gnrGrb;
    
    public GrblJntDriven_PhysHand PhysHand => physHand;

    public GnrGrb GnrGrb => gnrGrb;

    public GrblJntDriven_Grb(GrblJntDriven_PhysHand physHand, GnrGrb gnrGrb) {
        this.physHand = physHand;
        this.gnrGrb = gnrGrb;
    }
}
