using System.Collections.Generic;
using UnityEngine;

public class HandJntDriven_Grbs : IGnrGrbsCtrl {
    readonly List<HandJntDriven_Grb> grbs;

    public int GrbCount => grbs.Count;

    public HandJntDriven_Grbs(List<HandJntDriven_Grb> grbs) {
        this.grbs = grbs;
    }

    public void ClearGrbsList() {
        grbs.Clear();
    }

    public IGnrGrbData GetGrb(int i) => grbs[i];

    public void RemoveGrabFromList(IGnrGrbData grb) {
        if (!grbs.Remove((HandJntDriven_Grb)grb)) {
            Debug.LogError($"Could not find grab to remove from {nameof(HandJntDriven_Grbs)}!");
        }
    }
}
