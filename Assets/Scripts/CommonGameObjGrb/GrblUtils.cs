using UnityEngine;

public static class GrblUtils {
    // TODO: Move this to general utils.
    public static void EnableObjNSetPose(GameObject obj, Vector3 wldPos, Quaternion wldRot) {
        obj.SetActive(true);
        obj.transform.position = wldPos;
        obj.transform.rotation = wldRot;
    }

    /// <summary>
    /// Finds follow target grap point expressed in axe local space.
    /// </summary>
    public static Vector3 FollowTgtInitGrbPtInGrblSpc(Grb grb, Transform grblTrf) {
        Vector3 followTgtInitGrabPtWorld = MathUtils.UnscaledTrfPt(
            grb.physHand.followTgtTrf,
            grb.followTgtInitGrabPtInFollowTgtSpc
        );
        Vector3 followGrabLocal = MathUtils.UnscaledInvrsTrfPt(
            grblTrf,
            followTgtInitGrabPtWorld
        );
        return followGrabLocal;
    }

    /// <summary>
    /// Get the grip point of the phys hand target in world space.
    /// </summary>
    public static Vector3 GetTgtGripPtWorld(Grb grb, Vector3 localGripPt) {
        return grb.physHand.followTgtTrf.TransformPoint(localGripPt);
    }

    public static void LRGrab_ReleaseAllGrbs(IGrblJntDriven_Grbl grbl, GameObject lHandVisProxy, GameObject rHandVisProxy) {
        foreach (Grb grab in grbl.GrblCore.grbs)
            grab.physHand.OnGrabReleased(
                MathUtils.UnscaledTrfPt(grbl.GrblCore.transform, grab.initPhysHandPosInGrblSpc),
                MathUtils.RotFromTrfSpaceToWorld(grbl.GrblCore.transform, grab.initRotFromGrblToPhysHand)
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
        Vector3 worldAnchorPos =
            grbl.GrblCore.grbJnt.transform.TransformPoint(grbl.GrblCore.grbJnt.anchor);
        Gizmos.DrawWireSphere(worldAnchorPos, 0.02f);
    }
}
