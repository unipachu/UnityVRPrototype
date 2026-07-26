using System.Collections.Generic;
using UnityEngine;

public class BasicGrabbable : MonoBehaviour, IGrabbable {
    [SerializeField] ConfigurableJoint grabJnt;
    [SerializeField] Rigidbody rb;
    [Tooltip("Hand visual used to represent grabbing hand. \n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject lHandVisProxy;

    readonly List<Grab> grabs = new(2);

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    protected virtual void FixedUpdate() {
        if (grabs.Count == 1) {
            //grabs[0].physicsHand.SetWorldJointTargetToHandController();
            UpdateSingleGrabJnt();
        }
        else {
            UpdateMultiGrabJnt();
            //MultigrabJointSetup();
        }
    }

    void OnDisable() {
        ReleaseAllGrabs();
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    public bool CanBeGrabbed(PhysHand physHand) {
        return true;
    }

    public bool CanBeReleased(PhysHand physHand) {
        return true;
    }

    /// <summary>
    /// Finds <see cref="Grab"/> by the <see cref="PhysHand"/>.
    /// If <see cref="PhysHand"/> is not grabbing this, returns null.
    /// </summary>
    public Grab FindGrab(PhysHand physHand) {
        for (int i = 0; i < grabs.Count; i++) {
            if (grabs[i].physHand == physHand)
                return grabs[i];
        }
        Debug.LogWarning($"{physHand.name} was not grabbing {gameObject.name}!", this);
        return null;
    }

    public float GetDistanceToGrabPoint(Vector3 physHandWorldGrabPoint) {
        return Vector3.Distance(transform.position, physHandWorldGrabPoint);
    }

    public void InitiateGrab(PhysHand physHand) {
        // NOTE: Phys hand should check if the grabbable can be grabbed before calling this method.
        //if (!CanBeGrabbed(physHand))
        //    return false;
        Vector3 grabPointWorldPos = physHand.grabPoint.position;
        var newGrab = new Grab(
            physHand, 
            GeneralUtils.UnscaledInverseTransformPoint(transform, physHand.transform.position),
            GeneralUtils.RotationFromWorldToTransformSpace(transform, physHand.transform.rotation)
        );
        grabs.Add(newGrab);
        // Setup hand proxy visual.
        lHandVisProxy.SetActive(true);
        lHandVisProxy.transform.position = physHand.transform.position;
        lHandVisProxy.transform.rotation = physHand.transform.rotation;
    }

    public void ReleaseGrab(PhysHand physHand) {
        Grab grab = FindGrab(physHand);
        ReleaseGrab(grab);
        lHandVisProxy.SetActive(false);
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void ReleaseAllGrabs() {
        foreach (Grab grab in grabs)
            grab.physHand.OnGrabReleased(
                GeneralUtils.UnscaledTransformPoint(transform, grab.initPhysHandPosInGrabbableLocalSpace),
                GeneralUtils.RotationFromTransformSpaceToWorld(transform, grab.initRotFromGrabbableToPhysHand)
            );
        grabs.Clear();
        SetJntDrivesToZero();
    }

    void ReleaseGrab(Grab grab) {
        grab.physHand.OnGrabReleased(
            GeneralUtils.UnscaledTransformPoint(transform, grab.initPhysHandPosInGrabbableLocalSpace),
            GeneralUtils.RotationFromTransformSpaceToWorld(transform, grab.initRotFromGrabbableToPhysHand)
        ); grabs.Remove(grab);
        if (grabs.Count == 0)
            SetJntDrivesToZero();
    }

    /// <summary>
    /// E.g. when no hands are grabbing this.
    /// </summary>
    void SetJntDrivesToZero() {
        JointDrive jntDrive = new JointDrive();
        // Linear drives:
        jntDrive.positionSpring = 0;
        jntDrive.positionDamper = 0;
        jntDrive.maximumForce = 0;
        grabJnt.xDrive = jntDrive;
        grabJnt.yDrive = jntDrive;
        grabJnt.zDrive = jntDrive;
        // Angular drive:
        jntDrive.positionSpring = 0;
        jntDrive.positionDamper = 0;
        jntDrive.maximumForce = 0;
        grabJnt.slerpDrive = jntDrive;
    }

    void UpdateMultiGrabJnt() {
        // TODO
    }

    void UpdateSingleGrabJnt() {
        Grab grab = grabs[0];
        Transform ctrlTrf = grab.physHand.followTgtTrf;
        Quaternion targetWorldRot =
            ctrlTrf.rotation * Quaternion.Inverse(grab.initRotFromGrabbableToPhysHand);
        Vector3 targetWorldPos =
            ctrlTrf.position - targetWorldRot * grab.initPhysHandPosInGrabbableLocalSpace;
        grabJnt.targetPosition = targetWorldPos;
        grabJnt.targetRotation = targetWorldRot;
        // TODO: We update joint drives every frame. For this grabbable we could just update every time grab configuration changes.
        PhysHandNGrabbableUtils.SetWorldJntDrivesToDflt(grabJnt, grab.physHand.jntData);
    }
}
