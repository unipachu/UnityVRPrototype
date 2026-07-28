using UnityEngine;

public static class GrblUtils {
    public static void EnableProxyHand(GameObject handVisProxy, Vector3 handPos, Quaternion handRot) {
        handVisProxy.SetActive(true);
        handVisProxy.transform.position = handPos;
        handVisProxy.transform.rotation = handRot;
    }

    public static void LRGrab_ReleaseAllGrbs(IGrblJntDriven_Grbl grbl, GameObject lHandVisProxy, GameObject rHandVisProxy) {
        foreach (Grb grab in grbl.GrblCore.grbs)
            grab.physHand.OnGrabReleased(
                MathUtils.UnscaledTrfPt(grbl.GrblCore.transform, grab.initPhysHandPosInGrblLocalSpace),
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
        grb.physHand.OnGrabReleased(
            MathUtils.UnscaledTrfPt(grbl.GrblCore.transform, grb.initPhysHandPosInGrblLocalSpace),
            MathUtils.RotFromTrfSpaceToWorld(grbl.GrblCore.transform, grb.initRotFromGrblToPhysHand)
        );
        grbl.GrblCore.grbs.Remove(grb);
        if (physHandToRelease.side == Side.Left)
            lHandVisProxy.SetActive(false);
        else
            rHandVisProxy.SetActive(false);
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
