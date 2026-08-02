using UnityEngine;

/// <summary>
/// Utilities for game objects and Unity components.
/// </summary>
public static class ObjUtils{
    public static void ActivateNSetPose(GameObject go, Vector3 wldPos, Quaternion wldRot) {
        go.SetActive(true);
        go.transform.position = wldPos;
        go.transform.rotation = wldRot;
    }

    /// <summary>
    /// Draws small sphere where the joint anchor is.<br/>
    /// NOTE: Call this in OnDrawGizmos!
    /// </summary>
    public static void OnDrawGizmos_DrawJntAnch(ConfigurableJoint jnt) {
        if (jnt == null) {
            Debug.LogWarning("ConfigurableJoint was null.");
            return;
        }
        Gizmos.color = Color.yellow;
        Vector3 worldAnchorPos = MathUtils.TrfPtUnscaled(
            jnt.transform,
            jnt.anchor
        );
        Gizmos.DrawWireSphere(worldAnchorPos, 0.01f);
    }

    /// <summary>
    /// Draws small sphere where the joint anchor is.<br/>
    /// NOTE: Call this in OnDrawGizmos!
    /// </summary>
    public static void OnDrawGizmos_DrawJntConnectedAnch(ConfigurableJoint jnt) {
        if (jnt != null && jnt.connectedBody != null) {
            Gizmos.color = Color.darkOrange;
            Vector3 worldAnchorPos = MathUtils.TrfPtUnscaled(
                jnt.connectedBody.transform,
                jnt.connectedAnchor
            );
            Gizmos.DrawWireSphere(worldAnchorPos, 0.01f);
        } 
    }
}
