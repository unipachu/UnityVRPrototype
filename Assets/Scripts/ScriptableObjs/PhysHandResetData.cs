using UnityEngine;

/// <summary>
/// Settings for physics hand reset.
/// </summary>
[CreateAssetMenu(
    fileName = "PhysHandResetData",
    menuName = "VrPhysicsData/PhysHandResetData")]
public class PhysHandResetData : ScriptableObject
{
    [Header("Hand Pose Reset Settings")]
    [Tooltip("Reset hand pose if it gets too far from the controller.")]
    public bool useHandPoseReset = true;
    [Tooltip("Distance threshold from the controller which causes the phys hand to teleport to the controller pose.")]
    [Min(0f)]
    public float handPoseResetDist = 0.4f;
    [Tooltip("Layers used to check if the area around the hand controller is free before teleporting. Select all layers the physics hands can collide with.")]
    public LayerMask obstructionChkLayersMask;

}
