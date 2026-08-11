using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Represents a target for springs.
/// </summary>
public class EcsPhysSpringTgtAuthoring : MonoBehaviour {
    public int id;

    public class Baker : Baker<EcsPhysSpringTgtAuthoring> {
        public override void Bake(EcsPhysSpringTgtAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(
                entity,
                new EcsPhysSpringTgt { id = authoring.id }
            );
        }
    }
}

public struct EcsPhysSpringTgt : IComponentData {
    public int id;
    public float3 pos;
    public quaternion rot;
    public float3 linVel;
    public float3 angVel;
}