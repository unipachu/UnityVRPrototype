using System.Collections.Generic;
using UnityEngine;

public enum GrblJntDriven_BasicGrblSglGrbJntT {
    AnchAtGrblPiv,
    AnchAtPhysHandPos,
}

public class GrblJntDriven_BasicGrbl : MonoBehaviour, IGrbl {
    [Header("Settings")]
    [SerializeField] GrblJntDriven_BasicGrblSglGrbJntT sglGrbJntT = GrblJntDriven_BasicGrblSglGrbJntT.AnchAtGrblPiv;

    [Header("Other Refs")]
    [SerializeField] ConfigurableJoint grbJnt;
    [SerializeField] Rigidbody rb;
    [Tooltip("Hand visual used to represent grabbing hand. \n" +
        "Set the hand visual inactive in editor!")]
    [SerializeField] GameObject lHandVisProxy;
    [SerializeField] GameObject rHandVisProxy;
    
    [HideInInspector] public Fsm grbJntFsm = new();
    [HideInInspector] public GrblJntDriven_GrblJntSt_NoGrb jntSt_NoGrab;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPhysHandPos jntSt_SglGrb_SimpleAnchAtPhysHandPos;
    [HideInInspector] public GrblJntDriven_GrblJntSt_SglGrb_SimpleAnchAtPiv jntSt_SglGrb_SimpleAnchAtPiv;
    [HideInInspector] public GrblJntDriven_GrblJntSt_MultiGrb_SimpleAnchAtPiv jntSt_MultiGrb_SimpleAnchAtPiv;

    readonly List<Grb> grbs = new(2);

    public GameObject GrblGameObj => gameObject;
    public ConfigurableJoint GrbJnt => grbJnt;
    public List<Grb> Grbs => grbs;
    public Rigidbody Rb => rb;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    private void Awake() {
        // Initialize FSM states.
        jntSt_NoGrab = new(this);
        jntSt_SglGrb_SimpleAnchAtPhysHandPos = new(this);
        jntSt_SglGrb_SimpleAnchAtPiv = new(this);
        jntSt_MultiGrb_SimpleAnchAtPiv = new(this);
    }

    private void Start() {
        grbJntFsm.SwitchState(jntSt_NoGrab, this);
    }

    void FixedUpdate() {
        grbJntFsm.CurrentState.PhysicsTick();
    }

    void OnDrawGizmos() {
        if (grbJnt == null)
            return;
        Gizmos.color = Color.yellow;
        Vector3 worldAnchorPos = grbJnt.transform.TransformPoint(grbJnt.anchor);
        Gizmos.DrawWireSphere(worldAnchorPos, 0.02f);
    }

    void OnDisable() {
        ReleaseAllGrbs();
    }

    // -----------------------------------------
    // PUBLIC METHODS
    // -----------------------------------------

    public bool CanBeGrabbed(GrblJntDriven_PhysHand physHand) {
        // Can be grabbed by up to one left hand and one right hand simultaneously.
        return GrabCount(physHand.side) < 2;
    }

    public bool CanBeReleased(GrblJntDriven_PhysHand physHand) {
        return true;
    }

    /// <summary>
    /// Finds <see cref="Grb"/> by the <see cref="GrblJntDriven_PhysHand"/>.
    /// If <see cref="GrblJntDriven_PhysHand"/> is not grabbing this, returns null.
    /// </summary>
    public Grb FindGrb(GrblJntDriven_PhysHand physHand) {
        for (int i = 0; i < grbs.Count; i++) {
            if (grbs[i].physHand == physHand)
                return grbs[i];
        }
        Debug.LogWarning($"{physHand.name} was not grabbing {gameObject.name}!", this);
        return null;
    }

    public float GetDistToGrbPt(Vector3 physHandWorldGrabPoint) {
        return Vector3.Distance(transform.position, physHandWorldGrabPoint);
    }

    /// <summary>
    /// How many <see cref="GrblJntDriven_PhysHand"/>s of the specified side are grabbing this?
    /// </summary>
    public int GrabCount(Side handSide) {
        int counter = 0;
        foreach (Grb grab in grbs) {
            if(grab.physHand.side == handSide)
                counter++;
        }
        return counter;
    }

    public void InitiateGrb(GrblJntDriven_PhysHand physHand) {
        // NOTE: Phys hand should check if the grabbable can be grabbed before calling this method.
        //if (!CanBeGrabbed(physHand))
        //    return false;
        var newGrb = new Grb(
            physHand, 
            GeneralUtils.UnscaledInvrsTrfPt(transform, physHand.transform.position),
            GeneralUtils.RotFromWorldToTrfSpace(transform, physHand.transform.rotation)
        );
        grbs.Add(newGrb);
        // Setup hand proxy visual.
        if (physHand.side == Side.Left)
            EnableProxyHand(ref lHandVisProxy, physHand);
        else 
            EnableProxyHand(ref rHandVisProxy, physHand);
        SwitchJntStBasedOnGrbCount();
    }

    public void ReleaseGrb(GrblJntDriven_PhysHand physHand) {
        Grb grb = FindGrb(physHand);
        grb.physHand.OnGrabReleased(
            GeneralUtils.UnscaledTrfPt(transform, grb.initPhysHandPosInGrblLocalSpace),
            GeneralUtils.RotFromTrfSpaceToWorld(transform, grb.initRotFromGrblToPhysHand)
        );
        grbs.Remove(grb);
        if(physHand.side == Side.Left)
            lHandVisProxy.SetActive(false);
        else
            rHandVisProxy.SetActive(false);
        SwitchJntStBasedOnGrbCount();
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void ReleaseAllGrbs() {
        foreach (Grb grab in grbs)
            grab.physHand.OnGrabReleased(
                GeneralUtils.UnscaledTrfPt(transform, grab.initPhysHandPosInGrblLocalSpace),
                GeneralUtils.RotFromTrfSpaceToWorld(transform, grab.initRotFromGrblToPhysHand)
            );
        grbs.Clear();
        lHandVisProxy.SetActive(false);
        rHandVisProxy.SetActive(false);
        SwitchJntStBasedOnGrbCount();
    }

    void EnableProxyHand(ref GameObject handVisProxy, GrblJntDriven_PhysHand physHand) {
        handVisProxy.SetActive(true);
        handVisProxy.transform.position = physHand.transform.position;
        handVisProxy.transform.rotation = physHand.transform.rotation;
    }
    
    void SwitchJntStBasedOnGrbCount() {
        IFsmSt nextState = grbs.Count switch {
            0 => jntSt_NoGrab,
            1 => sglGrbJntT switch {
                GrblJntDriven_BasicGrblSglGrbJntT.AnchAtGrblPiv =>
                    jntSt_SglGrb_SimpleAnchAtPiv,
                GrblJntDriven_BasicGrblSglGrbJntT.AnchAtPhysHandPos =>
                    jntSt_SglGrb_SimpleAnchAtPhysHandPos,
                _ => throw new System.ArgumentOutOfRangeException(nameof(sglGrbJntT))
            },
            _ => jntSt_MultiGrb_SimpleAnchAtPiv
        };
        if (nextState != grbJntFsm.CurrentState)
            grbJntFsm.SwitchState(nextState, this);
    }
}
