using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

/// <summary>
/// DOTS NOTE: Authoring scripts allow for changing ECS component values through inspector.
/// </summary>
public class EcsPhysSpringAuthoring : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Completely disables forces applied by the spring.")]
    public bool springEnabled = true;

    [Header("Identification")]
    [Tooltip("Unique identifier used by GameObject proxies.")]
    public int springId;

    [Header("Anchor")]
    [Tooltip("Spring anchor attached to the entity, in entiy local space.")]
    public Vector3 localAnchor;

    [Header("Target")]
    [Tooltip("Desired world position.")]
    public Vector3 targetPosition;
    [Tooltip("Desired world rotation.")]
    public Quaternion targetRotation = Quaternion.identity;

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

            AddComponent(
                entity,
                new EcsPhysSpring {
                    enabled = authoring.springEnabled,
                    localAnchor = authoring.localAnchor,
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
            quaternion rot = new quaternion(
                authoring.targetRotation.x,
                authoring.targetRotation.y,
                authoring.targetRotation.z,
                authoring.targetRotation.w
            );
            AddComponent(
                entity,
                new EcsPhysSpringTgt {
                    pos = authoring.targetPosition,
                    rot = rot
                }
            );
            AddComponent(
                entity,
                new EcsPhysSpringTgtVel {
                    linVel = float3.zero,
                    angVel = float3.zero
                }
            );
            AddComponent(
                entity,
                new EcsPhysSpringBody {
                    body = bodyEntity
                }
            );
            AddComponent<EcsPhysSpringControlled>(entity);
            AddComponent(
                entity,
                new EcsPhysSpringId {
                    value = authoring.springId
                }
            );
        }
    }
}

/// <summary>
/// The world-space target of a custom physics spring.
/// </summary>
public struct EcsPhysSpringTgt : IComponentData {
    public float3 pos;
    public quaternion rot;
}

/// <summary>
/// Custom physics spring. Requires a spring target component to work.
/// DOTS NOTE: Components are structs which only hold data.
/// </summary>
public struct EcsPhysSpring : IComponentData {
    public bool enabled;
    public float3 localAnchor;
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
/// Runtime velocity of the custom physics spring target.
/// Used for moving targets. Allows the spring to compensate
/// for target motion instead of treating it as teleportation.
/// </summary>
public struct EcsPhysSpringTgtVel : IComponentData {
    public float3 linVel;
    public float3 angVel;
}

/// <summary>
/// Identifies a custom physics spring that can be controlled externally.
/// </summary>
public struct EcsPhysSpringControlled : IComponentData {
}

/// <summary>
/// Uniquely identifies a custom physics spring.
/// Used by GameObject proxies to locate the correct spring entity.
/// </summary>
public struct EcsPhysSpringId : IComponentData {
    public int value;
}
