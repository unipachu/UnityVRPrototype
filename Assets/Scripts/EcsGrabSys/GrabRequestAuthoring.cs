using Unity.Entities;
using UnityEngine;

/// <summary>
/// Allows a PhysHand to request a grab from a Grabbable.
/// The component is disabled by default.
/// </summary>
public class GrabRequestAuthoring : MonoBehaviour {
    public class Baker : Baker<GrabRequestAuthoring> {
        public override void Bake(GrabRequestAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity,new GrabRequest {hand = Entity.Null});
            SetComponentEnabled<GrabRequest>(entity, false);
        }
    }
}

/// <summary>
/// A pending request for this Grabbable to be grabbed.
/// </summary>
public struct GrabRequest : IComponentData, IEnableableComponent {
    /// <summary>
    /// The PhysHand requesting the grab.
    /// </summary>
    public Entity hand;
}
