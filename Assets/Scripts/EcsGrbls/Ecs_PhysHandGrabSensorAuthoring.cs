using System;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Marks an entity as a physics hand grab sensor.
/// The entity should have a trigger collider.
/// </summary>
[Obsolete("Use new grab system instead!")]
public class Ecs_PhysHandGrabSensorAuthoring : MonoBehaviour {
    [Tooltip("The PhysHand entity this sensor belongs to.")]
    public GameObject hand;

    public class Baker : Baker<Ecs_PhysHandGrabSensorAuthoring> {
        public override void Bake(Ecs_PhysHandGrabSensorAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            Entity handEntity = GetEntity(authoring.hand,TransformUsageFlags.Dynamic);
            AddComponent(
                entity,
                new PhysHandGrabSensor {
                    hand = handEntity
                }
            );
        }
    }
}

/// <summary>
/// Identifies a trigger entity as belonging to a PhysHand.
/// </summary>
public struct PhysHandGrabSensor : IComponentData {
    public Entity hand;
}