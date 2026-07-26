using UnityEngine;

/// <summary>
/// Data used for moving phys hand with configurable joint.
/// </summary>
[CreateAssetMenu(
    fileName = "NewPhysHandData",
    menuName = "VrPhysics/PhysHandData")]
public class PhysHandConfigurableJntData : ScriptableObject {
    [Header("Hand Pose Reset Settings")]
    [Tooltip("Reset hand pose if it gets too far from the controller.")]
    public bool useHandPoseReset = true;
    [Tooltip("Distance threshold from the controller which causes the phys hand to teleport to the controller pose.")]
    [Min(0f)]
    public float handPoseResetDist = 0.4f;
    [Tooltip("Layers used to check if the area around the hand controller is free before teleporting. Select all layers the physics hands can collide with.")]
    public LayerMask obstructionChkLayersMask;
    
    [Header("World Joint Settings")]
    [Tooltip("Default linear drive position spring.")]
    [Min(0f)]
    public float dfltLinDrivePosSpring = 5000;
    [Tooltip("Default linear drive position damper.")]
    [Min(0f)]
    public float dfltLinDrivePosDamper = 50;
    [Tooltip("Default linear drive max force.")]
    [Min(0f)]
    public float dfltLinDriveMaxForce = 40;
    [Tooltip("Default slerp drive position spring.")]
    [Min(0f)]
    public float dfltSlerpDrivePosSpring = 5000;
    [Tooltip("Default slerp drive position damper.")]
    [Min(0f)]
    public float dfltSlerpDriveDamper = 50;
    [Tooltip("Default slerp drive max force.")]
    [Min(0f)]
    public float defaultSlerpDriveMaxForce = 40;
}
