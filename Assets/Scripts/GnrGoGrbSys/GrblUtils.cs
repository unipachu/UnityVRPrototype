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
    public static float DistBetweenGrblRbPosNTheoFolTgtGrblPos(Rigidbody rb, IGnrGrbData grb) {
        return Vector3.Distance(rb.position, TheoFolTgtGrblPos(grb));
    }

    /// <summary>
    /// Finds the grab for the specified physics hand.
    /// </summary>
    public static IGnrGrbData FindGrb<TPhysHand, TGrbl>(
        IGnrGrbl<TPhysHand, TGrbl> grbl,
        IGnrPhysHand physHand,
        Object grblCtx
    ) where TPhysHand : IGnrPhysHand<TGrbl, TPhysHand>
        where TGrbl : IGnrGrbl<TPhysHand, TGrbl> 
    {
        for (int i = 0; i < grbl.GnrGrbs.GrbCount; i++) {
            if (grbl.GnrGrbs.GetGrb(i).GnrGrbData.gnrPhysHand == physHand)
                return grbl.GnrGrbs.GetGrb(i);
        }
        Debug.LogError($"{physHand.Trf.name} was not grabbing {grblCtx.name}!", grblCtx);
        return default;
    }

    /// <summary>
    /// Finds the index of the grab for the specified phys hand.
    /// </summary>
    /// <param name="grblCtx">For debug message context.</param>
    public static int FindGrbI<TGrb, TPhysHand>(List<TGrb> grbs, TPhysHand physHand, MonoBehaviour grblCtx)
        where TGrb : IGnrGrbData
        where TPhysHand : MonoBehaviour, IGnrPhysHand 
    {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].GnrGrbData.gnrPhysHand == physHand)
                return i;
        }
        Debug.LogError($"{physHand.name} was not grabbing {grblCtx.name}!", grblCtx);
        return -1;
    }

    /// <summary>
    /// Finds follow target grap point expressed in grabbable space.
    /// </summary>
    public static Vector3 FolTgtInitGrbPtInGrblSpc(GnrGrbData gnrGrb, Transform grblTrf) {
        Vector3 followTgtInitGrabPtWorld = MathUtils.TrfPtUnscaled(
            gnrGrb.gnrPhysHand.FolTgtTrf,
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
    public static Vector3 GetTgtGrbPtWld(GnrGrbData gnrGrb, Vector3 lclGrbPt) {
        return MathUtils.TrfPtUnscaled(gnrGrb.gnrPhysHand.FolTgtTrf, lclGrbPt);
    }

    /// <summary>
    /// This is called "LRGrb" since it expects only one left hand and one right hand visual proxy.
    /// </summary>
    public static void GrblJntDriven_LRGrb_ReleaseAllGrbs(
        IGrblJntDriven_Grbl grbl,
        GameObject lHandVisProxy,
        GameObject rHandVisProxy
    ) {
        for (int i = 0; i < grbl.Grbs.Count; i++) {
            GrblJntDriven_Grb grb = grbl.Grbs[i];
            grb.physHand.OnReleaseGrb(
                MathUtils.TrfPtUnscaled(grbl.Rb.transform, grb.GnrGrbData.initPhysHandPosInGrblSpc),
                MathUtils.TrfRot(grbl.Rb.transform, grb.GnrGrbData.initRotFromGrblToPhysHand)
            );
        }
        grbl.GnrGrbs.ClearGrbsList();
        lHandVisProxy.SetActive(false);
        rHandVisProxy.SetActive(false);
    }

    public static void GrblJntDriven_LRGrb_ReleaseGrbNHideProxyHand(
        IGrblJntDriven_Grbl grbl,
        GrblJntDriven_PhysHand physHandToRelease,
        GameObject lHandVisProxy,
        GameObject rHandVisProxy
    ) {
        IGnrGrbData grb = FindGrb(
            grbl,
            physHandToRelease,
            grbl.Rb
        );
        GameObject correspondingProxyHand =
            physHandToRelease.HandSide == Side.Left
                ? lHandVisProxy
                : rHandVisProxy;
        physHandToRelease.OnReleaseGrb(
            correspondingProxyHand.transform.position,
            correspondingProxyHand.transform.rotation
        );
        grbl.GnrGrbs.RemoveGrabFromList(grb);
        correspondingProxyHand.SetActive(false);
    }

    public static void HandJntDriven_ReleaseAllGrbs(IHandJntDriven_Grbl grbl) {
        for (int i = 0; i < grbl.Grbs.Count; i++) {
            HandJntDriven_Grb grb = grbl.Grbs[i];
            grb.physHand.OnReleaseGrb();
        }
        grbl.GnrGrbs.ClearGrbsList();
    }

    public static void HandJntDriven_LRGrb_ReleaseGrb(
        IHandJntDriven_Grbl grbl,
        HandJntDriven_PhysHand physHandToRelease
    ) {
        IGnrGrbData grb = FindGrb(
            grbl,
            physHandToRelease,
            grbl.Rb
        );
        physHandToRelease.OnReleaseGrb();
        grbl.GnrGrbs.RemoveGrabFromList(grb);
    }

    /// <summary>
    /// Is the specified phys hand grabbing?
    /// </summary>
    public static bool IsGrabbing<TGrb, TPhysHand>(List<TGrb> grbs, TPhysHand physHand)
        where TGrb : IGnrGrbData
        where TPhysHand : MonoBehaviour, IGnrPhysHand
    {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].GnrGrbData.gnrPhysHand == physHand)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Counts how many phys hands of the specified side are grabbing.
    /// </summary>
    public static int SidedGrbCount<TGrb, TPhysHand>(
        List<TGrb> grbs,
        Side handSide
    ) where TGrb : IGnrGrbData {
        int counter = 0;
        foreach (TGrb grb in grbs) {
            if (grb.GnrGrbData.gnrPhysHand.HandSide == handSide)
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
    public static Vector3 TheoFolTgtGrblPos(IGnrGrbData grb, Quaternion theoGrblRot) {
        return MathUtils.AlignLclPtToWldPt(
            grb.GnrGrbData.gnrPhysHand.FolTgtTrf.position,
            theoGrblRot,
            grb.GnrGrbData.initPhysHandPosInGrblSpc
        );
    }

    /// <summary>
    /// Postition of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static Vector3 TheoFolTgtGrblPos<TGrb>(TGrb grb)
        where TGrb : IGnrGrbData
    { 
        return MathUtils.AlignLclPtToWldPt(
            grb.GnrGrbData.gnrPhysHand.FolTgtTrf.position,
            TheoFolTgtGrblRot<TGrb>(grb),
            grb.GnrGrbData.initPhysHandPosInGrblSpc
        );
    }

    /// <summary>
    /// Pose of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">
    /// Grab with the corresponding phys hand's follow target holding the theoretical grabbable.
    /// </param>
    public static (Vector3, Quaternion) TheoFolTgtGrblPose<TGrb>(TGrb grb)
        where TGrb : IGnrGrbData
    {
        Quaternion theoRot = TheoFolTgtGrblRot<TGrb>(grb);
        Vector3 theoPos = TheoFolTgtGrblPos<TGrb>(grb);
        return (theoPos, theoRot);
    }

    /// <summary>
    /// Rotation of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static Quaternion TheoFolTgtGrblRot<TGrb>(TGrb grb)
        where TGrb : IGnrGrbData
    {
        return MathUtils.AlignLclRotToWldRot(
            grb.GnrGrbData.gnrPhysHand.FolTgtTrf.rotation,
            grb.GnrGrbData.initRotFromGrblToPhysHand
        );
    }
}
