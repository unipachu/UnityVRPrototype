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
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
public partial struct EcsPhysSpringSys : ISystem {
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<EcsPhysSpring>();
    }

    [BurstCompile]
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
            // NOTE: Compilation should fail if you try to burst compile Debug.Log call, since
            // NOTE C: it uses managed objects, so remove burst compile attribute before using this!
            // NOTE C: Actually I was wrong, Debug.Log DOES work with Burst methods - some
            // NOTE C: comilation magic happens there.
            //{
            //    FixedString128Bytes linImpMsg = $"lin imp: {linImp}";
            //    Debug.Log(linImpMsg);
            //    ComponentLookup<EcsPhysSpringTgt> tgtLookup = SystemAPI.GetComponentLookup<EcsPhysSpringTgt>(true);
            //    EcsPhysSpringTgt springTgt = tgtLookup[spring.ValueRO.tgt];
            //    float3 angVelWorldBefore = physVel.ValueRO.GetAngularVelocityWorldSpace(physMass.ValueRO, trf.ValueRO.Rotation);
            //    Debug.Log($"target pos: {springTgt.pos}");
            //    Debug.Log(
            //        $"SPRING ANGULAR\n" +
            //        $"  current rot:       {trf.ValueRO.Rotation.value}\n" +
            //        $"  target rot:        {springTgt.rot.value}\n" +
            //        $"  world ang vel:     {angVelWorldBefore}\n" +
            //        $"  target ang vel:    {springTgt.angVel}\n" +
            //        $"  angular impulse:   {angImp}\n" +
            //        $"  impulse magnitude: {math.length(angImp)}\n" +
            //        $"  inverse inertia:   {physMass.ValueRO.InverseInertia}\n" +
            //        $"  mass transform:    {physMass.ValueRO.Transform.rot.value}"
            //    );
            //}
            physVel.ValueRW.ApplyLinearImpulse(physMass.ValueRO, linImp);
            quaternion worldFromMotionRot = EcsMathNPhysUtils.TrfRot(
                trf.ValueRO.Rotation,
                physMass.ValueRO.Transform.rot
            );
            // NOTE: ApplyAngularImpulse uses local space.
            float3 angImpLclSpc = EcsMathNPhysUtils.InvrsTrfDir(worldFromMotionRot, angImp);
            physVel.ValueRW.ApplyAngularImpulse(physMass.ValueRO, angImpLclSpc);
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
            float3 comWldSpc = EcsMathNPhysUtils.TrfPtUnscaled(trf, physMass.CenterOfMass);
            // Linear spring.
            if (spring.enableLin) {
                // Since the spring acts at the COM, the point velocity
                // is simply the body's linear velocity.
                float3 curLinVel = physVel.Linear;
                float3 relLinVel = curLinVel - springTgt.linVel;
                float3 force = EcsMathNPhysUtils.CalculateSpringLinForce(
                    springTgt.pos,
                    comWldSpc,
                    relLinVel,
                    curLinVel,
                    spring.linSpring,
                    spring.maxLinSpringForce,
                    spring.linVelMatchDamper,
                    spring.maxLinVelMatchDamperForce,
                    spring.linDragDamper,
                    spring.maxLinDragDamperForce,
                    spring.maxLinTotalForce
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
                    wldAngVel,
                    spring.angSpring,
                    spring.maxAngSpringTq,
                    spring.angVelMatchDamper,
                    spring.maxAngVelMatchDamperTq,
                    spring.angDragDamper,
                    spring.maxAngDragDamperTq,
                    spring.maxTotalTq
                );
                angImp = tq * dt;
            }
        }
    }
}
