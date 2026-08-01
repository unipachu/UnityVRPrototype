using System.Collections.Generic;
using UnityEngine;

// TODO: Remove I from name.
public class IGrblJntDriven_Grbs : IGnrGrbsCtrl {
    readonly List<GrblJntDriven_Grb> grbs;

    public int GrbCount => grbs.Count;

    public IGrblJntDriven_Grbs(List<GrblJntDriven_Grb> grbs) {
        this.grbs = grbs;
    }

    public void ClearGrbsList() {
        grbs.Clear();
    }

    public IGnrGrbData GetGrb(int i) => grbs[i];

    public void RemoveGrabFromList(IGnrGrbData grb) {
        if (!grbs.Remove((GrblJntDriven_Grb)grb)) {
            Debug.LogError($"Could not find grab to remove from {nameof(IGrblJntDriven_Grbs)}!");
        }
    }
}
