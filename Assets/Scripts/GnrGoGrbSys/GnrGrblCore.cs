// TODO: Delete
//using System.Collections.Generic;
//using UnityEngine;

//public class GnrGrblCore<TGrb, TPhysHand> : MonoBehaviour
//    where TGrb : class, IGrb<TPhysHand>
//    where TPhysHand : MonoBehaviour {

//    public readonly List<TGrb> grbs = new(2);

//    /// <summary>
//    /// Finds a grab by the physics hand.
//    /// </summary>
//    public TGrb FindGrb(TPhysHand physHand) {
//        for (int i = 0; i < grbs.Count; i++) {
//            if (ReferenceEquals(grbs[i].PhysHand, physHand))
//                return grbs[i];
//        }

//        Debug.LogError($"{physHand.name} was not grabbing {gameObject.name}!", this);
//        return null;
//    }

//    /// <summary>
//    /// Finds the index of the grabbing physics hand.
//    /// </summary>
//    public int FindGrbIndex(TPhysHand physHand) {
//        for (int i = 0; i < grbs.Count; i++) {
//            if (ReferenceEquals(grbs[i].PhysHand, physHand))
//                return i;
//        }

//        Debug.LogError($"{physHand.name} was not grabbing {gameObject.name}!", this);
//        return -1;
//    }

//    /// <summary>
//    /// How many physics hands of the specified side are grabbing this?
//    /// </summary>
//    public int GrbCount(Side handSide) {
//        int counter = 0;

//        foreach (TGrb grb in grbs) {
//            if (grb.PhysHand.side == handSide)
//                counter++;
//        }

//        return counter;
//    }

//    /// <summary>
//    /// Is this physics hand grabbing this grabbable?
//    /// </summary>
//    public bool IsGrabbing(TPhysHand physHand) {
//        for (int i = 0; i < grbs.Count; i++) {
//            if (ReferenceEquals(grbs[i].PhysHand, physHand))
//                return true;
//        }

//        return false;
//    }
//}