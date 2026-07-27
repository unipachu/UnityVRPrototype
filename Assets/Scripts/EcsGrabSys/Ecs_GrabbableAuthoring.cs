using Unity.Entities;
using UnityEngine;

/// <summary>
/// Marks an entity as grabbable.
/// </summary>
public class Ecs_GrabbableAuthoring : MonoBehaviour {
    public class Baker : Baker<Ecs_GrabbableAuthoring> {
        public override void Bake(Ecs_GrabbableAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity,new Grabbable());
        }
    }
}

/// <summary>
/// Marks an entity as grabbable.
/// Gameplay systems may attach one or more grab slots to it.
/// </summary>
public struct Grabbable : IComponentData {
}