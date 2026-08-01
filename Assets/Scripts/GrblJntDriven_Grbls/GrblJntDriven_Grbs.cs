using System.Collections.Generic;
using UnityEngine;

public class GrblJntDriven_Grbs : IGnrGrbsCtrl {
    readonly List<GrblJntDriven_Grb> grbs;

    public int GrbCount => grbs.Count;

    public GrblJntDriven_Grbs(List<GrblJntDriven_Grb> grbs) {
        this.grbs = grbs;
    }

    public void ClearGrbsList() {
        grbs.Clear();
    }

    public IGnrGrbData GetGrb(int i) => grbs[i];

    public void RemoveGrabFromList(IGnrGrbData grb) {
        if (!grbs.Remove((GrblJntDriven_Grb)grb)) {
            Debug.LogError($"Could not find grab to remove from {nameof(GrblJntDriven_Grbs)}!");
        }
    }
}
