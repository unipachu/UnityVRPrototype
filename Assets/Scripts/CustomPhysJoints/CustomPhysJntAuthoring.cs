using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

/// <summary>
/// DOTS NOTE: Authoring scripts allow for changing ECS component values through inspector.
/// </summary>
public class CustomPhysJntAuthoring : MonoBehaviour
{
    [Header("Anchor")]
    [Tooltip("Joint anchor attached to the entity, in entiy local space.")]
    public Vector3 localAnchor;

    [Header("Target")]
    [Tooltip("Desired world position.")]
    public Vector3 targetPosition;
    [Tooltip("Desired world rotation.")]
    public Quaternion targetRotation = Quaternion.identity;

    [Header("Linear Drive")]
    [Tooltip("Whether the linear target is active.")]
    public bool enableLinear = true;
    public float linearSpring = 100f;
    public float linearDamper = 10f;
    public float maxForce = 1000f;
    public bool preventLinDampingOvershoot = true;

    [Header("Angular Drive")]
    [Tooltip("Whether the angular target is active.")]
    public bool enableAngular = true;
    public float angularSpring = 100f;
    public float angularDamper = 10f;
    public float maxTorque = 1000f;
    public bool preventAngDampingOvershoot = true;

    /// <summary>
    /// DOTS NOTE: Baker class adds components to the entity based on the authoring MonoBehaviour.
    /// </summary>
    public class CustomPhysJntBaker : Baker<CustomPhysJntAuthoring> {
        public override void Bake(CustomPhysJntAuthoring authoring) {
            // DOTS NOTE: TransformUsageFlags let's us optimize the entity's world space behavior - choose the most restrictive flag you need!
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(
                entity,
                new CustomPhysJnt {
                    localAnchor = authoring.localAnchor,
                    linSpring = authoring.linearSpring,
                    linDamper = authoring.linearDamper,
                    maxForce = authoring.maxForce,
                    preventLinDampingOvershoot = authoring.preventLinDampingOvershoot,
                    angSpring = authoring.angularSpring,
                    angDamper = authoring.angularDamper,
                    maxTq = authoring.maxTorque,
                    preventAngDampingOvershoot = authoring.preventAngDampingOvershoot,
                    enableLin = authoring.enableLinear,
                    enableAng = authoring.enableAngular
                }
            );
            AddComponent(
                entity,
                new CustomPhysJntTgt {
                    Pos = authoring.targetPosition,
                    Rot = new quaternion(
                        authoring.targetRotation.x,
                        authoring.targetRotation.y,
                        authoring.targetRotation.z,
                        authoring.targetRotation.w
                    ),
                    EnablePos = authoring.enableLinear,
                    EnableRot = authoring.enableAngular
                }
            );
        }
    }
}

/// <summary>
/// The physics joint will pull its local anchor toward the this target.
/// </summary>
public struct CustomPhysJntTgt : IComponentData {
    public float3 Pos;
    public quaternion Rot;
    // TODO: Do we need these?
    public bool EnablePos;
    public bool EnableRot;
}

/// <summary>
/// Custom physics joint. Requires a joint target component to work.
/// DOTS NOTE: Components are structs which only hold data.
/// </summary>
public struct CustomPhysJnt : IComponentData {
    public float3 localAnchor;
    // Linear drive
    public float linSpring;
    public float linDamper;
    public float maxForce;
    public bool preventLinDampingOvershoot;
    // Angular drive
    public float angSpring;
    public float angDamper;
    public float maxTq;
    public bool preventAngDampingOvershoot;
    // Enable flags
    public bool enableLin;
    public bool enableAng;
}