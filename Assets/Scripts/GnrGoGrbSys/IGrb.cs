// TODO: You could also have a even more generic version where instead of TPhysHand, we use IGnrPhysHand.
// TODO: Try and remove the generic phys hand type!
public interface IGrb<TPhysHand> where TPhysHand : IGnrPhysHand {
    TPhysHand PhysHand { get; }
    GnrGrb GnrGrb { get; }
}
