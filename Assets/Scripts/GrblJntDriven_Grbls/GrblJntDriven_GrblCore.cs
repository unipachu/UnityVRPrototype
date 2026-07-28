using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared core features of grabbable's grab joint driven grabbables.
/// </summary>
public class GrblJntDriven_GrblCore : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody rb;
    // TODO: This is really the only GrblJntDriven grabbable specific field, so if you'd want to
    // TODO C: make this class common between all game object-based grabbables, then maybe
    // TODO C: move the joint to IGrblJntDriven_Grbl. Then you could rename this "Grbs".
    public ConfigurableJoint grbJnt;
    
    public readonly List<Grb> grbs = new(2);

    /// <summary>
    /// Finds <see cref="Grb"/> by the <see cref="GrblJntDriven_PhysHand"/>.
    /// If <see cref="GrblJntDriven_PhysHand"/> is not grabbing this, returns null.
    /// </summary>
    public Grb FindGrb(GrblJntDriven_PhysHand physHand) {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].physHand == physHand)
                return grbs[i];
        }
        Debug.LogWarning($"{physHand.name} was not grabbing {gameObject.name}!", this);
        return null;
    }

    /// <summary>
    /// How many <see cref="GrblJntDriven_PhysHand"/>s of the specified side are grabbing this?
    /// </summary>
    public int GrbCount(Side handSide) {
        int counter = 0;
        foreach (Grb grab in grbs) {
            if (grab.physHand.side == handSide)
                counter++;
        }
        return counter;
    }
}
