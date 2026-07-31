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
}
