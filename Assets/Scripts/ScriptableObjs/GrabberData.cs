using UnityEngine;

[CreateAssetMenu(
    fileName = "NewGrabberData",
    menuName = "VrPhysics/GrabberData")]
public class GrabberData : ScriptableObject {
    [Header("Grab Settings")]
    [Tooltip("Radius of the grab overlap sphere.")]
    [Min(0f)]
    public float chkSphereR = 0.1f;
    [Tooltip("Layers used by the overlap sphere when searching for grabbable objects.")]
    public LayerMask grabLayers;
}
