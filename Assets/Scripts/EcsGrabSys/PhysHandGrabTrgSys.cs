using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

//  TODO: Copy from here: https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master/PhysicsSamples

[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct PhysHandGrabTriggerSys : ISystem {
    //[BurstCompile]
    //public void OnCreate(ref SystemState state) {
    //    state.RequireForUpdate<StatefulTriggerEvent>();
    //}
    //public void OnUpdate(ref SystemState state) {
    //    BufferLookup<PhysHandGrabCandidate> candidateLookup =
    //        SystemAPI.GetBufferLookup<PhysHandGrabCandidate>();
    //    ComponentLookup<PhysHandGrabSensor> sensorLookup =
    //        SystemAPI.GetComponentLookup<PhysHandGrabSensor>(
    //            true
    //        );
    //    ComponentLookup<Grabbable> grabbableLookup =
    //        SystemAPI.GetComponentLookup<Grabbable>(
    //            true
    //        );
    //    foreach (
    //        StatefulTriggerEvent triggerEvent
    //        in SystemAPI
    //            .GetSingleton<
    //                StatefulTriggerEventBuffer.Singleton
    //            >()
    //            .AsNativeArray()
    //    ) {
    //        Entity entityA = triggerEvent.EntityA;
    //        Entity entityB = triggerEvent.EntityB;
    //        HandleTrigger(
    //            triggerEvent.State,
    //            entityA,
    //            entityB,
    //            candidateLookup,
    //            sensorLookup,
    //            grabbableLookup
    //        );
    //        HandleTrigger(
    //            triggerEvent.State,
    //            entityB,
    //            entityA,
    //            candidateLookup,
    //            sensorLookup,
    //            grabbableLookup
    //        );
    //    }
    //}
    //private static void HandleTrigger(
    //    StatefulEventState state,
    //    Entity sensorEntity,
    //    Entity otherEntity,
    //    BufferLookup<PhysHandGrabCandidate> candidateLookup,
    //    ComponentLookup<PhysHandGrabSensor> sensorLookup,
    //    ComponentLookup<Grabbable> grabbableLookup
    //) {
    //    // Is this side the hand sensor?
    //    if (
    //        !sensorLookup.HasComponent(sensorEntity)
    //    ) {
    //        return;
    //    }
    //    // Is the other object grabbable?
    //    if (
    //        !grabbableLookup.HasComponent(otherEntity)
    //    ) {
    //        return;
    //    }
    //    Entity hand =
    //        sensorLookup[sensorEntity]
    //            .Hand;
    //    if (
    //        !candidateLookup.HasBuffer(hand)
    //    ) {
    //        return;
    //    }
    //    DynamicBuffer<PhysHandGrabCandidate> buffer =
    //        candidateLookup[hand];
    //    if (
    //        state == StatefulEventState.Enter
    //    ) {
    //        buffer.Add(
    //            new PhysHandGrabCandidate {
    //                Entity = otherEntity
    //            }
    //        );
    //    }
    //    else if (
    //        state == StatefulEventState.Exit
    //    ) {
    //        RemoveCandidate(
    //            buffer,
    //            otherEntity
    //        );
    //    }
    //}
    //private static void RemoveCandidate(
    //    DynamicBuffer<PhysHandGrabCandidate> buffer,
    //    Entity entity
    //) {
    //    for (
    //        int i = buffer.Length - 1;
    //        i >= 0;
    //        i--
    //    ) {
    //        if (
    //            buffer[i].Entity == entity
    //        ) {
    //            buffer.RemoveAt(i);
    //        }
    //    }
    //}
}
