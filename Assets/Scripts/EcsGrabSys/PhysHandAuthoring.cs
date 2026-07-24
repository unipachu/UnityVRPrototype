using Unity.Entities;
using UnityEngine;

/// <summary>
/// Marks an entity as a physics hand.
/// </summary>
public class PhysHandAuthoring : MonoBehaviour {
    public class Baker : Baker<PhysHandAuthoring> {
        public override void Bake(PhysHandAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(
                entity,
                new PhysHandGrab {isGrabbing = false, grabbable = Entity.Null, grabSlot = Entity.Null}
            );
            //AddBuffer<PhysHandGrabCandidateElement>(entity);
            AddBuffer<PhysHandGrabCandidate>(entity);
            //AddComponent(
            //    entity,
            //    new PhysHandGrabSensor {
            //        ClosestCandidate = Entity.Null
            //    }
            //);
            //AddComponent(
            //    entity,
            //    new PhysHandGrabCandidate {
            //        Entity = Entity.Null
            //    }
            //);
        }
    }
}

/// <summary>
/// Runtime grab state of a physics hand.
/// </summary>
public struct PhysHandGrab : IComponentData {
    /// <summary>
    /// True while the hand is grabbing something.
    /// </summary>
    public bool isGrabbing;
    /// <summary>
    /// Currently grabbed grabbable.
    /// Entity.Null if nothing is grabbed.
    /// </summary>
    public Entity grabbable;
    /// <summary>
    /// Grab slot currently in use.
    /// Entity.Null if nothing is grabbed.
    /// </summary>
    public Entity grabSlot;
}

///// <summary>
///// Stores the currently detected grabbable candidate.
///// </summary>
//public struct PhysHandGrabCandidate : IComponentData {
//    /// <summary>
//    /// Closest grabbable currently detected.
//    /// </summary>
//    public Entity Entity;
//}

///// <summary>
///// Stores entities currently inside the hand grab trigger.
///// </summary>
//public struct PhysHandGrabSensor : IComponentData {
//    public Entity ClosestCandidate;
//}

///// <summary>
///// Dynamic buffer containing nearby grabbable entities.
///// </summary>
//public struct PhysHandGrabCandidateElement : IBufferElementData {
//    public Entity Entity;
//}

/// <summary>
/// Stores grabbable entities currently inside the hand sensor.
/// </summary>
public struct PhysHandGrabCandidate : IBufferElementData {
    public Entity Entity;
}
