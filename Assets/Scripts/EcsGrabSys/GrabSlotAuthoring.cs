using Unity.Entities;
using UnityEngine;

/// <summary>
/// Defines one grab slot on a grabbable object.
/// Each slot controls one CustomPhysJnt.
/// </summary>
public class GrabSlotAuthoring : MonoBehaviour {
    [Tooltip("The GameObject containing the EcsPhysSpringAuthoring.")]
    public GameObject spring;

    public class Baker : Baker<GrabSlotAuthoring> {
        public override void Bake(GrabSlotAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.None);
            Entity jointEntity = GetEntity(authoring.spring, TransformUsageFlags.None);
            Entity grabbableEntity = GetEntity(authoring.transform.root.gameObject,TransformUsageFlags.Dynamic);
            AddComponent(entity, new GrabSlot {grabbable = grabbableEntity, joint = jointEntity, occupied = false});
        }
    }
}

/// <summary>
/// One available grab slot on a grabbable.
/// </summary>
public struct GrabSlot : IComponentData {
    /// <summary>
    /// The object this slot belongs to.
    /// </summary>
    public Entity grabbable;
    /// <summary>
    /// The EcsPhysSpring entity controlled by this slot.
    /// </summary>
    public Entity joint;
    /// <summary>
    /// True while this slot is occupied.
    /// </summary>
    public bool occupied;
}
