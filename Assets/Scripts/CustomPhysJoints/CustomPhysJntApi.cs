using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Helper functions for controlling CustomPhysJnt targets.
/// NOTE: These are for MAIN THREAD ONLY, not for burst jobs!
/// </summary>
public static class CustomPhysJntApi {
    /// <summary>
    /// Sets the world position target of a joint.
    /// </summary>
    public static void SetTargetPos(
        EntityManager entityManager,
        Entity jntEntity,
        float3 pos
    ) {
        CustomPhysJntTgt tgt =
            entityManager.GetComponentData<CustomPhysJntTgt>(
                jntEntity
            );
        tgt.pos = pos;
        entityManager.SetComponentData(
            jntEntity,
            tgt
        );
    }

    /// <summary>
    /// Sets the world rotation target of a joint.
    /// </summary>
    public static void SetTargetRot(
        EntityManager entityManager,
        Entity jntEntity,
        quaternion rot
    ) {
        CustomPhysJntTgt tgt =
            entityManager.GetComponentData<CustomPhysJntTgt>(
                jntEntity
            );
        tgt.rot = rot;
        entityManager.SetComponentData(
            jntEntity,
            tgt
        );
    }

    /// <summary>
    /// Sets both position and rotation targets.
    /// Preserves other target data.
    /// </summary>
    public static void SetTarget(
        EntityManager entityManager,
        Entity jntEntity,
        float3 pos,
        quaternion rot
    ) {
        CustomPhysJntTgt tgt =
            entityManager.GetComponentData<CustomPhysJntTgt>(
                jntEntity
            );
        tgt.pos = pos;
        tgt.rot = rot;
        entityManager.SetComponentData(
            jntEntity,
            tgt
        );
    }

    /// <summary>
    /// Enables or disables the entire joint.
    /// </summary>
    public static void SetEnabled(
        EntityManager entityManager,
        Entity jntEntity,
        bool enabled
    ) {
        CustomPhysJnt jnt =
            entityManager.GetComponentData<CustomPhysJnt>(
                jntEntity
            );
        jnt.enabled = enabled;
        entityManager.SetComponentData(
            jntEntity,
            jnt
        );
    }
}
