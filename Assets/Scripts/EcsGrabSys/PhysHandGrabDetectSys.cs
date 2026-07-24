using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct PhysHandGrabDetectSys : ISystem {
    //public void OnCreate(ref SystemState state) {
    //    state.RequireForUpdate<PhysicsWorldSingleton>();
    //}
    //public void OnUpdate(ref SystemState state) {
    //    PhysicsWorldSingleton physicsWorld =
    //        SystemAPI.GetSingleton<PhysicsWorldSingleton>();
    //    CollisionWorld collisionWorld =
    //        physicsWorld.CollisionWorld;
    //    ComponentLookup<Grabbable> grabbableLookup =
    //        SystemAPI.GetComponentLookup<Grabbable>(
    //            true
    //        );
    //    foreach (
    //        var (
    //            handGrab,
    //            candidate,
    //            transform
    //        )
    //        in SystemAPI.Query<
    //            RefRO<PhysHandGrab>,
    //            RefRW<PhysHandGrabCandidate>,
    //            RefRO<LocalTransform>
    //        >()
    //    ) {
    //        if (handGrab.ValueRO.IsGrabbing) {
    //            candidate.ValueRW.Entity = Entity.Null;
    //            continue;
    //        }
    //        candidate.ValueRW.Entity =
    //            FindClosestGrabbable(
    //                physicsWorld.PhysicsWorld,
    //                collisionWorld,
    //                grabbableLookup,
    //                transform.ValueRO.Position
    //            );
    //    }
    //}
    //private static Entity FindClosestGrabbable(
    //    PhysicsWorld physicsWorld,
    //    CollisionWorld collisionWorld,
    //    ComponentLookup<Grabbable> grabbableLookup,
    //    float3 position
    //) {
    //    Entity closest = Entity.Null;
    //    float closestDistance = float.MaxValue;
    //    NativeList<int> hits =
    //        new NativeList<int>(
    //            Allocator.Temp
    //        );
    //    OverlapSphereInput input =
    //        new OverlapSphereInput {
    //            Center = position,
    //            Radius = 0.15f,
    //            Filter = CollisionFilter.Default
    //        };
    //    if (
    //        collisionWorld.OverlapSphere(
    //            input,
    //            ref hits
    //        )
    //    ) {
    //        foreach (int rigidBodyIndex in hits) {
    //            Entity entity =
    //                physicsWorld.Bodies[rigidBodyIndex]
    //                    .Entity;
    //            if (
    //                !grabbableLookup.HasComponent(entity)
    //            ) {
    //                continue;
    //            }
    //            float distance =
    //                math.length(
    //                    physicsWorld.Bodies[rigidBodyIndex]
    //                        .WorldFromBody.pos -
    //                    position
    //                );
    //            if (distance < closestDistance) {
    //                closestDistance = distance;
    //                closest = entity;
    //            }
    //        }
    //    }
    //    hits.Dispose();
    //    return closest;
    //}
}