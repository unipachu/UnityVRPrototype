using Unity.Entities;
using UnityEngine;

/// <summary>
/// Data about player inputs.
/// </summary>
public class EcsPlrCtrlAuthoring : MonoBehaviour {
    [Tooltip("Unique player id. Used to pass data from game object scene to ECS subscene.")]
    public int plrId;

    public class Baker : Baker<EcsPlrCtrlAuthoring> {
        public override void Bake(EcsPlrCtrlAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(
                entity,
                new EcsPlrInput { plrId = authoring.plrId }
            );
        }
    }
}

public struct EcsPlrInput : IComponentData {
    public int plrId;
    public float lGrbValue;
    public bool lGrbPressed;
    public float rGrbValue;
    public bool rGrbPressed;
    public float lTrgValue;
    public bool lTrgPressed;
    public float rTrgValue;
    public bool rTrgPressed;
}