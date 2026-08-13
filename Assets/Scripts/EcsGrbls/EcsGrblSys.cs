using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Controls grabbables.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EcsGrblSys : ISystem {
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<EcsGrbl>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        foreach (
            var (grbl, grblTrf, grblSpringTgt, grblSpring, grblEntity) in SystemAPI.Query<
                RefRO<EcsGrbl>,
                RefRO<LocalTransform>,
                RefRW<EcsPhysSpringTgt>,
                RefRW<EcsPhysSpring>
            >().WithEntityAccess()
        ) {
            DynamicBuffer<EcsGrb> grbBuffer = SystemAPI.GetBuffer<EcsGrb>(grblEntity);
            if (grbBuffer.IsEmpty) {
                // TODO MINOR: You could enable and disable the spring values in PhysHand system
                // TODO MINOR C: when starting a grab instead of setting them every frame in here.
                grblSpring.ValueRW.enabled = false;
                continue;
            }
            grblSpring.ValueRW.enabled = true;
            EcsGrb grb = grbBuffer[0];
            EcsPhysSpring handSpring = SystemAPI.GetComponent<EcsPhysSpring>(grb.physHandEntity);
            EcsPhysSpringTgt folTgt = SystemAPI.GetComponent<EcsPhysSpringTgt>(handSpring.tgt);
            // TODO: Use math util (and make sure math works).
            quaternion desiredGrblRot = math.mul(folTgt.rot, math.inverse(grb.initRotFromGrblToPhysHand));
            // TODO: Use math util.
            float3 desiredGrblPos = folTgt.pos - math.mul(
                desiredGrblRot,
                grb.initPhysHandPosInGrblLclSpc * grblTrf.ValueRO.Scale
            );
            grblSpringTgt.ValueRW.pos = desiredGrblPos;
            grblSpringTgt.ValueRW.rot = desiredGrblRot;
        }
    }
}
