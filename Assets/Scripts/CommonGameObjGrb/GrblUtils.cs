using UnityEngine;

/// <summary>
/// Utilities for game object grabbables.
/// </summary>
public static class GrblUtils {
    /// <summary>
    /// Finds follow target grap point expressed in axe local space.
    /// </summary>
    public static Vector3 FollowTgtInitGrbPtInGrblSpc(Grb grb, Transform grblTrf) {
        Vector3 followTgtInitGrabPtWorld = MathUtils.TrfPtUnscaled(
            grb.physHand.followTgtTrf,
            grb.followTgtInitGrabPtInFollowTgtSpc
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
    public static Vector3 GetTgtGripPtWorld(Grb grb, Vector3 lclGrbPt) {
        return MathUtils.TrfPtUnscaled(grb.physHand.followTgtTrf, lclGrbPt);
    }

    public static void LRGrab_ReleaseAllGrbs(IGrblJntDriven_Grbl grbl, GameObject lHandVisProxy, GameObject rHandVisProxy) {
        foreach (Grb grab in grbl.GrblCore.grbs)
            grab.physHand.OnGrabReleased(
                MathUtils.TrfPtUnscaled(grbl.GrblCore.transform, grab.initPhysHandPosInGrblSpc),
                MathUtils.TrfRot(grbl.GrblCore.transform, grab.initRotFromGrblToPhysHand)
            );
        grbl.GrblCore.grbs.Clear();
        lHandVisProxy.SetActive(false);
        rHandVisProxy.SetActive(false);
    }

    public static void LRGrb_ReleaseGrb(
        IGrblJntDriven_Grbl grbl,
        GrblJntDriven_PhysHand physHandToRelease,
        GameObject lHandVisProxy,
        GameObject rHandVisProxy
    ) {
        Grb grb = grbl.GrblCore.FindGrb(physHandToRelease);
        GameObject correspondingProxyHand = physHandToRelease.side == Side.Left ? lHandVisProxy : rHandVisProxy;
        grb.physHand.OnGrabReleased(
            correspondingProxyHand.transform.position,
            correspondingProxyHand.transform.rotation
        );
        grbl.GrblCore.grbs.Remove(grb);
        correspondingProxyHand.SetActive(false);
    }

    public static void OnDrawGizmos_DrawGrbJntAnchor(IGrblJntDriven_Grbl grbl) {
        if (grbl == null || grbl.GrblCore == null || grbl.GrblCore.grbJnt == null) {
            Debug.Log("Something was null in the grbl");
            return;
        }
        Gizmos.color = Color.yellow;
        Vector3 worldAnchorPos = MathUtils.TrfPtUnscaled(
            grbl.GrblCore.grbJnt.transform,
            grbl.GrblCore.grbJnt.anchor
        );
        Gizmos.DrawWireSphere(worldAnchorPos, 0.01f);
    }

    /// <summary>
    /// Distance between the grabbable's current rigidbody position and the theoretical grabbable position
    /// if the follow target (i.e. hand controller) would be grabbing it like the phys hand's initial grab
    /// of the grabbable.
    /// /// </summary>
    public static float DistBetweenGrblRbPosNTheoFolTgtGrblPos(Rigidbody rb, Grb grb) {
        return Vector3.Distance(rb.position, TheoFolTgtGrblPos(grb));
    }

    /// <summary>
    /// Postition of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    /// <param name="theoGrblRot">NOTE: You can use <see cref="TheoFolTgtGrblRot"/> to get the rot.</param>
    public static Vector3 TheoFolTgtGrblPos(Grb grb, Quaternion theoGrblRot) {
        return MathUtils.AlignLclPtToWldPt(
            grb.physHand.followTgtTrf.position,
            theoGrblRot,
            grb.initPhysHandPosInGrblSpc
        );
    }

    /// <summary>
    /// Postition of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static Vector3 TheoFolTgtGrblPos(Grb grb) {
        return MathUtils.AlignLclPtToWldPt(
            grb.physHand.followTgtTrf.position,
            TheoFolTgtGrblRot(grb),
            grb.initPhysHandPosInGrblSpc
        );
    }

    /// <summary>
    /// Pose of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static (Vector3, Quaternion) TheoFolTgtGrblPose(Grb grb) {
        Quaternion theoRot = TheoFolTgtGrblRot(grb);
        Vector3 theoPos = TheoFolTgtGrblPos(grb, theoRot);
        return (theoPos, theoRot);
    }

    /// <summary>
    /// Rotation of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static Quaternion TheoFolTgtGrblRot(Grb grb) {
        return MathUtils.AlignLclRotToWldRot(
            grb.physHand.followTgtTrf.rotation,
            grb.initRotFromGrblToPhysHand
        );
    }
}
