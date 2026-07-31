/// <summary>
/// Grabs grabbing a grabbable.
/// </summary>
// TODO: Rename to IGnrGrbs
public interface IGnrGrbs {
    int GrbCount { get; }

    /// <summary>
    /// Delete all grabs from the list.<br/>
    /// NOTE: This is a helper method for complete grab release methods!
    /// </summary>
    void ClearGrbsList();
    IGrb GetGrb(int i);
    /// <summary>
    /// NOTE: This is a helper method for complete grab release methods!
    /// </summary>
    void RemoveGrabFromList(IGrb grb);
}
