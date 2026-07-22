using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Synchronizes a GameObject transform with a CustomPhysJnt target.
/// </summary>
public class CustomPhysJntTgtProxy : MonoBehaviour {
    [Header("Joint")]
    [Tooltip("Unique ID of the joint to control.")]
    public int jointId;

    EntityManager entityManager;
    Entity jointEntity;
    bool initialized;

    float3 prevPos;
    quaternion prevRot;

    void Start() {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        prevPos = transform.position;
        prevRot = transform.rotation;
    }

    void FixedUpdate() {
        if (!initialized) {
            FindJoint();
            if (!initialized)
                return;
        }
        float dt = Time.fixedDeltaTime;
        float3 pos = transform.position;
        quaternion rot = transform.rotation;

        //-------------------------------------------------
        // Update joint target
        //-------------------------------------------------

        CustomPhysJntTgt tgt = entityManager.GetComponentData<CustomPhysJntTgt>(jointEntity);
        tgt.pos = pos;
        tgt.rot = rot;
        entityManager.SetComponentData(jointEntity, tgt);

        //-------------------------------------------------
        // Update target velocity
        //-------------------------------------------------

        CustomPhysJntTgtVel tgtVel = entityManager.GetComponentData<CustomPhysJntTgtVel>(jointEntity);
        tgtVel.linVel = (pos - prevPos) / dt;
        tgtVel.angVel = CustomPhysUtils.GetRotErr(prevRot, rot) / dt;
        entityManager.SetComponentData(jointEntity, tgtVel);
        prevPos = pos;
        prevRot = rot;
    }

    private void FindJoint() {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(CustomPhysJntId));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities) {
            CustomPhysJntId id = entityManager.GetComponentData<CustomPhysJntId>(entity);
            if (id.Value == jointId) {
                jointEntity = entity;
                initialized = true;
                break;
            }
        }
        // TODO: Do you need to dispose this here? Would the Allocator.Temp dispose of this anyway after this frame?
        entities.Dispose();
        if (!initialized)
            Debug.LogWarning($"CustomPhysJntTgtProxy: Could not find joint with ID {jointId}.", this);
    }
}
