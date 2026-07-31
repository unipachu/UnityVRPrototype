// TODO: You could also have a even more generic version where instead of TPhysHand, we use IGnrPhysHand.
public interface IGrb<TPhysHand> where TPhysHand : IGnrPhysHand {
    TPhysHand PhysHand { get; }
}
