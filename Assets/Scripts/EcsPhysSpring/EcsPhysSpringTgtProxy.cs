using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Synchronizes a GameObject transform with a EcsPhysSpring target.
/// </summary>
public class EcsPhysSpringTgtProxy : MonoBehaviour {
    [Header("Spring")]
    [Tooltip("Unique ID of the spring to control.")]
    [SerializeField] int springId;

    [Header("Refs")]
    [SerializeField] Transform tgtTrf;

    EntityManager entityManager;
    Entity springEntity;
    /// <summary>
    /// Has this found a spring target component?
    /// </summary>
    bool springFound;

    float3 prevTgtPos;
    quaternion prevTgtRot;

    void Start() {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        prevTgtPos = tgtTrf.position;
        prevTgtRot = tgtTrf.rotation;
    }

    void FixedUpdate() {
        if (!springFound) {
            // TODO: Manually search for the correct spring every FixedUpdate. This is a very primitive system, and there likely is
            // TODO C: a better way to manage springs. Perhaps spring targets should be set entirely inside the subscene with some sort of
            // TODO C: spring/spring target register at the start of the game, and this sort
            // TODO C: of class should be only sync a transform entity (used as a spring target) to the pos/rot of a normal game object,
            // TODO C: simplifying the communication between normal game objects and the entities.
            FindSpring();
            if (!springFound) {
                // TODO: This warning seems to always trigger at the beginning of play. I don't know why.
                Debug.LogWarning($"Could not find spring with ID {springId} in FixedUpdate. Trying again next FixedUpdate.", this);
                return;
            }
        }

        float dt = Time.fixedDeltaTime;
        float3 tgtPos = tgtTrf.position;
        quaternion tgtRot = tgtTrf.rotation;

        // TODO: Spring impulses are solved in PhysicsSystemGroup before PhysicsSimulationGroup - so does this FixedUpdate
        // TODO C: run before or after spring impulses are solved? Should I cache the target pos/rot/vel?
        
        // Update spring target
        EcsPhysSpringTgt tgt = entityManager.GetComponentData<EcsPhysSpringTgt>(springEntity);
        tgt.pos = tgtPos;
        tgt.rot = tgtRot;
        entityManager.SetComponentData(springEntity, tgt);

        // Update target velocity
        EcsPhysSpringTgtVel tgtVel = entityManager.GetComponentData<EcsPhysSpringTgtVel>(springEntity);
        tgtVel.linVel = (tgtPos - prevTgtPos) / dt;
        tgtVel.angVel = EcsMathNPhysUtils.GetRotErr(prevTgtRot, tgtRot) / dt;
        entityManager.SetComponentData(springEntity, tgtVel);
        prevTgtPos = tgtPos;
        prevTgtRot = tgtRot;
    }

    /// <summary>
    /// Finds and caches a reference to a spring entity with matching id.
    /// </summary>
    private void FindSpring() {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(EcsPhysSpringId));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities) {
            EcsPhysSpringId id = entityManager.GetComponentData<EcsPhysSpringId>(entity);
            if (id.value == springId) {
                springEntity = entity;
                springFound = true;
                break;
            }
        }
        // Because of Allocator.Temp, entities would be disposed automatically
        // at the end of the frame, but we can already clean it up here.
        entities.Dispose();
    }
}
