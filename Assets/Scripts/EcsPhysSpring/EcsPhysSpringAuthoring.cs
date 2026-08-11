using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

/// <summary>
/// DOTS NOTE: Authoring scripts allow for changing ECS component values through inspector.
/// </summary>
public class EcsPhysSpringAuthoring : MonoBehaviour {
    [Header("General")]
    [Tooltip("Completely disables forces applied by the spring.")]
    public bool springEnabled = true;

    [Header("Anchor")]
    [Tooltip("Spring anchor attached to the entity, in entiy local space.")]
    public Vector3 localAnchor;

    [Header("Spring Target")]
    [Tooltip("Entity that contains EcsPhysSpringTgt.")]
    public EcsPhysSpringTgtAuthoring target;

    [Header("Linear Drive")]
    [Tooltip("Whether the linear target is active.")]
    public bool enableLinear = true;
    public float linearSpring = 20;
    public float linearDamper = 1;
    public float maxForce = 99999;

    [Header("Angular Drive")]
    [Tooltip("Whether the angular target is active.")]
    public bool enableAngular = true;
    public float angularSpring = 10;
    public float angularDamper = 1;
    public float maxTorque = 99999;

    [Header("Connected Body")]
    public GameObject connectedBody;

    /// <summary>
    /// DOTS NOTE: Baker class adds components to the entity based on the authoring MonoBehaviour.
    /// </summary>
    public class EcsPhysSpringBaker : Baker<EcsPhysSpringAuthoring> {
        public override void Bake(EcsPhysSpringAuthoring authoring) {
            // DOTS NOTE: TransformUsageFlags let's us optimize the entity's world space behavior - choose the most restrictive flag you need!
            Entity entity = GetEntity(TransformUsageFlags.None);
            Entity bodyEntity = GetEntity(authoring.connectedBody, TransformUsageFlags.Dynamic);
            Entity targetEntity = GetEntity(authoring.target, TransformUsageFlags.None);
            AddComponent(
                entity,
                new EcsPhysSpring {
                    enabled = authoring.springEnabled,
                    lclAnch = authoring.localAnchor,
                    tgt = targetEntity,
                    linSpring = authoring.linearSpring,
                    linDamper = authoring.linearDamper,
                    maxForce = authoring.maxForce,
                    angSpring = authoring.angularSpring,
                    angDamper = authoring.angularDamper,
                    maxTq = authoring.maxTorque,
                    enableLin = authoring.enableLinear,
                    enableAng = authoring.enableAngular
                }
            );
            AddComponent(
                entity,
                new EcsPhysSpringBody {
                    body = bodyEntity
                }
            );
            AddComponent<EcsPhysSpringControlled>(entity);
        }
    }
}

/// <summary>
/// Custom physics spring. Requires a spring target component to work.
/// DOTS NOTE: Components are structs which only hold data.
/// </summary>
public struct EcsPhysSpring : IComponentData {
    public bool enabled;
    public float3 lclAnch;
    // DOTS NOTE: This is not a pointer, but a struct with entity index. In a system you can then:
        // [ReadOnly]
        // public ComponentLookup<EcsPhysSpringTgt> tgtLookup;
        // EcsPhysSpringTgt tgt = tgtLookup[spring.target];
    public Entity tgt;
    // Linear drive
    public float linSpring;
    public float linDamper;
    public float maxForce;
    // Angular drive
    public float angSpring;
    public float angDamper;
    public float maxTq;
    // Enable flags
    public bool enableLin;
    public bool enableAng;
}

/// <summary>
/// References the rigidbody entity affected by this custom physics spring.
/// </summary>
public struct EcsPhysSpringBody : IComponentData {
    public Entity body;
}

/// <summary>
/// Identifies a custom physics spring that can be controlled externally.
/// </summary>
public struct EcsPhysSpringControlled : IComponentData {
}
