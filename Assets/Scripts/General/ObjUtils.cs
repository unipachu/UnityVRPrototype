using UnityEngine;

/// <summary>
/// Utilities for game objects and Unity components.
/// </summary>
public static class ObjUtils{
    public static void ActivateNSetPose(GameObject obj, Vector3 wldPos, Quaternion wldRot) {
        obj.SetActive(true);
        obj.transform.position = wldPos;
        obj.transform.rotation = wldRot;
    }
}
