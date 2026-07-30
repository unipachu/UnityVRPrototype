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
    /// Get the grip point of the phys hand target in world space.
    /// </summary>
    public static Vector3 GetTgtGripPtWorld(Grb grb, Vector3 localGripPt) {
        //return grb.physHand.followTgtTrf.TransformPoint(localGripPt);
        return MathUtils.TrfPtUnscaled(grb.physHand.followTgtTrf, localGripPt);
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
        //Vector3 worldAnchorPos =
        //    grbl.GrblCore.grbJnt.transform.TransformPoint(grbl.GrblCore.grbJnt.anchor);
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
    public static float DistBetweenGrblRbPosNTheoreticalFollowTgtGrblPos(Rigidbody rb, Grb grb) {
        return Vector3.Distance(rb.position, TheoreticalFollowTgtGrblPos(grb, TheoreticalFollowTgtGrblRot(grb)));
    }

    /// <summary>
    /// Postition of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    /// <param name="theoreticalGrblRot">NOTE: You can use <see cref="TheoreticalFollowTgtGrblRot"/> to get the rot.</param>
    public static Vector3 TheoreticalFollowTgtGrblPos(Grb grb, Quaternion theoreticalGrblRot) {
        //return grb.physHand.followTgtTrf.position - theoreticalGrblRot * grb.initPhysHandPosInGrblSpc;
        return MathUtils.AlignLclPtToWldPt(
            grb.physHand.followTgtTrf.position,
            theoreticalGrblRot,
            grb.initPhysHandPosInGrblSpc
        );
    }

    /// <summary>
    /// Pose of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static (Vector3, Quaternion) TheoreticalFollowTgtGrblPose(Grb grb) {
        Quaternion theoreticalRot = TheoreticalFollowTgtGrblRot(grb);
        Vector3 theoreticalPos = TheoreticalFollowTgtGrblPos(grb, theoreticalRot);
        return (theoreticalPos, theoreticalRot);
    }

    /// <summary>
    /// Rotation of the theoretical grabbable in world space if the follow target (i.e. hand controller)
    /// would be grabbing it like the phys hand's initial grab of the grabbable.
    /// </summary>
    /// <param name="grb">Grab with the corresponding phys hand's follow target holding the theoretical grabbable.</param>
    public static Quaternion TheoreticalFollowTgtGrblRot(Grb grb) {
        //return grb.physHand.followTgtTrf.rotation * Quaternion.Inverse(grb.initRotFromGrblToPhysHand);
        return MathUtils.AlignLclRotToWldRot(
            grb.physHand.followTgtTrf.rotation,
            grb.initRotFromGrblToPhysHand
        );
    }
}
