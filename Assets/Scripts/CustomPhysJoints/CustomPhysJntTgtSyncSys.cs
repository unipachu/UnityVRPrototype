using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(CustomPhysJntSys))]
public partial struct CustomPhysJntTgtSyncSys : ISystem {
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<CustomPhysJntTgtSync>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (
            var (
                tgtSync,
                tgt,
                tgtVel
            )
            in SystemAPI.Query<
                RefRO<CustomPhysJntTgtSync>,
                RefRW<CustomPhysJntTgt>,
                RefRW<CustomPhysJntTgtVel>
            >()
        ) {
            LocalTransform targetTransform =
                SystemAPI.GetComponent<LocalTransform>(tgtSync.ValueRO.Target);
            float3 newPos = targetTransform.Position;
            quaternion newRot = targetTransform.Rotation;
            tgtVel.ValueRW.linVel = (newPos - tgt.ValueRO.pos) / dt;
            tgtVel.ValueRW.angVel =
                CustomPhysUtils.GetRotErr(tgt.ValueRO.rot, newRot) / dt;
            tgt.ValueRW.pos = newPos;
            tgt.ValueRW.rot = newRot;
        }
    }
}