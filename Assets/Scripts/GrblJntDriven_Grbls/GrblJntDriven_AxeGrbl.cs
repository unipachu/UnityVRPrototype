using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Axe grabbable.
/// </summary>
public class GrblJntDriven_AxeGrbl : MonoBehaviour, IGnrGrblData, IGrblJntDriven_Grbl, IDblGrb_GrbLineAlignable {
    [Header("Settings")]
    [SerializeField] float minGrbPtLclY = -0.49f;
    [SerializeField] float maxGrbPtHandLclY = -0.03f;

    [Header("Refs")]
    //[SerializeField] GrblJntDriven_GrblCore grblCore;
    [Tooltip("Hand visual used to represent grabbing left hand.\n" +
    "Set the hand visual inactive in editor!")]
    // TODO: You could probably do without the proxy holders...
    [SerializeField] GameObject lHandVisProxyHolder;
    [SerializeField] GameObject lHandVisProxyVis;
    [Tooltip("Hand visual used to represent grabbing right hand.\n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject rHandVisProxyHolder;
    [SerializeField] GameObject rHandVisProxyVis;
    [SerializeField] ConfigurableJoint grbJnt;
    [SerializeField] Rigidbody rb;

    [HideInInspector] public Fsm grbJntFsm = new();
    [HideInInspector] public GrblJntDriven_GrblJntSt_DblGrb_GrbLineAligned jntSt_DblGrb_GrbLineAligned;
    [HideInInspector] public GrblJntDriven_GrblJntSt_NoGrb jntSt_NoGrb;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv jntSt_SglGrb_SimpleAnchAtPiv;

    readonly List<GrblJntDriven_Grb> grbs = new(2);
    IGrblJntDriven_Grbs gnrGrbs;
    readonly Vector3 followTgtInitGrabPtInFollowTgtSpc = new Vector3(0, -0.025f, 0);

    public IGnrGrbsCtrl GnrGrbs => gnrGrbs;
    public List<GrblJntDriven_Grb> Grbs => grbs;
    public ConfigurableJoint GrbJnt => grbJnt;
    public Rigidbody Rb => rb;
    public Transform Trf => transform;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Awake() {
        // Initialize FSM states.
        jntSt_DblGrb_GrbLineAligned = new(this, this);
        jntSt_NoGrb = new(this);
        jntSt_SglGrb_SimpleAnchAtPiv = new(this);
        gnrGrbs = new(grbs);
    }

    void Start() {
        grbJntFsm.SwitchState(jntSt_NoGrb, this);
    }

    void FixedUpdate() {
        grbJntFsm.CurSt.PhysicsTick();
    }

    void Update() {
        if (grbs.Count == 2)
            UpdateUpperProxyHand();
    }

    void OnDrawGizmos() {
        ObjUtils.OnDrawGizmos_DrawJntAnch(grbJnt);
    }

    void OnDisable() {
        ReleaseAllGrbs();
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    /// <summary>
    /// NOTE: This grbl can be grabbed by up to one left hand and one right hand simultaneously.
    /// </summary>
    public bool CanBeGrabbed(GrblJntDriven_PhysHand physHand)
        => GrblUtils.SidedGrbCount<GrblJntDriven_Grb, GrblJntDriven_PhysHand>(
            grbs,
            physHand.handSide) == 0;

    public bool CanBeReleased(GrblJntDriven_PhysHand physHand) => true;

    public float GetDistToGrbPt(Vector3 physHandWorldGrbPt)
        => Vector3.Distance(transform.position, physHandWorldGrbPt);

    public float GetPosHand0Wt() => 1 - LowestHandIndex();
    
    public float GetRotHand0Wt() => 1 - LowestHandIndex();

    public void InitiateGrb(GrblJntDriven_PhysHand physHand) {
        Vector3 initPhysHandPosInGrblSpc = MathUtils.InvrsTrfPtUnscaled(transform, physHand.transform.position);
        // We clamp init phys hand pos so that it aligns with the handle.
        initPhysHandPosInGrblSpc.x = 0;
        initPhysHandPosInGrblSpc.y = Mathf.Clamp(initPhysHandPosInGrblSpc.y, minGrbPtLclY, maxGrbPtHandLclY);
        initPhysHandPosInGrblSpc.z = 0;
        var newGrb = new GrblJntDriven_Grb(
            physHand,
            new GnrGrbData(
                physHand,
                initPhysHandPosInGrblSpc,
                MathUtils.InvrsTrfRot(transform, physHand.transform.rotation),
                followTgtInitGrabPtInFollowTgtSpc
            )
        );
        grbs.Add(newGrb);
        // Setup hand proxy visual.
        GameObject proxyHolder = physHand.handSide == Side.Left ? lHandVisProxyHolder : rHandVisProxyHolder;
        // NOTE: Currently proxy holder is already aligned with the handle (transform.up) so this works.
        Quaternion axeRotWithTwistAroundHandle = MathUtils.CalculateRelativeTwist(
            transform.rotation,
            physHand.followTgtTrf.rotation,
            transform.up
        );
        ObjUtils.ActivateNSetPose(
            proxyHolder,
            MathUtils.TrfPtUnscaled(transform, initPhysHandPosInGrblSpc),
            axeRotWithTwistAroundHandle
        );
        // TODO: Ugh, here I'm enabling the holder while on release I disable the child visual object.
        // TODO C: This is an easy fix, but is very ugly. Maybe remove the holders entirely and hard code
        // TODO C: the proxy hand pos and rot constraints.
        if (physHand.handSide == Side.Left)
            lHandVisProxyVis.SetActive(true);
        else
            rHandVisProxyVis.SetActive(true);
        SwitchJntStBasedOnGrbCount();
    }

    void ReleaseAllGrbs() {
        GrblUtils.LRGrb_ReleaseAllGrbs(this, lHandVisProxyHolder, rHandVisProxyHolder);
        SwitchJntStBasedOnGrbCount();
    }

    public void ReleaseGrb(GrblJntDriven_PhysHand physHand) {
        // If lower hand is about to be released.
        if (
            grbs.Count == 2 &&
            GrblUtils.FindGrbI(grbs, physHand, this) == LowestHandIndex()
        ) {
            // Choose the opposite side proxy from the hand being released.
            GameObject proxy = physHand.handSide == Side.Right ? lHandVisProxyHolder : rHandVisProxyHolder;
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
            grbs.Count != 0,
            "Tried to find local lowest hand on handle but there was 0 grabs!",
            this
        );
        if (grbs.Count == 1)
            return 0;
        // Check which initial is higher on local Y axis.
        float grb0LocalHght = grbs[0].gnrGrb.initPhysHandPosInGrblSpc.y;
        float grb1LocalHght = grbs[1].gnrGrb.initPhysHandPosInGrblSpc.y;
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
            grbs.Count != 0,
            "Tried to find local lowest hand on handle but there was 0 grabs!",
            this
        );
        if(grbs.Count == 1)
            return 0;
        // Check which initial is higher on local Y axis.
        float grb0LocalHght = grbs[0].gnrGrb.initPhysHandPosInGrblSpc.y;
        float grb1LocalHght = grbs[1].gnrGrb.initPhysHandPosInGrblSpc.y;
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
        GrblJntDriven_Grb grb = grbs[HighestHandIndex()];
        // Jic we clamp init phys hand pos so that it aligns with the handle.
        proxyLocalPos.x = 0;
        proxyLocalPos.y = Mathf.Clamp(proxyLocalPos.y, minGrbPtLclY, maxGrbPtHandLclY);
        proxyLocalPos.z = 0;
        grb.gnrGrb.initPhysHandPosInGrblSpc = proxyLocalPos;
        grb.gnrGrb.initRotFromGrblToPhysHand =
            MathUtils.InvrsTrfRot(
                transform,
                grb.physHand.followTgtTrf.rotation
            );
    }

    void SwitchJntStBasedOnGrbCount() {
        IFsmSt nextState = grbs.Count switch {
            0 => jntSt_NoGrb,
            1 => jntSt_SglGrb_SimpleAnchAtPiv,
            2 => jntSt_DblGrb_GrbLineAligned,
            _ => throw new System.ArgumentOutOfRangeException(nameof(grbs.Count))
        };
        if (nextState != grbJntFsm.CurSt)
            grbJntFsm.SwitchState(nextState, this);
    }

    void UpdateUpperProxyHand() {
        Debug.Assert(grbs.Count == 2, $"Grabs count was not 2! It was: {grbs.Count}", this);
        // Handle axis in world space (+Y of the axe).
        Vector3 handleAxis = transform.up;
        GrblJntDriven_Grb grb = grbs[HighestHandIndex()];
        GameObject proxy = grb.physHand.handSide == Side.Left ? lHandVisProxyHolder : rHandVisProxyHolder;
        // Visual grab position in the axe's local space.
        // TODO: proxy hand should be placed so that it is offset by the grab point. Maybe.
        // TODO C: Think about this when you are less tired.
        Vector3 proxyLocalPos = grb.gnrGrb.initPhysHandPosInGrblSpc;
        Vector3 followGrabLocal = GrblUtils.FolTgtInitGrbPtInGrblSpc(grb.gnrGrb, transform);
        // Visually slide along the handle.
        proxyLocalPos.y = Mathf.Min(followGrabLocal.y, maxGrbPtHandLclY);
        Vector3 proxyWorldPos = MathUtils.TrfPtUnscaled(transform, proxyLocalPos);
        // Twist around handle.
        float twistDeg = MathUtils.ExtractSignedTwistAng(
            transform.rotation,
            grb.physHand.followTgtTrf.rotation,
            handleAxis
        ) * Mathf.Rad2Deg;
        Quaternion proxyRot = Quaternion.AngleAxis(twistDeg, handleAxis) * transform.rotation;
        proxy.transform.SetPositionAndRotation(proxyWorldPos, proxyRot);
    }
}
