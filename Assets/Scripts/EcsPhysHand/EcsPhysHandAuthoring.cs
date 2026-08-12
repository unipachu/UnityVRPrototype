using Unity.Entities;
using UnityEngine;

/// <summary>
/// Bakes physics hand specific components.
/// </summary>
public class EcsPhysHandAuthoring : MonoBehaviour{
    [Tooltip("Player controller used to identify the input source.")]
    public EcsPlrCtrlAuthoring plrCtrl;
    public MeshRenderer meshRenderer;
    public Side handSide;

    public class Baker : Baker<EcsPhysHandAuthoring> {
        public override void Bake(EcsPhysHandAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            Entity plrCtrlEntity = GetEntity(authoring.plrCtrl, TransformUsageFlags.None);
            Entity meshRendererEntity = GetEntity(authoring.meshRenderer, TransformUsageFlags.None);
            AddComponent(
                entity,
                new EcsPhysHand {
                    plrCtrlEntity = plrCtrlEntity,
                    meshRendererEntity = meshRendererEntity,
                    handSide = authoring.handSide
                }
            );
        }
    }
}

public struct EcsPhysHand : IComponentData {
    public Entity plrCtrlEntity;
    public Entity meshRendererEntity;
    public Side handSide;
}