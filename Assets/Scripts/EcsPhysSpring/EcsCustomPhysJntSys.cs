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
// DOTS NOTE: BurstCompile attribute here makes burst compile default on all eligible methods of this type.
// DOTS NOTE C: With this attribute the methods themselves shouldn't need the BurstCompile attribute, unless
// DOTS NOTE C: BurstCompile options are selected per method.
[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
// We want this to open just before the physics simulation so that the impulses are then applied in the physics simulation.
[UpdateBefore(typeof(PhysicsSimulationGroup))]
// DOTS NOTE: struct is partial because Unity automatically creates some additional code for the system.
public partial struct EcsPhysSpringSys : ISystem {
    // DOTS NOTE: This is run when the ECS World is created. This would typically be before Awake (unless you create a
    // DOTS NOTE C: World at runtime).
    // DOTS NOTE: The "state" parameter belongs to this system specifically. Each system has their own SystemState instance
    // DOTS NOTE C: which represents that particular system's state. Internally Unity tracks dependencies between systems,
    // DOTS NOTE C: and so SystemState can be used to e.g. make jobs from other systems depend on this system's jobs.
    public void OnCreate(ref SystemState state) {
        // DOTS NOTE: Specifies when OnUpdate should run.
        // DOTS NOTE C: Here, the system only updates if at least one EcsPhysSpring exists in the World.
        state.RequireForUpdate<EcsPhysSpring>();
    }

    // DOTS NOTE: OnUpdate runs at a point in Unity's Player Loop determined by the
    // DOTS NOTE C: UpdateInGroup, UpdateBefore, and UpdateAfter attributes.
    // DOTS NOTE C: Systems themselves do not run in parallel. Parallelization is
    // DOTS NOTE C: only achieved by scheduling jobs within a system.
    public void OnUpdate(ref SystemState state) {
        // DOTS NOTE: SystemAPI.Time.DeltaTime returns the ECS World's simulation timestep.
        // DOTS NOTE C: For systems running in PhysicsSystemGroup, this is the fixed physics timestep,
        // DOTS NOTE C: not the real world time elapsed between OnUpdate calls.
        float dt = SystemAPI.Time.DeltaTime;
        // DOTS NOTE : This creates an Entity -> EcsPhysSpringImp hash map that can store multiple
        // DOTS NOTE C: values per key (many impulses per entity). NativeParallel means this is thread safe so multiple worker threads
        // DOTS NOTE C: can execute impMap.Add() in parallel.
        // DOTS NOTE C: Size parameter is not maximum, the container can grow if needed.
        // DOTS NOTE C: The allocator parameter TempJob is meant for data that only lives a few frames
        // DOTS NOTE C: (we destroy this impMap later during this OnUpdate anyway, so this is okay) and
        // DOTS NOTE C: allows for jobs to use this. A faster allocator would be Temp, which allocates only
        // DOTS NOTE C: for one thread for one frame. A slower allocator would be Persistent, which liver longer,
        // DOTS NOTE C: but requires to be manually disposed of.
        // TODO: I believe ECS tries to avoid pointers. Are we breaking ECS rules here? Maybe not since Entity is a struct.
        NativeParallelMultiHashMap<Entity, EcsPhysSpringImp> impMap =
            new NativeParallelMultiHashMap<Entity, EcsPhysSpringImp>(
                128,
                Allocator.TempJob
            );
        // DOTS NOTE: Component lookups let you access components by entity e.g.:
        // DOTS NOTE C: LocalTransform trf = trfLookup[entity];
        // DOTS NOTE C: The parameter "true" marks these as read only, which tells the ECS that other systems/jobs
        // DOTS NOTE C: which also only read the component can run in parallel. SystemAPI is only availabe on 
        // DOTS NOTE C: the main thread, so we get the lookups here and pass them to the job.
        // DOTS NOTE C: You could alternatively use: state.GetComponentLookup<LocalTransform>(true);
        ComponentLookup<LocalTransform> trfLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        ComponentLookup<PhysicsVelocity> velLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true);
        ComponentLookup<PhysicsMass> massLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true);
        // DOTS NOTE: We could assign this job handle to state.Dependency, which would inform the ECS
        // DOTS NOTE C: dependency system that this system has scheduled a job. Jobs from other systems
        // DOTS NOTE C: with conflicting component access would then automatically depend on this job.
        // DOTS NOTE C: Here we do not do that because we complete the job before leaving OnUpdate.
        // DOTS NOTE C: We use the parameter in ScheduleParallel(state.Dependency) to tell the job
        // DOTS NOTE C: scheduler to wait for other jobs (of other systems which this job depend on) to
        // DOTS NOTE C: finish before starting this job.
        // TODO: If no dependency parameter is set, does the main thread ever wait for the job to finish?
        JobHandle handle =
            new EcsPhysSpringJob {
                dt = dt,
                trfLookup = trfLookup,
                velLookup = velLookup,
                massLookup = massLookup,
                impMap = impMap.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        // DOTS NOTE: We wait for the job to complete since we will use its newly written data next.
        // DOTS NOTE C: That is also why this job probably does not run parallel with anything and
        // DOTS NOTE C: is probably useless here. But it is good for demonstrative purposes.
        handle.Complete();

        // Apply accumulated impulses.
        // DOTS NOTE: SystemAPI.Query let's you loop over all entities which have all of these components.
        // DOTS NOTE C: WithEntityAccess() also returns the Entity itself with the query.
        foreach (var (physVel, physMass, trf, springAttachedEntity)
            in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<PhysicsMass>, RefRO<LocalTransform>>().WithEntityAccess()
        ) {
            if (
                !impMap.TryGetFirstValue(
                    springAttachedEntity,
                    out EcsPhysSpringImp imp,
                    out NativeParallelMultiHashMapIterator<Entity> it
                )
            ) {
                // TODO: Probably should not go here. Throw an error?
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
            // DOTS NOTE: The query declares which components are accessed as readonly and which are accessed as readwrite.
            // DOTS NOTE C: The query returns RefRO/RefRW wrappers that provide access to the actual component data through
            // DOTS NOTE C: ValueRO and ValueRW.
            physVel.ValueRW.Linear += linImp * physMass.ValueRO.InverseMass;
            physVel.ValueRW.Angular += EcsMathNPhysUtils.ApplyInverseInertia(
                angImp,
                trf.ValueRO.Rotation,
                physMass.ValueRO.InverseInertia
            );
        }
        // DOTS NOTE: I'm not fully sure how Allocator.TempJob works. Even though it is meant for short
        // DOTS NOTE C: lived allocations, it still needs to be manually disposed.
        impMap.Dispose();
    }

    /// <summary>
    /// Calculates spring spring impulse contributions and stores them in an impulse multi hash map.
    /// </summary>
    // DOTS NOTE: Nested types do not inherit attributes from their containing type,
    // DOTS NOTE C: so BurstCompile must be applied explicitly to this job.
    [BurstCompile]
    public partial struct EcsPhysSpringJob : IJobEntity {
        public float dt;
        // DOTS NOTE: ReadOnly attribute is used for the job safety system.
        [ReadOnly]
        public ComponentLookup<LocalTransform> trfLookup;
        [ReadOnly]
        public ComponentLookup<PhysicsVelocity> velLookup;
        [ReadOnly]
        public ComponentLookup<PhysicsMass> massLookup;
        // Job writes to this hash map.
        public NativeParallelMultiHashMap<Entity, EcsPhysSpringImp>.ParallelWriter impMap;

        // DOTS NOTE: Execute is called once for every entity that matches the component requirements defined by its parameters.
        // DOTS NOTE C: Here the job source generator automatically creates a component query with e.g. RefRO<EcsPhysSpring>.
        // DOTS NOTE C: "in" means the component data is passed by a readonly reference. If you used "ref" instead, you could
        // DOTS NOTE C: also write to the component, e.g.: ref EcsPhysSpring spring.
        private void Execute(
            in EcsPhysSpring spring,
            in EcsPhysSpringTgt springTgt,
            in EcsPhysSpringTgtVel springTgtVel,
            in EcsPhysSpringBody springBody
        ) {
            Entity springAttachedEntity = springBody.body;
            LocalTransform trf = trfLookup[springAttachedEntity];
            PhysicsVelocity physVel = velLookup[springAttachedEntity];
            PhysicsMass physMass = massLookup[springAttachedEntity];
            float3 linImp = float3.zero;
            float3 angImp = float3.zero;

            CalculateAndApplySpringImpulse(
                spring,
                springTgt,
                springTgtVel,
                trf,
                physVel,
                physMass,
                dt,
                ref linImp,
                ref angImp
            );
            impMap.Add(
                springAttachedEntity,
                new EcsPhysSpringImp {
                    linImp = linImp,
                    angImp = angImp
                }
            );
        }

        // DOTS NOTE: This should be automatically burst compiled because of the attribute in the containing type.
        public static void CalculateAndApplySpringImpulse(
            in EcsPhysSpring spring,
            in EcsPhysSpringTgt springTgt,
            in EcsPhysSpringTgtVel springTgtVel,
            in LocalTransform trf,
            in PhysicsVelocity physVel,
            in PhysicsMass physMass,
            float dt,
            ref float3 linImp,
            ref float3 angImp
        ) {
            if (!spring.enabled)
                return;
            float3 worldAnchor = EcsMathNPhysUtils.TransformPointIgnoreScale(trf, spring.localAnchor);
            float3 worldCenterOfMass = trf.Position + math.rotate(trf.Rotation, physMass.CenterOfMass);

            // Linear spring
            if (spring.enableLin) {
                float3 anchorVelocity =
                    EcsMathNPhysUtils.GetPointVelocity(
                        physVel.Linear,
                        physVel.Angular,
                        worldAnchor,
                        worldCenterOfMass
                    );
                float3 targetVelocity = springTgtVel.linVel;
                float3 relativeVelocity = anchorVelocity - targetVelocity;
                float3 force =
                    EcsMathNPhysUtils.CalculateSpringLinForce(
                        springTgt.pos,
                        worldAnchor,
                        relativeVelocity,
                        spring.linSpring,
                        spring.linDamper,
                        spring.maxForce
                    );
                linImp = force * dt;
                float3 leverArm = worldAnchor - worldCenterOfMass;
                angImp += math.cross(leverArm, linImp);
            }

            // Angular spring
            if (spring.enableAng) {
                float3 targetAngVel = springTgtVel.angVel;
                float3 relativeAngVel = physVel.Angular - targetAngVel;
                float3 tq =
                    EcsMathNPhysUtils.CalculateSpringAngTq(
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
