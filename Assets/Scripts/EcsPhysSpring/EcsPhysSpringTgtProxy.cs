using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Holds data for ECS synchronization.
/// </summary>
public class EcsPhysSpringTgtProxy : MonoBehaviour {
    [Header("Target")]
    [SerializeField] Transform tgtTrf;

    [Header("Id")]
    public int tgtId;

    EntityManager entityManager;
    Entity tgtEntity;
    bool tgtFound;


    Vector3 prevPos = Vector3.zero;
    Quaternion prevRot = Quaternion.identity;
    Vector3 linVel = Vector3.zero;
    Vector3 angVel = Vector3.zero;

    private void Start() {
        prevPos = transform.position;
        prevRot = transform.rotation;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    public float3 TgtPos => tgtTrf.position;
    public quaternion TgtRot => tgtTrf.rotation;
    public float3 TgtLinVel => linVel;
    public float3 TgtAngVel => angVel;


    private void FixedUpdate() {
        linVel = MathUtils.LinVel(prevPos, transform.position, Time.fixedDeltaTime);
        angVel = MathUtils.AngVel(prevRot, transform.rotation, Time.fixedDeltaTime);
        prevPos = transform.position;
        prevRot = transform.rotation;

        if (!tgtFound) {
            FindSpringTgtComponent();
            if (!tgtFound)
                return;
        }

        entityManager.SetComponentData(
            tgtEntity,
            new EcsPhysSpringTgt {
                id = tgtId,
                pos = TgtPos,
                rot = TgtRot,
                linVel = TgtLinVel,
                angVel = TgtAngVel
            }
        );
    }

    private void FindSpringTgtComponent() {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(EcsPhysSpringTgt));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities) {
            EcsPhysSpringTgt tgt = entityManager.GetComponentData<EcsPhysSpringTgt>(entity);
            if (tgt.id == tgtId) {
                tgtEntity = entity;
                tgtFound = true;
                break;
            }
        }
        entities.Dispose();
    }
}

