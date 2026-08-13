using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Passes game object spring target data to the ECS subscene.
/// </summary>
public class EcsPhysSpringTgtProxy : MonoBehaviour {
    [Header("Target")]
    [Tooltip("Transform you want to use as a target for the ECS spring "
        + "(e.g. XR Origin's hand transform for physics hand).")]
    [SerializeField] Transform tgtTrf;
    [Tooltip("Target position offset, e.g. when physics hand center of mass does not align with the XR Origin hand.")]
    [SerializeField] Vector3 tgtLclPosOffset;

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
        prevPos = MathUtils.TrfPtUnscaled(tgtTrf, tgtLclPosOffset);
        prevRot = tgtTrf.rotation;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    public float3 TgtPos => MathUtils.TrfPtUnscaled(tgtTrf, tgtLclPosOffset);
    public quaternion TgtRot => tgtTrf.rotation;
    public float3 TgtLinVel => linVel;
    public float3 TgtAngVel => angVel;

    private void FixedUpdate() {
        Vector3 offsetPos = MathUtils.TrfPtUnscaled(tgtTrf, tgtLclPosOffset);
        linVel = MathUtils.LinVel(prevPos, offsetPos, Time.fixedDeltaTime);
        angVel = MathUtils.AngVel(prevRot, tgtTrf.rotation, Time.fixedDeltaTime);
        prevPos = offsetPos;
        prevRot = tgtTrf.rotation;
        if (!tgtFound) {
            FindSpringTgtComponent();
            if (!tgtFound)
                return;
        }
        //Debug.Log("id " + tgtId + "Tgt pos: " + TgtPos, this);
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

