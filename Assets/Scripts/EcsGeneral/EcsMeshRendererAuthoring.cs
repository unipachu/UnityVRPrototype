using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class EcsMeshRendererAuthoring : MonoBehaviour {
    public class Baker : Baker<EcsPhysSpringTgtAuthoring> {
        public override void Bake(EcsPhysSpringTgtAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(
                entity,
                new DisableRendering()
            );
        }
    }
}
