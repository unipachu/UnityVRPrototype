using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Reads player input and initializes grabs.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EcsPhysHandSys : ISystem {
    /// <summary>
    /// We teleport the hand here during grab.
    /// </summary>
    static readonly float3 hiddenPos = new float3(0f, -10000f, 0f);

    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<EcsPhysHand>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        foreach (
            var (
                physHand,
                physHandTrf,
                physHandSpring,
                physHandCol,
                physHandVel,
                physHandEntity
            ) in SystemAPI.Query<
                RefRW<EcsPhysHand>,
                RefRW<LocalTransform>,
                RefRW<EcsPhysSpring>,
                RefRW<PhysicsCollider>,
                RefRW<PhysicsVelocity>
            >().WithEntityAccess()
        ) {
            EcsPlrInput input = SystemAPI.GetComponent<EcsPlrInput>(physHand.ValueRO.plrCtrlEntity);
            bool isGrbPressed = physHand.ValueRO.handSide == Side.Left
                ? input.lGrbPressed
                : input.rGrbPressed;
            RefRW<URPMaterialPropertyBaseColor> color
                = SystemAPI.GetComponentRW<URPMaterialPropertyBaseColor>(physHand.ValueRO.meshRendererEntity);
            // NOTE: We change mesh color instead of animating it, since animations and ECS are
            // NOTE C: not natively compatible.
            color.ValueRW.Value = isGrbPressed
                // Red
                ? new float4(0, 0, 1, 1)
                // Grey
                : new float4(0.5f, 0.5f, 0.5f, 1);
            if(!isGrbPressed) {
                physHand.ValueRW.isGripping = false;
                if(physHand.ValueRW.grabbedGrblEntity != Entity.Null) {
                    // TODO MAYBE: Maybe this should leave a request for the grabbable which
                    // TODO MAYBE C: the grabbable system would then process and actually
                    // TODO MAYBE C: handle the grab release (instead of handling it in here).
                    EcsGrbl grbl = SystemAPI.GetComponent<EcsGrbl>(physHand.ValueRW.grabbedGrblEntity);
                    if (grbl.canBeReleased) {
                        Entity grabbedGrblEntity = physHand.ValueRW.grabbedGrblEntity;
                        // Remove this hand's grab entry from the grabbable's buffer.
                        DynamicBuffer<EcsGrb> grbBuffer = SystemAPI.GetBuffer<EcsGrb>(grabbedGrblEntity);
                        EcsGrb grb = default;
                        for (int i = 0; i < grbBuffer.Length; i++) {
                            if (grbBuffer[i].physHandEntity == physHandEntity) {
                                grb = grbBuffer[i];
                                // NOTE: Moves final item to the deleted spot, so if you ever do multiplayer,
                                // NOTE C: remember that the buffer grab indices do not reflect the order in which
                                // NOTE C: the grabs were initialized.
                                grbBuffer.RemoveAtSwapBack(i);
                                break;
                            }
                        }
                        LocalTransform grblTrf = SystemAPI.GetComponent<LocalTransform>(grabbedGrblEntity);
                        physHandTrf.ValueRW.Position = EcsMathNPhysUtils.TrfPtUnscaled(grblTrf, grb.initPhysHandPosInGrblLclSpc);
                        physHandTrf.ValueRW.Rotation = math.mul(grblTrf.Rotation, grb.initRotFromGrblToPhysHand);
                        physHandSpring.ValueRW.enabled = true; // fixed: re-enable, don't disable
                        physHandVel.ValueRW.Linear = float3.zero;
                        physHandVel.ValueRW.Angular = float3.zero;
                        // TODO MINOR: What filter should this use?
                        physHandCol.ValueRW.Value.Value.SetCollisionFilter(CollisionFilter.Default);
                        physHand.ValueRW.grabbedGrblEntity = Entity.Null;
                    }
                }
            }
            else if (isGrbPressed && !physHand.ValueRO.isGripping) {
                //Debug.Log("Searching for EcsGrbl...");
                physHand.ValueRW.isGripping = true;
                var hits = new NativeList<DistanceHit>(Allocator.Temp);
                bool hasHit = physicsWorld.OverlapSphere(
                    EcsMathNPhysUtils.TrfPtUnscaled(physHandTrf.ValueRO, physHand.ValueRO.grblSearchSphereLclPos),
                    physHand.ValueRO.grblSearchSphereR,
                    ref hits,
                    // TODO MINOR: Filter out all but dynamic physics objects.
                    CollisionFilter.Default
                );
                if (hasHit) {
                    float3 grblSearchSphereWldPos = EcsMathNPhysUtils.TrfPtUnscaled(
                        physHandTrf.ValueRO,
                        physHand.ValueRO.grblSearchSphereLclPos
                    );
                    Entity closestGrbl = Entity.Null;
                    float closestDist = float.MaxValue;
                    LocalTransform grblTrf;
                    float dist = 0;
                    for (int i = 0; i < hits.Length; i++) {
                        Entity hitEntity = physicsWorld.Bodies[hits[i].RigidBodyIndex].Entity;
                        if (!SystemAPI.HasComponent<EcsGrbl>(hitEntity))
                            continue;
                        EcsGrbl grbl = SystemAPI.GetComponent<EcsGrbl>(hitEntity);
                        switch (grbl.canBeGrabbed) {
                            case EcsGrblCanBeGrabbedMode.AlwaysAllow:
                                break;
                            case EcsGrblCanBeGrabbedMode.AllowMax1Grab:
                                DynamicBuffer<EcsGrb> grbBuffer = SystemAPI.GetBuffer<EcsGrb>(hitEntity);
                                if (grbBuffer.Length != 0)
                                    continue;
                                break;
                            case EcsGrblCanBeGrabbedMode.AllowMax2GrabbingHands:
                                // TODO: Check grabbing hands and already 2 grabbing, continue.
                                break;
                            default:
                                Debug.LogError("Switch defaulted");
                                break;
                        }
                        switch (grbl.distMode) {
                            case EcsGrblDistMode.ClosestOnCollider:
                                dist = hits[i].Distance;
                                break;
                            case EcsGrblDistMode.ToPivot:
                                grblTrf = SystemAPI.GetComponent<LocalTransform>(hitEntity);
                                dist = math.distance(grblSearchSphereWldPos, grblTrf.Position);
                                break;
                            case EcsGrblDistMode.ToLocalPoint:
                                grblTrf = SystemAPI.GetComponent<LocalTransform>(hitEntity);
                                float3 grabPtWldPos = EcsMathNPhysUtils.TrfPtUnscaled(grblTrf, grbl.distLclPt);
                                dist = math.distance(grblSearchSphereWldPos, grabPtWldPos);
                                break;
                            default:
                                Debug.LogError("Switch defaulted");
                                break;
                        }
                        if (dist < closestDist) {
                            closestDist = dist;
                            closestGrbl = hitEntity;
                        }
                    }
                    if (closestGrbl != Entity.Null) {
                        Debug.Log("Found EcsGrbl available for grabbing!");
                        grblTrf = SystemAPI.GetComponent<LocalTransform>(closestGrbl);
                        LocalTransform handTrf = physHandTrf.ValueRO;
                        // TODO MINOR: Use math utils. And use UNSCALED positions!
                        quaternion invGrblRot = math.inverse(grblTrf.Rotation);
                        float3 initPhysHandPosInGrblLclSpc =
                            math.mul(invGrblRot, handTrf.Position - grblTrf.Position) / grblTrf.Scale;
                        quaternion initRotFromGrblToPhysHand = math.mul(invGrblRot, handTrf.Rotation);
                        // Follow target's world pose, via the hand's own spring joint.
                        EcsPhysSpring handSpring = SystemAPI.GetComponent<EcsPhysSpring>(physHandEntity);
                        EcsPhysSpringTgt springTgt = SystemAPI.GetComponent<EcsPhysSpringTgt>(handSpring.tgt);
                        // TODO MINOR: Make sure math works and use math utils.
                        quaternion invFolTgtRot = math.inverse(springTgt.rot);
                        float3 theoInitGrbPtInSpringTgtSpc = math.mul(invFolTgtRot, handTrf.Position - springTgt.pos);
                        DynamicBuffer<EcsGrb> grbBuffer = SystemAPI.GetBuffer<EcsGrb>(closestGrbl);
                        grbBuffer.Add(new EcsGrb {
                            physHandEntity = physHandEntity,
                            initPhysHandPosInGrblLclSpc = initPhysHandPosInGrblLclSpc,
                            initRotFromGrblToPhysHand = initRotFromGrblToPhysHand,
                            theoInitGrbPtInSpringTgtSpc = theoInitGrbPtInSpringTgtSpc
                        });
                        physHand.ValueRW.grabbedGrblEntity = closestGrbl;
                        physHandSpring.ValueRW.enabled = false;
                        physHandVel.ValueRW.Linear = float3.zero;
                        physHandVel.ValueRW.Angular = float3.zero;
                        // Disable phys hand collision.
                        physHandCol.ValueRW.Value.Value.SetCollisionFilter(CollisionFilter.Zero);
                        // TODO MINOR: Make phys hand kinematic if it doesn't require changes in memory layout.
                        // Move the phys hand mesh far away (since I couldn't find a memory friendly
                        // way to disable the hand mesh).
                        physHandTrf.ValueRW.Position = hiddenPos;
                    }
                }
                hits.Dispose();
            }
            if (!isGrbPressed && physHand.ValueRO.isGripping)
                physHand.ValueRW.isGripping = false;
        }
    }
}
