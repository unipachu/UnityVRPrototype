using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Bakes physics hand specific components.
/// </summary>
public class EcsPhysHandAuthoring : MonoBehaviour{
    [Tooltip("Player controller used to identify the input source.")]
    public EcsPlrCtrlAuthoring plrCtrl;
    public MeshRenderer meshRenderer;
    public Side handSide;
    public Vector3 grblSearchSphereLclPos = new Vector3(0, -0.04f, 0.015f);
    public float grblSearchSphereR = 0.04f;

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
                    handSide = authoring.handSide,
                    grblSearchSphereLclPos = authoring.grblSearchSphereLclPos,
                    grblSearchSphereR = authoring.grblSearchSphereR
                }
            );
        }
    }
}

public struct EcsPhysHand : IComponentData {
    public Entity plrCtrlEntity;
    public Entity meshRendererEntity;
    public Side handSide;
    public float3 grblSearchSphereLclPos;
    public float grblSearchSphereR;
    /// <summary>
    /// Is the corresponding grip button pressed down?
    /// </summary>
    public bool isGripping;
    /// <summary>
    /// The grabbable entity this hand currently holds or Entity.Null if not grabbing anything.
    /// </summary>
    public Entity grabbedGrblEntity;
}