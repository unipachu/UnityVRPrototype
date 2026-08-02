using UnityEngine;

/// <summary>
/// Default settings used for configurable joint.
/// </summary>
[CreateAssetMenu(
    fileName = "DfltConfigJntData",
    menuName = "VrPhysicsData/DfltConfigJntData")]
public class DfltConfigJntData : ScriptableObject {
    [Header("Configurable Joint Settings")]
    [Tooltip("Default linear drive position spring.")]
    [Min(0f)]
    public float dfltLinDrvPosSpring = 5000;
    [Tooltip("Default linear drive position damper.")]
    [Min(0f)]
    public float dfltLinDrvPosDamper = 50;
    [Tooltip("Default linear drive max force.")]
    [Min(0f)]
    public float dfltLinDrvMaxForce = 50;
    [Tooltip("Default slerp drive position spring.")]
    [Min(0f)]
    public float dfltSlerpDrvPosSpring = 3000;
    [Tooltip("Default slerp drive position damper.")]
    [Min(0f)]
    public float dfltSlerpDrvDamper = 50;
    [Tooltip("Default slerp drive max force.")]
    [Min(0f)]
    public float dfltSlerpDrvMaxForce = 50;
}
