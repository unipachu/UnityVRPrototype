using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum KeyholeSt {
    LookForSnpls,
    InterpSnplToSnpTgt,
    SnplInSnpTgt,
    InterpSnplFromSnpTgt,
}

enum SnappedSnplSt {
    Outside,
    InsideMiddle,
    InsideEnd,
}

/// <summary>
/// Keyhole snappable target for <see cref="IKeyholeSnpl"/> game objects.
/// </summary>
// TODO: Currently 
public class SnapTgtKeyhole : MonoBehaviour, ISnapTgt {
    [Header("Snappable Search Settings")]
    [SerializeField] Vector3 overlapSphereUnscaledLclPos;
    [Tooltip("Radius of the overlap sphere used for looking keyhole snappables.")]
    [Min(0f)]
    [SerializeField] float overlapSphereR = 0.02f;
    [Tooltip("Layers used by the overlap sphere when searching for keyhole snappables.")]
    [SerializeField] LayerMask grbLayers;
    [Tooltip("Max angle required for a snappable to snap to this snap target, in degrees.")]
    [SerializeField] float requiredMaxAngForSnapping = 10;

    [Header("Snap-In and Snap-Out Interpolation Settings")]
    [Tooltip("Duration for snappable interpolating into and away from snap during " +
        "snap initialization and end.")]
    [SerializeField] float snapInInterpDur = 0.1f;
    // TODO: Do we even need snap out interpolation?
    [SerializeField] float snapOutInterpDur = 0.05f;

    [Header("Rumble Settings")]
    [SerializeField] float snapStartRumbleAmp = 0.1f;
    [SerializeField] float snapStartRumbleDur = 0.1f;
    [SerializeField] float snapStartRumbleFreq = 0.05f;
    [SerializeField] float keyholeEndReachedRumbleAmp = 0.5f;
    [SerializeField] float keyholeEndReachedRumbleDur = 0.05f;
    [SerializeField] float keyholeEndReachedRumbleFreq = 0.05f;
    [SerializeField] float padlockUnlockedRumbleAmp = 0.5f;
    [SerializeField] float padlockUnlockedRumbleDur = 0.05f;
    [SerializeField] float padlockUnlockedRumbleFreq = 0.05f;


    [Header("Snap Out Settings")]
    [Tooltip("Distance from grabbable to theoretical follow target grabbable position required for " +
        "the snapped grabbable to exit the snap. See code for detailed 'snap out' requirements.")]
    [SerializeField] float snapOutDist = 0.15f;
    [Tooltip("Tip is required to fall below this threshold for the snapped grabbable to exit the snap. " +
        "See code for detailed 'snap out' requirements.")]
    [SerializeField] float snapOutDep = -0.1f;

    [Header("Snappable Movement In Keyhole Settings")]
    [SerializeField] float snplTipMinLclDepth = -0.01f;
    [Tooltip("Should correspond with the keyhole edge.")]
    [SerializeField] float snplTipMinRotDisabledLclDepth = 0.007f;
    [SerializeField] float snplTipMaxRotDisabledLclDepth = 0.095f;
    [SerializeField] float snplTipMaxLclDepth = 0.1f;
    [SerializeField] float lowerAbsRollInsertionThreshold = 2;
    [SerializeField] float upperAbsRollInsertionThreshold = 178;
    [SerializeField] float snplLerpLinSpd = 0.4f;
    [SerializeField] float snplLerpAngSpd = 720;

    [Header("Refs")]
    [SerializeField] GameObject shackle;

    IKeyholeSnpl snpl = null;
    // No need to allocate this every physics tick...
    readonly Collider[] overlapResults = new Collider[8];
    KeyholeSt st = KeyholeSt.LookForSnpls;
    SnappedSnplSt snplSt = SnappedSnplSt.Outside;
    float interpTimer = 0;
    bool padlockUnlocked = false;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    private void FixedUpdate() {
        switch (st) {
            case KeyholeSt.LookForSnpls:
                break;
            case KeyholeSt.InterpSnplToSnpTgt:
                interpTimer += Time.fixedDeltaTime;
                // Allow roll input to target rot.
                Quaternion snplWldTgtRot = transform.rotation;
                if (snpl.Grbl.GnrGrbs.GrbCount != 0) {
                    Quaternion theoTipWldRot = TheoFolTgtTipWldRot(snpl.Grbl.GnrGrbs.GetGrb(0));
                    snplWldTgtRot = MathUtils.CalculateRelativeTwist(
                        transform.rotation,
                        theoTipWldRot,
                        transform.forward
                    );
                }
                MathUtils.InterpRbSoChildAlignsWithTgtPose(
                    snpl.Rb,
                    snpl.KeyTipLclPos,
                    // NOTE: We use Quaternion.identity here since we expect rb forward to be
                    // NOTE C: the keyhole insertion direction.
                    Quaternion.identity, 
                    transform.position + (transform.forward * snplTipMinLclDepth),
                    snplWldTgtRot,
                    interpTimer / snapInInterpDur
                );
                if(interpTimer > snapInInterpDur)
                    st = KeyholeSt.SnplInSnpTgt;
                    snplSt = SnappedSnplSt.Outside;
                break;
            case KeyholeSt.SnplInSnpTgt:
                SnplPhysicsTick();
                break;
            case KeyholeSt.InterpSnplFromSnpTgt:
                // TODO: You should probably have a separate interpolate for when the key is grabbed,
                // TODO : _so that it interpolates to the theoretical grabbable pos.
                interpTimer += Time.fixedDeltaTime;
                MathUtils.InterpRbSoChildAlignsWithTgtPose(
                    snpl.Rb,
                    snpl.KeyTipLclPos,
                    // NOTE: We use Quaternion.identity here since we expect rb forward to be
                    // NOTE C: the keyhole insertion direction.
                    Quaternion.identity,
                    transform.position + new Vector3(0, 0, snplTipMinLclDepth),
                    // TODO: If the keyhole is moving, you need to use the keyhole rotation for all except twist.
                    snpl.Rb.rotation,
                    interpTimer / snapOutInterpDur
                );
                if (interpTimer > snapOutInterpDur)
                    // TODO: You should give the snpl rb a vel that matches the previous snpl lerp vel.
                    EndSnp();
                break;
            default:
                Debug.LogError("Switch defaulted", this);
                break;
        }
    }

    private void Update() {
        switch (st) {
            case KeyholeSt.LookForSnpls:
                // We look for snappables in Update to ensure that rigidbodies and transforms are
                // synced in case we need to cache relative poses (I'm not sure if we do).
                LookForKeyholeSnpls();
                break;
            case KeyholeSt.InterpSnplToSnpTgt:
                break;
            case KeyholeSt.SnplInSnpTgt:
                break;
            case KeyholeSt.InterpSnplFromSnpTgt:
                break;
            default:
                break;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.greenYellow;
        Gizmos.DrawWireSphere(MathUtils.TrfPtUnscaled(transform, overlapSphereUnscaledLclPos), overlapSphereR);
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void EndSnp() {
        snpl.OnEndSnp();
        snpl = null;
        st = KeyholeSt.LookForSnpls;
    }

    void LookForKeyholeSnpls() {
        Vector3 worldPos = MathUtils.TrfPtUnscaled(transform, overlapSphereUnscaledLclPos);
        int hitCount = Physics.OverlapSphereNonAlloc(
            worldPos,
            overlapSphereR,
            overlapResults,
            grbLayers,
            QueryTriggerInteraction.Ignore
        );
        for (int i = 0; i < hitCount; i++) {
            Collider col = overlapResults[i];
            if (col.attachedRigidbody == null)
                continue;
            IKeyholeSnpl foundSnpl = col.attachedRigidbody.GetComponent<IKeyholeSnpl>();
            if (foundSnpl == null)
                continue;
            if (!foundSnpl.CanSnp())
                continue;
            // NOTE: We can use rb.transform.forward here because this method is called
            // NOT C: in Update and therefore rigibody and its transform are in sync.
            if (Vector3.Angle(
                transform.forward,
                foundSnpl.Rb.transform.forward) > requiredMaxAngForSnapping
            )
                continue;
            foundSnpl.InitSnp(this);
            snpl = foundSnpl;
            for (int j = 0; j < snpl.Grbl.GnrGrbs.GrbCount; j++)
                snpl.Grbl.GnrGrbs.GetGrb(j).PhysHand.CtrlHapticImpPlr.SendHapticImpulse(
                    snapStartRumbleAmp,
                    snapStartRumbleDur,
                    snapStartRumbleFreq
                );
            interpTimer = 0;
            st = KeyholeSt.InterpSnplToSnpTgt;
            return;
        }
    }

    private void MoveSnappedSnpl() {
        Rigidbody snplRb = snpl.Rb;
        IGrb grb = snpl.Grbl.GnrGrbs.GetGrb(0);
        float tipDepthInKeyholeSpc = TipPosInKeyholeSpc().z;
        // Find theoretical snappable depth and interpolate towards that.
        Vector3 theoTipWldPos = TheoFolTgtTipWldPos(grb);
        Quaternion theoTipWldRot = TheoFolTgtTipWldRot(grb);
        
        float theoTipLclDepth = MathUtils.InvrsTrfPtUnscaled(transform, theoTipWldPos).z;
        float clampedTheoTipLclDepth = Mathf.Clamp(theoTipLclDepth, snplTipMinLclDepth, snplTipMaxLclDepth);
        //Debug.Log("local clamped depth: " + clampedTheoTipLclDepth);
        Vector3 tipWldTgtPos = MathUtils.TrfPt(
            transform.position,
            transform.rotation,
            new Vector3(0f, 0f, clampedTheoTipLclDepth)
        );
        // Calculate snappable position that places the local tip to the target depth.
        Vector3 snplWldTgtPos = MathUtils.AlignLclPtToWldPt(
            tipWldTgtPos,
            transform.rotation,
            snpl.KeyTipLclPos
        );
        // We find rotation that starts with transform.rotation and then twists around transform.forward
        // based on the rotation of the theoretical tip world rotation.
        Quaternion snplWldTgtRot = MathUtils.CalculateRelativeTwist(
            transform.rotation,
            theoTipWldRot,
            transform.forward
        );

        //snplRb.Move(snplWldTgtPos, snplWldTgtRot);



        float snplRollInKeyholeSpcRad = MathUtils.ExtractSignedTwistAng(
            transform.rotation,        
            snplRb.rotation,
            transform.forward
        );
        Vector3 tipLclTgtPos = MathUtils.InvrsTrfPtUnscaled(transform, tipWldTgtPos);
        float tipLclTgtDepth = tipLclTgtPos.z;
        //// If key is outside the hole:
        //if (tipDepthInKeyholeSpc <= snplTipMinRotDisabledLclDepth) {

        //} else if (tipDepthInKeyholeSpc > snplTipMinRotDisabledLclDepth && tipDepthInKeyholeSpc < snplTipMaxRotDisabledLclDepth) {
        //    snplWldTgtRot = snplRb.rotation;
        //} else {
        //    if (absSnplRollInKeyholeSpcDeg > 5 && absSnplRollInKeyholeSpcDeg < 175) {
        //        snplLclTgtDepth = Mathf.Clamp(tipDepthInKeyholeSpc, snplTipMaxRotDisabledLclDepth, snplTipMaxLclDepth);
        //    }
        //    else {
        //        snplLclTgtDepth = Mathf.Min(tipDepthInKeyholeSpc, snplTipMaxLclDepth);
        //    }
        //}
        //snplLclTgtPos = new Vector3(0, 0, snplLclTgtDepth);
        //snplWldTgtPos = MathUtils.TrfPtUnscaled(transform, snplLclTgtPos);



        float absSnplRollInKeyholeSpcDeg = Mathf.Abs(snplRollInKeyholeSpcRad * Mathf.Rad2Deg);
        bool linearMovAllowed =
            absSnplRollInKeyholeSpcDeg < lowerAbsRollInsertionThreshold ||
            absSnplRollInKeyholeSpcDeg > upperAbsRollInsertionThreshold;
        //Debug.Log("snplSt: " + snplSt.ToString());
        switch (snplSt) {
            case SnappedSnplSt.Outside:
                tipLclTgtDepth = Mathf.Max(tipLclTgtDepth, snplTipMinLclDepth);
                if (
                    // TODO: Using tip tgt depth is unstable because we interpolate to it and so
                    // TODO C: it can be quite far from actualy tip. But using actual tip depth does not
                    // TODO C: work either (try it if you don't believe me).
                    tipLclTgtDepth > snplTipMinRotDisabledLclDepth &&
                    linearMovAllowed
                )
                    snplSt = SnappedSnplSt.InsideMiddle;
                else
                    tipLclTgtDepth = Mathf.Min(tipLclTgtDepth, snplTipMinRotDisabledLclDepth);
                break;
            case SnappedSnplSt.InsideMiddle:
                snplWldTgtRot = snplRb.rotation;
                if (tipLclTgtDepth < snplTipMinRotDisabledLclDepth)
                    snplSt = SnappedSnplSt.Outside;
                else if (tipLclTgtDepth > snplTipMaxRotDisabledLclDepth) {
                    snplSt = SnappedSnplSt.InsideEnd;
                    grb.PhysHand.CtrlHapticImpPlr.SendHapticImpulse(
                        keyholeEndReachedRumbleAmp,
                        keyholeEndReachedRumbleDur,
                        keyholeEndReachedRumbleFreq
                    );
                }
                break;
            case SnappedSnplSt.InsideEnd:
                tipLclTgtDepth = Mathf.Min(tipLclTgtDepth, snplTipMaxLclDepth);
                if (
                    tipLclTgtDepth < snplTipMaxRotDisabledLclDepth &&
                    linearMovAllowed
                )
                    snplSt = SnappedSnplSt.InsideMiddle;
                else
                    tipLclTgtDepth = Mathf.Max(tipLclTgtDepth, snplTipMaxRotDisabledLclDepth);
                // If rotated key at keyhole end, unlock padlock.
                if (absSnplRollInKeyholeSpcDeg > 85 && absSnplRollInKeyholeSpcDeg < 95 && !padlockUnlocked) {
                    padlockUnlocked = true;
                    StartCoroutine(UnlockShackleAnim());
                    grb.PhysHand.CtrlHapticImpPlr.SendHapticImpulse(
                        padlockUnlockedRumbleAmp,
                        padlockUnlockedRumbleDur,
                        padlockUnlockedRumbleFreq
                    );
                }
                break;
            default:
                Debug.LogError("Switch defaulted", this);
                break;
        }
        tipWldTgtPos = MathUtils.TrfPtUnscaled(transform, new Vector3(0, 0, tipLclTgtDepth));
        snplWldTgtPos = MathUtils.AlignLclPtToWldPt(
            tipWldTgtPos,
            snplWldTgtRot,
            snpl.KeyTipLclPos);



        //snplWldTgtPos = Vector3.Lerp(snplRb.position, snplWldTgtPos, snplLerpLinSpd * Time.fixedDeltaTime);
        // TODO: Only lerp twist around keyhole insertion axis so that the keyhole can move while snapped.
        //snplWldTgtRot = Quaternion.Slerp(snplRb.rotation, snplWldTgtRot, snplLerpAngSpd * Time.fixedDeltaTime);

        snplWldTgtPos = Vector3.MoveTowards(
            snplRb.position,
            snplWldTgtPos,
            snplLerpLinSpd * Time.fixedDeltaTime
        );
        // TODO: Only have speed around keyhole insertion axis so that the keyhole can move while snapped.
        snplWldTgtRot = Quaternion.RotateTowards(
            snplRb.rotation,
            snplWldTgtRot,
            snplLerpAngSpd * Time.fixedDeltaTime
        );

        snplRb.Move(snplWldTgtPos, snplWldTgtRot);
    }

    void SnplPhysicsTick() {
        int grbCount = snpl.Grbl.GnrGrbs.GrbCount;

        //if (snplGrbs.Count != 0) {
        //    Debug.Log("depth:" + MathUtils.InvrsTrfPtUnscaled(transform, TheoFolTgtTipWldPos(snplGrbs[0])).z);
        //    Debug.Log("dist bet:" + GrblUtils.DistBetweenGrblRbPosNTheoFolTgtGrblPos(snpl.GrblCore.rb, snplGrbs[0]));

        //}

        if (
            snplSt == SnappedSnplSt.Outside &&
            (snpl.Grbl.GnrGrbs.GrbCount == 0 || (
                GrblUtils.DistBetweenGrblRbPosNTheoFolTgtGrblPos(snpl.Rb, snpl.Grbl.GnrGrbs.GetGrb(0)) > snapOutDist ||
                // Key should be more easily pulled away from snap if pulled away from key insertion direction.
                MathUtils.InvrsTrfPtUnscaled(transform, TheoFolTgtTipWldPos(snpl.Grbl.GnrGrbs.GetGrb(0))).z < snapOutDep)
            )
        ) {
            interpTimer = 0;
            st = KeyholeSt.InterpSnplFromSnpTgt;
            return;
        }
        if(snpl.Grbl.GnrGrbs.GrbCount != 0)
            MoveSnappedSnpl();
    }

    // TODO: You should probably just cache tip theoretical world pos and keyhole space pos every update
    // TODO C: when a snappable is snapped to the keyhole.
    Vector3 TheoFolTgtTipWldPos(IGrb grb) {
        var theoPose = GrblUtils.TheoFolTgtGrblPose(grb);
        return MathUtils.TrfPt(theoPose.Item1, theoPose.Item2, snpl.KeyTipLclPos);
    }

    Quaternion TheoFolTgtTipWldRot(IGrb grb) {
        // Theoretical tip world rotation is same as the grabbable theoretical world rotation...
        return GrblUtils.TheoFolTgtGrblRot(grb);
    }

    Vector3 TipPosInKeyholeSpc() {
        Vector3 tipWldPos = MathUtils.TrfPtUnscaled(snpl.Rb, snpl.KeyTipLclPos);
        return MathUtils.InvrsTrfPtUnscaled(transform, tipWldPos);
    }

    IEnumerator UnlockShackleAnim() {
        Vector3 startPos = shackle.transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 0.05f;
        float duration = 0.1f;
        float timer = 0f;
        while (timer < duration) {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            shackle.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        shackle.transform.localPosition = endPos;
    }
}
