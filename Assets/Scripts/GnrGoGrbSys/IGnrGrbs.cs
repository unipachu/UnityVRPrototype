/// <summary>
/// Grabs grabbing a grabbable.
/// </summary>
public interface IGnrGrbs {
    int GrbCount { get; }

    /// <summary>
    /// Delete all grabs from the list.<br/>
    /// NOTE: This is a helper method for complete grab release methods!
    /// </summary>
    void ClearGrbsList();
    IGnrGrbData GetGrb(int i);
    /// <summary>
    /// NOTE: This is a helper method for complete grab release methods!
    /// </summary>
    void RemoveGrabFromList(IGnrGrbData grb);
}
