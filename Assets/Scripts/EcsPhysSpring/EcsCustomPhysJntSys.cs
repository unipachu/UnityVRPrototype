using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

/// <summary>
/// Computes and applies impulses for custom physics springs.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
public partial struct EcsPhysSpringSys : ISystem {
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<EcsPhysSpring>();
    }
    public void OnUpdate(ref SystemState state) {
        float dt = SystemAPI.Time.DeltaTime;
        // This can be used with parallel jobs.
        NativeParallelMultiHashMap<Entity, EcsPhysSpringImp> impMap =
            new NativeParallelMultiHashMap<Entity, EcsPhysSpringImp>(128, Allocator.TempJob);
        ComponentLookup<EcsPhysSpringTgt> tgtLookup = SystemAPI.GetComponentLookup<EcsPhysSpringTgt>(true);
        ComponentLookup<LocalTransform> trfLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        ComponentLookup<PhysicsVelocity> velLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true);
        ComponentLookup<PhysicsMass> massLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true);
        // NOTE: We require completion immediately after, but we can still calculate the spring
        // NOTE C: impulses in parallel.
        JobHandle handle =
            new EcsPhysSpringJob {
                dt = dt,
                tgtLookup = tgtLookup,
                trfLookup = trfLookup,
                velLookup = velLookup,
                massLookup = massLookup,
                // DOTS NOTE: AsParallelWriter() is required for parallel writing.
                impMap = impMap.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        // TODO: This probably isn't needed since we don't start new jobs before completing this job handle.
        state.Dependency = handle;
        // NOTE: We wait for the job to complete since we will use its newly written data next.
        handle.Complete();
        // Apply accumulated impulses.
        foreach (var (physVel, physMass, trf, springBody)
            in SystemAPI.Query<
                RefRW<PhysicsVelocity>,
                RefRO<PhysicsMass>,
                RefRO<LocalTransform>,
                RefRO<EcsPhysSpringBody>>()
        ) {
            Entity bodyEntity = springBody.ValueRO.body;

            if (!impMap.TryGetFirstValue(
                bodyEntity,
                out EcsPhysSpringImp imp,
                out NativeParallelMultiHashMapIterator<Entity> it)
            ) {
                // NOTE: If no springs affect a spring body, then continue.
                continue;
            }
            float3 linImp = float3.zero;
            float3 angImp = float3.zero;
            do {
                linImp += imp.linImp;
                angImp += imp.angImp;
            }
            while (impMap.TryGetNextValue( out imp, ref it));
            physVel.ValueRW.Linear += linImp * physMass.ValueRO.InverseMass;
            physVel.ValueRW.Angular += EcsMathNPhysUtils.ApplyInverseInertia(
                angImp,
                trf.ValueRO.Rotation,
                physMass.ValueRO.InverseInertia
            );
        }
        // After applying the impulses, dispose the impulse map.
        impMap.Dispose();
    }

    /// <summary>
    /// Calculates spring spring impulse contributions and stores them in an impulse multi hash map.
    /// </summary>
    [BurstCompile]
    public partial struct EcsPhysSpringJob : IJobEntity {
        public float dt;
        [ReadOnly]
        public ComponentLookup<EcsPhysSpringTgt> tgtLookup;
        [ReadOnly]
        public ComponentLookup<LocalTransform> trfLookup;
        [ReadOnly]
        public ComponentLookup<PhysicsVelocity> velLookup;
        [ReadOnly]
        public ComponentLookup<PhysicsMass> massLookup;
        // Job writes to this hash map.
        public NativeParallelMultiHashMap<Entity, EcsPhysSpringImp>.ParallelWriter impMap;

        private void Execute(
            in EcsPhysSpring spring,
            in EcsPhysSpringBody springBody
        ) {
            if (!spring.enabled)
                return;
            Entity bodyEntity = springBody.body;
            if (!tgtLookup.HasComponent(spring.tgt))
                return;
            LocalTransform trf = trfLookup[bodyEntity];
            PhysicsVelocity physVel = velLookup[bodyEntity];
            PhysicsMass physMass = massLookup[bodyEntity];
            EcsPhysSpringTgt springTgt = tgtLookup[spring.tgt];
            float3 linImp = float3.zero;
            float3 angImp = float3.zero;
            CalculateSpringImpulse(
                spring,
                springTgt,
                trf,
                physVel,
                physMass,
                dt,
                ref linImp,
                ref angImp
            );
            impMap.Add(
                bodyEntity,
                new EcsPhysSpringImp {linImp = linImp, angImp = angImp}
            );
        }

        public static void CalculateSpringImpulse(
            in EcsPhysSpring spring,
            in EcsPhysSpringTgt springTgt,
            in LocalTransform trf,
            in PhysicsVelocity physVel,
            in PhysicsMass physMass,
            float dt,
            ref float3 linImp,
            ref float3 angImp
        ) {
            float3 worldAnchor = EcsMathNPhysUtils.TransformPointIgnoreScale(trf, spring.lclAnch);
            float3 worldCenterOfMass = trf.Position + math.rotate(trf.Rotation, physMass.CenterOfMass);
            // Linear spring.
            if (spring.enableLin) {
                float3 anchorVelocity = EcsMathNPhysUtils.GetPointVelocity(
                    physVel.Linear,
                    physVel.Angular,
                    worldAnchor,
                    worldCenterOfMass
                );
                float3 relativeVelocity = anchorVelocity - springTgt.linVel;
                float3 force = EcsMathNPhysUtils.CalculateSpringLinForce(
                    springTgt.pos,
                    worldAnchor,
                    relativeVelocity,
                    spring.linSpring,
                    spring.linDamper,
                    spring.maxForce
                );
                linImp = force * dt;
                float3 leverArm = worldAnchor - worldCenterOfMass;
                // NOTE: If local anchor is not at the COM, the "linear spring" will also generate
                // NOTE C: angular impulse.
                angImp += math.cross(leverArm, linImp);
            }
            // Angular spring.
            if (spring.enableAng) {
                float3 relativeAngVel = physVel.Angular - springTgt.angVel;
                float3 tq = EcsMathNPhysUtils.CalculateSpringAngTq(
                    trf.Rotation,
                    springTgt.rot,
                    relativeAngVel,
                    spring.angSpring,
                    spring.angDamper,
                    spring.maxTq
                );
                angImp += tq * dt;
            }
        }
    }
}
