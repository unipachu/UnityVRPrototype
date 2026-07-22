using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
public partial struct CustomPhysJntSys : ISystem {
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<CustomPhysJnt>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        float dt = SystemAPI.Time.DeltaTime;

        NativeParallelMultiHashMap<Entity, CustomPhysImpulse> impMap =
            new NativeParallelMultiHashMap<Entity, CustomPhysImpulse>(
                128,
                Allocator.TempJob
            );
        ComponentLookup<LocalTransform> trfLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        ComponentLookup<PhysicsVelocity> velLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true);
        ComponentLookup<PhysicsMass> massLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true);
        JobHandle handle =
            new CustomPhysJntJob {
                dt = dt,
                trfLookup = trfLookup,
                velLookup = velLookup,
                massLookup = massLookup,
                impMap = impMap.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        handle.Complete();

        //-------------------------------------------------
        // Apply accumulated impulses
        //-------------------------------------------------

        foreach (
            var (
                physVel,
                physMass,
                trf,
                body
            )
            in SystemAPI.Query<
                RefRW<PhysicsVelocity>,
                RefRO<PhysicsMass>,
                RefRO<LocalTransform>
            >().WithEntityAccess()
        ) {
            if (
                !impMap.TryGetFirstValue(
                    body,
                    out CustomPhysImpulse imp,
                    out NativeParallelMultiHashMapIterator<Entity> it
                )
            ) {
                continue;
            }
            float3 linImp = float3.zero;
            float3 angImp = float3.zero;
            do {
                linImp += imp.linImp;
                angImp += imp.angImp;
            }
            while (
                impMap.TryGetNextValue(
                    out imp,
                    ref it
                )
            );
            physVel.ValueRW.Linear += linImp * physMass.ValueRO.InverseMass;
            physVel.ValueRW.Angular += CustomPhysUtils.ApplyInverseInertia(
                angImp,
                trf.ValueRO.Rotation,
                physMass.ValueRO.InverseInertia
            );
        }

        impMap.Dispose();
    }

    [BurstCompile]
    public partial struct CustomPhysJntJob : IJobEntity {
        public float dt;
        [ReadOnly]
        public ComponentLookup<LocalTransform> trfLookup;
        [ReadOnly]
        public ComponentLookup<PhysicsVelocity> velLookup;
        [ReadOnly]
        public ComponentLookup<PhysicsMass> massLookup;
        public NativeParallelMultiHashMap<Entity, CustomPhysImpulse>.ParallelWriter impMap;

        private void Execute(
            in CustomPhysJnt jnt,
            in CustomPhysJntTgt jntTgt,
            in CustomPhysJntTgtVel jntTgtVel,
            in CustomPhysJntBody jntBody
        ) {
            Entity body = jntBody.Body;
            LocalTransform trf = trfLookup[body];
            PhysicsVelocity physVel = velLookup[body];
            PhysicsMass physMass = massLookup[body];
            float3 linImp = float3.zero;
            float3 angImp = float3.zero;
            
            ApplyJntImp(
                jnt,
                jntTgt,
                jntTgtVel,
                trf,
                physVel,
                physMass,
                dt,
                ref linImp,
                ref angImp
            );
            impMap.Add(
                body,
                new CustomPhysImpulse {
                    linImp = linImp,
                    angImp = angImp
                }
            );
        }
    }

    private static void ApplyJntImp(
        in CustomPhysJnt jnt,
        in CustomPhysJntTgt jntTgt,
        in CustomPhysJntTgtVel jntTgtVel,
        in LocalTransform trf,
        in PhysicsVelocity physVel,
        in PhysicsMass physMass,
        float dt,
        ref float3 linImp,
        ref float3 angImp
    ) {
        if (!jnt.enabled)
            return;
        float3 worldAnchor = CustomPhysUtils.GetWorldAnchor(trf, jnt.localAnchor);
        float3 worldCenterOfMass = trf.Position + math.rotate(trf.Rotation, physMass.CenterOfMass);

        //-------------------------------------------------
        // Linear spring
        //-------------------------------------------------

        if (jnt.enableLin) {
            float3 anchorVelocity =
                CustomPhysUtils.GetPointVelocity(
                    physVel.Linear,
                    physVel.Angular,
                    worldAnchor,
                    worldCenterOfMass
                );
            float3 targetVelocity = jntTgtVel.linVel;
            float3 relativeVelocity = anchorVelocity - targetVelocity;
            float3 force =
                CustomPhysUtils.CalculateLinForce(
                    jntTgt.pos,
                    worldAnchor,
                    relativeVelocity,
                    jnt.linSpring,
                    jnt.linDamper,
                    jnt.maxForce
                );
            linImp = force * dt;
            float3 leverArm = worldAnchor - worldCenterOfMass;
            angImp += math.cross(leverArm, linImp);
        }

        //-------------------------------------------------
        // Angular spring
        //-------------------------------------------------

        if (jnt.enableAng) {
            float3 targetAngVel = jntTgtVel.angVel;
            float3 relativeAngVel =
                physVel.Angular -
                targetAngVel;
            float3 tq =
                CustomPhysUtils.CalculateAngTq(
                    trf.Rotation,
                    jntTgt.rot,
                    relativeAngVel,
                    jnt.angSpring,
                    jnt.angDamper,
                    jnt.maxTq
                );
            angImp += tq * dt;
        }
    }
}
