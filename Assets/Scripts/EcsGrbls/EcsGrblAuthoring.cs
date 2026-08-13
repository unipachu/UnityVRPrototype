using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EcsGrblAuthoring : MonoBehaviour {
    [Tooltip("The way the physics hand calculates distance to grabbable when trying to grab it.")]
    public EcsGrblDistMode distMode;
    [Tooltip("Only used when distMode == ToLocalPoint.")]
    public float3 distLclPt;
    [Tooltip("Restriction mode on when this grabbable can be grabbed.")]
    public EcsGrblCanBeGrabbedMode canBeGrabbedMode;

    public class Baker : Baker<EcsGrblAuthoring> {
        public override void Bake(EcsGrblAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(
                entity,
                new EcsGrbl {
                    distMode = authoring.distMode,
                    distLclPt = authoring.distLclPt,
                    canBeGrabbed = authoring.canBeGrabbedMode,
                    canBeReleased = true,
                }
            );
            AddBuffer<EcsGrb>(entity);
        }
    }
}

public struct EcsGrbl : IComponentData {
    public EcsGrblDistMode distMode;
    public float3 distLclPt;
    // TODO: Later this could be made into a set of bools e.g. canBeGrabbedByLeft/canBeGrabbedByRight
    // TODO C: and then have a system update those values.
    public EcsGrblCanBeGrabbedMode canBeGrabbed;
    // TODO C: A separate grabbable system could later update if releasing the grabbable is allowed.
    public bool canBeReleased;
}

/// <summary>
/// NOTE: Increase size if the game would ever allow for more than 2 phys hands (e.g. because of multiplayer).
/// NOTE C: Going over the capacity will reallocate a new heap block big enough for the new elements
/// NOTE C: and buffer header will now instead have a pointer to that external block, breaking component's
/// NOTE C: contiguousness. Be aware.
/// </summary>
[InternalBufferCapacity(2)]
public struct EcsGrb : IBufferElementData {
    /// <summary>
    /// Physics hand grabbing the grabbable.
    /// </summary>
    public Entity physHandEntity;
    /// <summary>
    /// Unscaled position of the hand in the grabbable's local space when the grab was initialized.
    /// </summary>
    public float3 initPhysHandPosInGrblLclSpc;
    /// <summary>
    /// Rotation from the grabbed object to the hand when the grab was initialized.
    /// </summary>
    public quaternion initRotFromGrblToPhysHand;
    /// <summary>
    /// Theoretical grab point in the physics hand's spring target space, as if the
    /// follow target had grabbed the grabbable the same way as the physics hand.
    /// </summary>
    public float3 theoInitGrbPtInSpringTgtSpc;
}