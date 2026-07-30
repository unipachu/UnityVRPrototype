using UnityEngine;

/// <summary>
/// Axe grabbable.
/// </summary>
public class GrblJntDriven_AxeGrbl : MonoBehaviour, IGrblJntDriven_Grbl, IDblGrb_GrbLineAligned {
    [Header("Refs")]
    [SerializeField] GrblJntDriven_GrblCore grblCore;
    [Tooltip("Hand visual used to represent grabbing left hand.\n" +
    "Set the hand visual inactive in editor!")]
    // TODO: You could probably do without the proxy holders...
    [SerializeField] GameObject lHandVisProxyHolder;
    [SerializeField] GameObject lHandVisProxyVis;
    [Tooltip("Hand visual used to represent grabbing right hand.\n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject rHandVisProxyHolder;
    [SerializeField] GameObject rHandVisProxyVis;
    [SerializeField] float minGrbPtLclY = -0.49f;
    [SerializeField] float maxGrbPtHandLclY = -0.03f;

    // Finite state machine
    [HideInInspector] public Fsm grbJntFsm = new();
    [HideInInspector] public GrblJntDriven_GrblJntSt_DblGrb_GrbLineAligned jntSt_DblGrb_GrbLineAligned;
    [HideInInspector] public GrblJntDriven_GrblJntSt_NoGrb jntSt_NoGrb;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv jntSt_SglGrb_SimpleAnchAtPiv;

    readonly Vector3 followTgtInitGrabPtInFollowTgtSpc = new Vector3(0, -0.025f, 0);

    public GrblJntDriven_GrblCore GrblCore => grblCore;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Awake() {
        // Initialize FSM states.
        jntSt_DblGrb_GrbLineAligned = new(this, this);
        jntSt_NoGrb = new(this);
        jntSt_SglGrb_SimpleAnchAtPiv = new(this);
    }

    void Start() {
        grbJntFsm.SwitchState(jntSt_NoGrb, this);
    }

    void FixedUpdate() {
        grbJntFsm.CurrentState.PhysicsTick();
    }

    void Update() {
        if (grblCore.grbs.Count == 2)
            UpdateUpperProxyHand();
    }

    void OnDrawGizmos() {
        GrblUtils.OnDrawGizmos_DrawGrbJntAnchor(this);
    }

    void OnDisable() {
        ReleaseAllGrbs();
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    public bool CanBeGrabbed(GrblJntDriven_PhysHand physHand) {
        // Can be grabbed by up to one left hand and one right hand simultaneously.
        return grblCore.GrbCount(physHand.side) == 0;
    }

    public bool CanBeReleased(GrblJntDriven_PhysHand physHand) {
        return true;
    }

    public float GetDistToGrbPt(Vector3 physHandWorldGrbPt) {
        return Vector3.Distance(transform.position, physHandWorldGrbPt);
    }

    public float GetPosHand0Wt() => 1 - LowestHandIndex();
    
    public float GetRotHand0Wt() => 1 - LowestHandIndex();

    // We want the grab to always align with the axe handle.
    // TODO: actually we want the grab point align with the handle and then use grab point
    // TODO C: to calculate everything. I made a mistake by using just the physicsHand position
    // TODO C: for grab calculations. Also grab point and grab overlapsphere pos should be separate.
    // TODO C: Except... different grabbables might want to attach to different points of the hand.
    // TODO C: Hmm... Maybe grabbables should just have some sort of hard coded offset for the
    // TODO C: grabbable-specific grab point.
    public void InitiateGrb(GrblJntDriven_PhysHand physHand) {
        Vector3 initPhysHandPosInGrblSpc = MathUtils.InvrsTrfPtUnscaled(transform, physHand.transform.position);
        // We clamp init phys hand pos so that it aligns with the handle.
        initPhysHandPosInGrblSpc.x = 0;
        initPhysHandPosInGrblSpc.y = Mathf.Clamp(initPhysHandPosInGrblSpc.y, minGrbPtLclY, maxGrbPtHandLclY);
        initPhysHandPosInGrblSpc.z = 0;
        var newGrb = new Grb(
            physHand,
            initPhysHandPosInGrblSpc,
            MathUtils.InvrsTrfRot(transform, physHand.transform.rotation),
            followTgtInitGrabPtInFollowTgtSpc
        );
        grblCore.grbs.Add(newGrb);
        // Setup hand proxy visual.
        GameObject proxyHolder = physHand.side == Side.Left ? lHandVisProxyHolder : rHandVisProxyHolder;
        Quaternion proxyRot = transform.rotation;
        Quaternion twistResidual = physHand.followTgtTrf.rotation * Quaternion.Inverse(proxyRot);
        float initTwistDeg = MathUtils.ExtractSignedTwistAng(twistResidual, transform.up) * Mathf.Rad2Deg;
        // Create rotation around the handle.
        proxyRot = Quaternion.AngleAxis(initTwistDeg, transform.up) * transform.rotation;
        ObjUtils.ActivateNSetPose(
            proxyHolder,
            MathUtils.TrfPtUnscaled(transform, initPhysHandPosInGrblSpc),
            proxyRot
        );
        // TODO: Ugh, here I'm enabling the holder while on release I disable the child visual object.
        // TODO C: This is an easy fix, but is very ugly. Maybe remove the holders entirely and hard code
        // TODO C: the proxy hand pos and rot constraints.
        if (physHand.side == Side.Left)
            lHandVisProxyVis.SetActive(true);
        else
            rHandVisProxyVis.SetActive(true);
        SwitchJntStBasedOnGrbCount();
    }

    void ReleaseAllGrbs() {
        GrblUtils.LRGrab_ReleaseAllGrbs(this, lHandVisProxyHolder, rHandVisProxyHolder);
        SwitchJntStBasedOnGrbCount();
    }

    public void ReleaseGrb(GrblJntDriven_PhysHand physHand) {
        // If lower hand is about to be released.
        if (GrblCore.grbs.Count == 2 && GrblCore.FindGrbIndex(physHand) == LowestHandIndex()) {
            // Choose the opposite side proxy from the hand being released.
            GameObject proxy = physHand.side == Side.Right ? lHandVisProxyHolder : rHandVisProxyHolder;
            ReinitializeGrabFromCurrentProxyPose(proxy);
        }
        GrblUtils.LRGrb_ReleaseGrb(this, physHand, lHandVisProxyVis, rHandVisProxyVis);
        SwitchJntStBasedOnGrbCount();
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    /// <summary>
    /// Finds grab with highest initial grab pos in local Y dir.
    /// </summary>
    /// <returns></returns>
    int HighestHandIndex() {
        Debug.Assert(
            GrblCore.grbs.Count != 0,
            "Tried to find local lowest hand on handle but there was 0 grabs!",
            this
        );
        if (GrblCore.grbs.Count == 1)
            return 0;
        // Check which initial is higher on local Y axis.
        float grb0LocalHght = grblCore.grbs[0].initPhysHandPosInGrblSpc.y;
        float grb1LocalHght = grblCore.grbs[1].initPhysHandPosInGrblSpc.y;
        if (grb0LocalHght > grb1LocalHght)
            return 0;
        return 1;
    }

    /// <summary>
    /// Finds grab with lowest initial grab pos in local Y dir.
    /// </summary>
    /// <returns></returns>
    int LowestHandIndex() {
        Debug.Assert(
            GrblCore.grbs.Count != 0,
            "Tried to find local lowest hand on handle but there was 0 grabs!",
            this
        );
        if(GrblCore.grbs.Count == 1)
            return 0;
        // Check which initial is higher on local Y axis.
        float grb0LocalHght = grblCore.grbs[0].initPhysHandPosInGrblSpc.y;
        float grb1LocalHght = grblCore.grbs[1].initPhysHandPosInGrblSpc.y;
        if (grb0LocalHght < grb1LocalHght)
            return 0;
        return 1;
    }

    void ReinitializeGrabFromCurrentProxyPose(GameObject proxy) {
        Vector3 proxyLocalPos =
            MathUtils.InvrsTrfPtUnscaled(
                transform,
                proxy.transform.position
            );
        Grb grb = GrblCore.grbs[HighestHandIndex()];
        // Jic we clamp init phys hand pos so that it aligns with the handle.
        proxyLocalPos.x = 0;
        proxyLocalPos.y = Mathf.Clamp(proxyLocalPos.y, minGrbPtLclY, maxGrbPtHandLclY);
        proxyLocalPos.z = 0;
        grb.initPhysHandPosInGrblSpc = proxyLocalPos;
        grb.initRotFromGrblToPhysHand =
            MathUtils.InvrsTrfRot(
                transform,
                grb.physHand.followTgtTrf.rotation
            );
    }

    void SwitchJntStBasedOnGrbCount() {
        IFsmSt nextState = grblCore.grbs.Count switch {
            0 => jntSt_NoGrb,
            1 => jntSt_SglGrb_SimpleAnchAtPiv,
            2 => jntSt_DblGrb_GrbLineAligned,
            _ => throw new System.ArgumentOutOfRangeException(nameof(grblCore.grbs.Count))
        };
        if (nextState != grbJntFsm.CurrentState)
            grbJntFsm.SwitchState(nextState, this);
    }

    void UpdateUpperProxyHand() {
        Debug.Assert(GrblCore.grbs.Count == 2, $"Grabs count was not 2! It was: {GrblCore.grbs.Count}", this);
        // Handle axis in world space (+Y of the axe).
        Vector3 handleAxis = transform.up;
        Grb grb = grblCore.grbs[HighestHandIndex()];
        GameObject proxy = grb.physHand.side == Side.Left ? lHandVisProxyHolder : rHandVisProxyHolder;
        // Visual grab position in the axe's local space.
        // TODO: proxy hand should be placed so that it is offset by the grab point. Maybe.
        // TODO C: Think about this when you are less tired.
        Vector3 proxyLocalPos = grb.initPhysHandPosInGrblSpc;
        Vector3 followGrabLocal = GrblUtils.FollowTgtInitGrbPtInGrblSpc(grb, transform);
        // Visually slide along the handle.
        proxyLocalPos.y = Mathf.Min(followGrabLocal.y, maxGrbPtHandLclY);
        Vector3 proxyWorldPos = MathUtils.TrfPtUnscaled(transform, proxyLocalPos);
        // Twist around handle.
        Quaternion proxyRot = transform.rotation;
        Quaternion twistResidual = grb.physHand.followTgtTrf.rotation * Quaternion.Inverse(proxyRot);
        float twistDeg = MathUtils.ExtractSignedTwistAng(twistResidual, handleAxis) * Mathf.Rad2Deg;
        proxyRot = Quaternion.AngleAxis(twistDeg, handleAxis) * proxyRot;
        proxy.transform.SetPositionAndRotation(proxyWorldPos, proxyRot);
    }

    void UpdateProxyHands() {
        lHandVisProxyHolder.SetActive(false);
        rHandVisProxyHolder.SetActive(false);
        if (grblCore.grbs.Count == 0)
            return;
        int lowerHandIndex = LowestHandIndex();
        // Handle axis in world space (+Y of the axe).
        Vector3 handleAxis = transform.up;
        for (int i = 0; i < grblCore.grbs.Count; ++i) {
            Grb grb = grblCore.grbs[i];
            GameObject proxy = grb.physHand.side == Side.Left ? lHandVisProxyHolder : rHandVisProxyHolder;
            proxy.SetActive(true);
            // Visual grab position in the axe's local space.
            Vector3 proxyLocalPos = grb.initPhysHandPosInGrblSpc;
            // Only the upper hand is allowed to slide.
            if (i != lowerHandIndex) {
                // Controller grip point expressed in axe local space.
                Vector3 followTgtInitGrabPtWorld = MathUtils.TrfPtUnscaled(grb.physHand.followTgtTrf, grb.followTgtInitGrabPtInFollowTgtSpc);
                Vector3 followGrabLocal = MathUtils.InvrsTrfPtUnscaled(
                    transform,
                    followTgtInitGrabPtWorld
                );
                // Visually slide along the handle.
                proxyLocalPos.y = Mathf.Min(followGrabLocal.y, maxGrbPtHandLclY);
            }
            Vector3 proxyWorldPos = MathUtils.TrfPtUnscaled(transform, proxyLocalPos);
            Quaternion proxyRot = transform.rotation;
            // Only the upper hand twists.
            if (i != lowerHandIndex) {
                Quaternion twistResidual = grb.physHand.followTgtTrf.rotation * Quaternion.Inverse(proxyRot);
                float twistDeg = MathUtils.ExtractSignedTwistAng(twistResidual, handleAxis) * Mathf.Rad2Deg;
                proxyRot = Quaternion.AngleAxis(twistDeg, handleAxis) * proxyRot;
            }
            proxy.transform.SetPositionAndRotation(proxyWorldPos, proxyRot);
        }
    }
}
