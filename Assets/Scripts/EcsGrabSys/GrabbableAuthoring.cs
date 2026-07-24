using Unity.Entities;
using UnityEngine;

/// <summary>
/// Marks an entity as grabbable.
/// </summary>
public class GrabbableAuthoring : MonoBehaviour {
    public class Baker : Baker<GrabbableAuthoring> {
        public override void Bake(GrabbableAuthoring authoring) {
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