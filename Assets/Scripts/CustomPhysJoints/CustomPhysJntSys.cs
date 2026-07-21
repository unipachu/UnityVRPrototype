using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

/// <summary>
/// DOTS NOTE: Systems can have logic to read/write components.
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
public partial struct CustomPhysJntSys : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<CustomPhysJnt>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (
            var (jnt,
                jntTgt,
                trf,
                vel,
                mass
            ) in SystemAPI.Query<
                RefRO<CustomPhysJnt>,
                RefRO<CustomPhysJntTgt>,
                RefRO<LocalTransform>,
                RefRW<PhysicsVelocity>,
                RefRO<PhysicsMass>
            >()
        ) {
            ApplyJnt(
                jnt.ValueRO,
                jntTgt.ValueRO,
                trf.ValueRO,
                ref vel.ValueRW,
                mass.ValueRO,
                dt
            );
        }
    }

    private static void ApplyJnt(
        in CustomPhysJnt jnt,
        in CustomPhysJntTgt tgt,
        in LocalTransform trf,
        ref PhysicsVelocity physVel,
        in PhysicsMass physMass,
        float deltaTime)
    {
        float3 worldAnchor = CustomPhysUtils.GetWorldAnchor(trf, jnt.localAnchor);
        float3 worldCenterOfMass = trf.Position + math.rotate(trf.Rotation, physMass.CenterOfMass);
        float3 linImpulse = float3.zero;
        float3 angImpulse = float3.zero;

        //-------------------------------------------------
        // Linear spring
        //-------------------------------------------------

        if (jnt.enableLin) {
            float3 anchorVelocity = CustomPhysUtils.GetPointVelocity(
                physVel.Linear,
                physVel.Angular,
                worldAnchor,
                worldCenterOfMass
            );
            float3 force = CustomPhysUtils.CalculateLinForce(
                tgt.Pos,
                worldAnchor,
                anchorVelocity,
                jnt.linSpring,
                jnt.maxForce
            );
            // NOTE: Apparently when a force is applied to a point on a real world rigidbody,
            // NOTE C: the impulse is divided between linear and angular impulses like this.
            linImpulse = force * deltaTime;
            float3 leverArm = worldAnchor - worldCenterOfMass;
            angImpulse += math.cross(leverArm, linImpulse);
        }

        //-------------------------------------------------
        // Angular spring
        //-------------------------------------------------

        if (jnt.enableAng) {
            float3 torque = CustomPhysUtils.CalculateAngTq(
                trf.Rotation,
                tgt.Rot,
                physVel.Angular,
                jnt.angSpring,
                jnt.maxTq
            );
            angImpulse += torque * deltaTime;
        }

        //-------------------------------------------------
        // Apply linear impulse
        //-------------------------------------------------

        physVel.Linear += linImpulse * physMass.InverseMass;

        //-------------------------------------------------
        // Apply angular impulse
        //-------------------------------------------------

        physVel.Angular += CustomPhysUtils.ApplyInverseInertia(
            angImpulse,
            trf.Rotation,
            physMass.InverseInertia
        );
    }
}
