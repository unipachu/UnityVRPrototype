/// <summary>
/// Generic controller that can do basic operations on a grabs collection.<br/>
/// NOTE: This is needed because generic collections in C# are invariant, so
/// polymorphism cannot be used to treat a List&lt;GrblJntDriven_Grb&gt; as a
/// List&lt;IGnrGrbData&gt;.
/// </summary>
public interface IGnrGrbsCtrl {
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
