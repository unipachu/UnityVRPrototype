using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/// <summary>
/// Reads inputs and changes color for now.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct EcsPhysHandSys : ISystem {
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<EcsPhysHand>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        foreach (var physHand in SystemAPI.Query<RefRW<EcsPhysHand>>()) {
            EcsPlrInput input = SystemAPI.GetComponent<EcsPlrInput>(physHand.ValueRO.plrCtrlEntity);
            bool isGrbPressed = physHand.ValueRO.handSide == Side.Left
                ? input.lGrbPressed
                : input.rGrbPressed;
            RefRW<URPMaterialPropertyBaseColor> color
                = SystemAPI.GetComponentRW<URPMaterialPropertyBaseColor>(physHand.ValueRO.meshRendererEntity);
            // Change material based on input.
            color.ValueRW.Value = isGrbPressed
                // Red
                ? new float4(1, 0, 0, 1)
                // Grey
                : new float4(0.5f, 0.5f, 0.5f, 1);
        }
    }
}
