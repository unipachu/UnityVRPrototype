using UnityEngine;

/// <summary>
/// Applies spring movement to the Rigidbody or the Transform of this game object.
/// </summary>
public class SpringMov : MonoBehaviour{
    [Header("Settings")]
    public bool startAtTgt = true;
    public float linSpring = 10;
    public float linVelMatchDamper = 5;
    public float linDragDamper = 1;
    public float maxLinAcc = 99999;
    public float angSpring = 1;
    public float angVelMatchDamper = 1;
    public float angDragDamper = 1;
    public float maxAngAcc = 99999;

    [Header("Refs")]
    [Tooltip("Rigidbody to be moved with Rigidbody.Move. " +
        "Should be KINEMATIC for stable spring calculations and interpolation. " +
        "If empty, the Transform is moved directly.")]
    [SerializeField] Rigidbody rb;
    [Tooltip("Target for the spring.")]
    public Transform tgt;

    MotSt motSt = new();
    MotSt tgtMotSt = new();
    Pose tgtPrevPose;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        tgtPrevPose = new(tgt.position, tgt.rotation);
        if (startAtTgt) {
            transform.position = tgt.position;
            transform.rotation = tgt.rotation;
        }
    }

    void Update() {
        if (rb != null)
            return;
        UpdateTgtMotSt(Time.deltaTime);
        Vector3 rbPos = transform.position;
        Quaternion rbRot = transform.rotation;
        MathUtils.UpdateSpringTrf(
            ref rbPos,
            ref rbRot,
            ref motSt,
            in tgtMotSt,
            tgt.position,
            tgt.rotation,
            Time.deltaTime,
            linSpring,
            linVelMatchDamper,
            linDragDamper,
            angSpring,
            angVelMatchDamper,
            angDragDamper,
            maxLinAcc,
            maxAngAcc
        );
        transform.SetPositionAndRotation(rbPos, rbRot);
        // Update tgt prev pose.
        tgtPrevPose = new(tgt.position, tgt.rotation);
    }

    void FixedUpdate() {
        if (rb == null)
            return;
        UpdateTgtMotSt(Time.fixedDeltaTime);
        Vector3 rbPos = rb.position;
        Quaternion rbRot = rb.rotation;
        MathUtils.UpdateSpringTrf(
            ref rbPos,
            ref rbRot,
            ref motSt,
            in tgtMotSt,
            tgt.position,
            tgt.rotation,
            Time.fixedDeltaTime,
            linSpring,
            linVelMatchDamper,
            linDragDamper,
            angSpring,
            angVelMatchDamper,
            angDragDamper,
            maxLinAcc,
            maxAngAcc
        );
        rb.Move(rbPos, rbRot);
        // Update tgt prev pose.
        tgtPrevPose = new(tgt.position, tgt.rotation);
    }

    /// <summary>
    /// Saves target linear and angular velocities.
    /// </summary>
    void UpdateTgtMotSt(float dt) {
        // Make sure we do not divide by a zero delta time.
        if (dt > 0) {
            // Linear velocity.
            tgtMotSt.linVel = (tgt.position - tgtPrevPose.position) / dt;
            // Angular velocity.
            tgtMotSt.angVel = MathUtils.AngVel(tgtPrevPose.rotation, tgt.rotation, dt);
        }
        else {
            tgtMotSt.linVel = Vector3.zero;
            tgtMotSt.angVel = Vector3.zero;
        }
    }
}
