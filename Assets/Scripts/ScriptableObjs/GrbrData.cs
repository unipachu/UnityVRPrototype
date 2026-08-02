using UnityEngine;

/// <summary>
/// Data for a grabber (e.g. a physics hand) which can grab grabbables.
/// </summary>
[CreateAssetMenu(
    fileName = "GrbrData",
    menuName = "VrPhysicsData/GrbrData")]
public class GrbrData : ScriptableObject {
    [Header("Grab Settings")]
    [Tooltip("Radius of the grab overlap sphere.")]
    [Min(0f)]
    public float overlapSphereR = 0.045f;
    [Tooltip("Layers used by the overlap sphere when searching for grabbable objects.")]
    public LayerMask grbLayers;
}
