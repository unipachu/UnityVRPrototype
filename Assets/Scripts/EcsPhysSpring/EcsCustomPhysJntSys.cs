using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Physics.Extensions;
using UnityEngine;

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
        // NOTE: We require completion immediately after, but we can still calculate the spring
        // NOTE C: impulses in parallel.
        JobHandle handle =
            new EcsPhysSpringJob {
                dt = dt,
                tgtLookup = SystemAPI.GetComponentLookup<EcsPhysSpringTgt>(true)
                // DOTS NOTE: AsParallelWriter() is required for parallel writing.
            }.ScheduleParallel(state.Dependency);
        // TODO: This probably isn't needed since we don't start new jobs before completing this job handle.
        state.Dependency = handle;
        // NOTE: We wait for the job to complete since we will use its newly written data next.
        handle.Complete();
        // Apply accumulated impulses.
        foreach (var (physVel, physMass, trf, impAccum, spring)
            in SystemAPI.Query<
                RefRW<PhysicsVelocity>,
                RefRO<PhysicsMass>,
                RefRO<LocalTransform>,
                RefRW<CustomPhysImpulseAccum>,
                RefRO<EcsPhysSpring>>()
        ) {
            float3 linImp = impAccum.ValueRO.linImp;
            float3 angImp = impAccum.ValueRO.angImp;
            //ComponentLookup<EcsPhysSpringTgt> tgtLookup = SystemAPI.GetComponentLookup<EcsPhysSpringTgt>(true);
            //EcsPhysSpringTgt springTgt = tgtLookup[spring.ValueRO.tgt];
            //float3 angVelWorldBefore = physVel.ValueRO.GetAngularVelocityWorldSpace(physMass.ValueRO, trf.ValueRO.Rotation);
            //Debug.Log(
            //    $"SPRING ANGULAR\n" +
            //    $"  current rot:       {trf.ValueRO.Rotation.value}\n" +
            //    $"  target rot:        {springTgt.rot.value}\n" +
            //    $"  world ang vel:     {angVelWorldBefore}\n" +
            //    $"  target ang vel:    {springTgt.angVel}\n" +
            //    $"  angular impulse:   {angImp}\n" +
            //    $"  impulse magnitude: {math.length(angImp)}\n" +
            //    $"  inverse inertia:   {physMass.ValueRO.InverseInertia}\n" +
            //    $"  mass transform:    {physMass.ValueRO.Transform.rot.value}"
            //);
            physVel.ValueRW.ApplyLinearImpulse(physMass.ValueRO, linImp);
            quaternion worldFromMotionRot = EcsMathNPhysUtils.TrfRot(
                trf.ValueRO.Rotation,
                physMass.ValueRO.Transform.rot
            );
            float3 angImpMotion = EcsMathNPhysUtils.InvrsTrfDir(
                worldFromMotionRot,
                angImp
            );
            physVel.ValueRW.ApplyAngularImpulse(physMass.ValueRO, angImpMotion);
            impAccum.ValueRW = new CustomPhysImpulseAccum();
        }
    }

    /// <summary>
    /// Calculates spring impulse contributions and stores them in an impulse accumulator.
    /// </summary>
    [BurstCompile]
    public partial struct EcsPhysSpringJob : IJobEntity {
        public float dt;
        [ReadOnly]
        public ComponentLookup<EcsPhysSpringTgt> tgtLookup;

        private void Execute(
            in EcsPhysSpring spring,
            in LocalTransform trf,
            in PhysicsVelocity physVel,
            in PhysicsMass physMass,
            ref CustomPhysImpulseAccum accum
        ) {
            if (!spring.enabled)
                return;
            if (!tgtLookup.HasComponent(spring.tgt))
                return;
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
            accum.linImp += linImp;
            accum.angImp += angImp;
        }

        // TODO: Make separate relative velocity damper and world velocity damper and give each their own max force/torque values.
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
            // The spring always acts at the rigidbody's center of mass.
            // TODO: Use math util.
            float3 wldCom = trf.Position + math.rotate(trf.Rotation, physMass.CenterOfMass);
            // Linear spring.
            if (spring.enableLin) {
                // Since the spring acts at the COM, the point velocity
                // is simply the body's linear velocity.
                float3 relVel = physVel.Linear - springTgt.linVel;
                float3 force = EcsMathNPhysUtils.CalculateSpringLinForce(
                    springTgt.pos,
                    wldCom,
                    relVel,
                    spring.linSpring,
                    spring.linDamper,
                    spring.maxForce
                );
                linImp = force * dt;
            }
            // Angular spring.
            if (spring.enableAng) {
                float3 wldAngVel = physVel.GetAngularVelocityWorldSpace(physMass, trf.Rotation);
                float3 relAngVel = wldAngVel - springTgt.angVel;
                float3 tq = EcsMathNPhysUtils.CalculateSpringAngTq(
                    trf.Rotation,
                    springTgt.rot,
                    relAngVel,
                    spring.angSpring,
                    spring.angDamper,
                    spring.maxTq
                );
                angImp = tq * dt;
            }
        }
    }
}
