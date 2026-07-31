using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utilities for game object grabbables.
/// </summary>
public static class GrblUtils {
    /// <summary>
    /// Distance between the grabbable's current rigidbody position and the theoretical grabbable position
    /// if the follow target (i.e. hand controller) would be grabbing it like the phys hand's initial grab
    /// of the grabbable.
    /// /// </summary>
    // TODO: Make generic.
    public static float DistBetweenGrblRbPosNTheoFolTgtGrblPos(Rigidbody rb, GrblJntDriven_Grb grb) {
        return Vector3.Distance(rb.position, TheoFolTgtGrblPos< GrblJntDriven_Grb, GrblJntDriven_PhysHand>(grb));
    }

    /// <summary>
    /// Finds the grab for the specified physics hand.
    /// </summary>
    public static TGrb FindGrb<TGrb, TPhysHand>(
        List<TGrb> grbs,
        TPhysHand physHand,
        MonoBehaviour grblCtx
    )
    where TGrb : class, IGrb<TPhysHand>
    where TPhysHand : MonoBehaviour, IGnrPhysHand {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].PhysHand == physHand)
                return grbs[i];
        }
        Debug.LogError($"{physHand.name} was not grabbing {grblCtx.name}!", grblCtx);
        return null;
    }

    /// <summary>
    /// Finds the index of the grab for the specified phys hand.
    /// </summary>
    public static int FindGrbIndex<TGrb, TPhysHand>(
        List<TGrb> grbs,
        TPhysHand physHand,
        MonoBehaviour grbl
    )
    where TGrb : class, IGrb<TPhysHand>
    where TPhysHand : MonoBehaviour, IGnrPhysHand {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].PhysHand == physHand)
                return i;
        }
        Debug.LogError($"{physHand.name} was not grabbing {grbl.name}!", grbl);
        return -1;
    }

    /// <summary>
    /// Finds follow target grap point expressed in grabbable space.
    /// </summary>
    public static Vector3 FolTgtInitGrbPtInGrblSpc(GnrGrb gnrGrb, Transform grblTrf) {
        Vector3 followTgtInitGrabPtWorld = MathUtils.TrfPtUnscaled(
            gnrGrb.gnrPhysHand.FollowTgtTrf,
            gnrGrb.theoInitGrbPtInFolTgtSpc
        );
        Vector3 followGrabLocal = MathUtils.InvrsTrfPtUnscaled(
            grblTrf,
            followTgtInitGrabPtWorld
        );
        return followGrabLocal;
    }

    /// <summary>
    /// Get the grab point of the phys hand target in world space.
    /// </summary>
    public static Vector3 GetTgtGripPtWorld(GnrGrb gnrGrb, Vector3 lclGrbPt) {
        return MathUtils.TrfPtUnscaled(gnrGrb.gnrPhysHand.FollowTgtTrf, lclGrbPt);
    }

    /// <summary>
    /// Is the specified phys hand grabbing?
    /// </summary>
    public static bool IsGrabbing<TGrb, TPhysHand>(List<TGrb> grbs,TPhysHand physHand)
        where TGrb : IGrb<TPhysHand>
        where TPhysHand : MonoBehaviour, IGnrPhysHand
    {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].PhysHand == physHand)
                return true;
        }
        return false;
    }

    public static void LRGrab_ReleaseAllGrbs(IGrblJntDriven_Grbl grbl, GameObject lHandVisProxy, GameObject rHandVisProxy) {
        foreach (GrblJntDriven_Grb grab in grbl.GrblCore.grbs)
            grab.physHand.OnGrabReleased(
                MathUtils.TrfPtUnscaled(grbl.GrblCore.transform, grab.gnrGrb.initPhysHandPosInGrblSpc),
                MathUtils.TrfRot(grbl.GrblCore.transform, grab.gnrGrb.initRotFromGrblToPhysHand)
            );
        grbl.GrblCore.grbs.Clear();
        lHandVisProxy.SetActive(false);
        rHandVisProxy.SetActive(false);
    }

    // TODO: Make generic.
    public static void LRGrb_ReleaseGrb(
        IGrblJntDriven_Grbl grbl,
        GrblJntDriven_PhysHand physHandToRelease,
        GameObject lHandVisProxy,
        GameObject rHandVisProxy
    ) {
        GrblJntDriven_Grb grb = FindGrb(
            grbl.GrblCore.grbs,
            physHandToRelease,
            grbl.GrblCore);
        GameObject correspondingProxyHand = physHandToRelease.handSide == Side.Left ? lHandVisProxy : rHandVisProxy;
        grb.physHand.OnGrabReleased(
            correspondingProxyHand.transform.position,
            correspondingProxyHand.transform.rotation
        );
        grbl.GrblCore.grbs.Remove(grb);
        correspondingProxyHand.SetActive(false);
    }

    public static void OnDrawGizmos_DrawGrbJntAnchor(IGrblJntDriven_Grbl grbl) {
        if (grbl == null || grbl.GrblCore == null || grbl.GrbJnt == null) {
            Debug.Log("Something was null in the grbl");
            return;
        }
        Gizmos.color = Color.yellow;
        Vector3 worldAnchorPos = MathUtils.TrfPtUnscaled(
            grbl.GrbJnt.transform,
            grbl.GrbJnt.anchor
        );
        Gizmos.DrawWireSphere(worldAnchorPos, 0.01f);
    }

    /// <summary>
    /// Counts how many phys hands of the specified side are grabbing.
    /// </summary>
    public static int SidedGrbCount<TGrb, TPhysHand>(
        List<TGrb> grbs,
        Side handSide
    )
    where TGrb : IGrb<TPhysHand>
    where TPhysHand : IGnrPhysHand {
        int counter = 0;
        foreach (TGrb grb in grbs) {
            if (grb.PhysHand.HandSide == handSide)
                counter++;
        }
        return counter;
    }

    /// <summary>
    /// Postition of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    /// <param name="theoGrblRot">NOTE: You can use <see cref="TheoFolTgtGrblRot"/> to get the rot.</param>
    public static Vector3 TheoFolTgtGrblPos(GrblJntDriven_Grb grb, Quaternion theoGrblRot) {
        return MathUtils.AlignLclPtToWldPt(
            grb.physHand.followTgtTrf.position,
            theoGrblRot,
            grb.gnrGrb.initPhysHandPosInGrblSpc
        );
    }

    /// <summary>
    /// Postition of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static Vector3 TheoFolTgtGrblPos<TGrb, TPhysHand>(TGrb grb)
        where TGrb : IGrb<TPhysHand>
        where TPhysHand : IGnrPhysHand {
        return MathUtils.AlignLclPtToWldPt(
            grb.PhysHand.FollowTgtTrf.position,
            TheoFolTgtGrblRot<TGrb, TPhysHand>(grb),
            grb.GnrGrb.initPhysHandPosInGrblSpc
        );
    }

    /// <summary>
    /// Pose of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">
    /// Grab with the corresponding phys hand's follow target holding the theoretical grabbable.
    /// </param>
    public static (Vector3, Quaternion) TheoFolTgtGrblPose<TGrb, TPhysHand>(TGrb grb)
        where TGrb : IGrb<TPhysHand>
        where TPhysHand : IGnrPhysHand
    {
        Quaternion theoRot = TheoFolTgtGrblRot<TGrb, TPhysHand>(grb);
        Vector3 theoPos = TheoFolTgtGrblPos<TGrb, TPhysHand>(grb);
        return (theoPos, theoRot);
    }

    /// <summary>
    /// Rotation of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static Quaternion TheoFolTgtGrblRot<TGrb, TPhysHand>(TGrb grb)
        where TGrb : IGrb<TPhysHand>
        where TPhysHand : IGnrPhysHand
    {
        return MathUtils.AlignLclRotToWldRot(
            grb.PhysHand.FollowTgtTrf.rotation,
            grb.GnrGrb.initRotFromGrblToPhysHand
        );
    }
}
