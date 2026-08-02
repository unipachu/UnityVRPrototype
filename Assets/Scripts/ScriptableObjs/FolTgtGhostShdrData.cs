using UnityEngine;

[CreateAssetMenu(
    fileName = "FolTgtGhostShdrData",
    menuName = "VrPhysicsData/FolTgtGhostShdrData")]
public class FolTgtGhostShdrData : ScriptableObject
{
    [Header("Follow Target Hand Ghost Shader Settings")]
    [Tooltip("Distance from physics hand under which the follow target hand is completely invisible.")]
    public float invisibleDist = 0.001f;
    [Tooltip("Distance from physics hand where follow target hand reaches max transparency.")]
    public float maxTransparencyDist = 0.1f;
    public float maxTransparency = 0.9f;
}
