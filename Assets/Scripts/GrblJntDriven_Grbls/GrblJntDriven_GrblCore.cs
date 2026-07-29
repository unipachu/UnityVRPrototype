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
    /// </summary>
    public Grb FindGrb(GrblJntDriven_PhysHand physHand) {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].physHand == physHand)
                return grbs[i];
        }
        Debug.LogError($"{physHand.name} was not grabbing {gameObject.name}!", this);
        return null;
    }

    /// <summary>
    /// Finds the index of the grabbing <see cref="GrblJntDriven_PhysHand"/>.
    /// </summary>
    public int FindGrbIndex(GrblJntDriven_PhysHand physHand) {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].physHand == physHand)
                return i;
        }
        Debug.LogError($"{physHand.name} was not grabbing {gameObject.name}!", this);
        return -1;
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

    /// <summary>
    /// Is this <see cref="GrblJntDriven_PhysHand"/> grabbing this grabbable?
    /// </summary>
    public bool IsGrabbing(GrblJntDriven_PhysHand physHand) {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].physHand == physHand)
                return true;
        }
        return false;
    }

    public float DistBetweenRbNPhysHandFollowTgt(int grbI) {
        return Vector3.Distance(rb.position, grbs[grbI].physHand.followTgtTrf.position);
    }
}
