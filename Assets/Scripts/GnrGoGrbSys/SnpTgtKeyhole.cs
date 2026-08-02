using System.Collections;
using UnityEngine;
using UnityEngine.XR.OpenXR.NativeTypes;



/// <summary>
/// Keyhole snappable target for <see cref="IKeyholeSnpl"/> game objects.
/// </summary>
public class SnpTgtKeyhole : MonoBehaviour, ISnpTgt {
    [Header("Snappable Search Settings")]
    [SerializeField] Vector3 overlapSphereUnscaledLclPos;
    [Tooltip("Radius of the overlap sphere used for looking keyhole snappables.")]
    [Min(0f)]
    [SerializeField] float overlapSphereR = 0.02f;
    [Tooltip("Layers used by the overlap sphere when searching for keyhole snappables.")]
    [SerializeField] LayerMask grbLayers;
    [Tooltip("Max angle required for a snappable to snap to this snap target, in degrees.")]
    [SerializeField] float requiredMaxAngForSnapping = 15;

    [Header("Snap-In and Snap-Out Interpolation Settings")]
    [Tooltip("Duration for snappable interpolating into and away from snap during " +
        "snap initialization and end.")]
    [SerializeField] float snpInInterpDur = 0.1f;
    [SerializeField] float snpOutInterpDur = 0.05f;

    [Header("Controller Rumble Settings")]
    [SerializeField] float snpStartRumbleAmp = 0.1f;
    [SerializeField] float snpStartRumbleDur = 0.1f;
    [SerializeField] float snpStartRumbleFreq = 0.05f;
    [SerializeField] float keyholeEndReachedRumbleAmp = 0.5f;
    [SerializeField] float keyholeEndReachedRumbleDur = 0.05f;
    [SerializeField] float keyholeEndReachedRumbleFreq = 0.05f;
    [SerializeField] float padlockUnlockedRumbleAmp = 0.5f;
    [SerializeField] float padlockUnlockedRumbleDur = 0.05f;
    [SerializeField] float padlockUnlockedRumbleFreq = 0.05f;

    [Header("Snap Out Settings")]
    [Tooltip("Distance from grabbable to theoretical follow target grabbable position required for " +
        "the snapped grabbable to exit the snap. See code for detailed 'snap out' requirements.")]
    [SerializeField] float snpOutDist = 0.15f;
    [Tooltip("Tip is required to fall below this threshold for the snapped grabbable to exit the snap. " +
        "See code for detailed 'snap out' requirements.")]
    [SerializeField] float snpOutDep = -0.1f;

    [Header("Snappable Movement In Keyhole Settings")]
    [SerializeField] float snplTipMinLclDepth = -0.01f;
    [Tooltip("Should correspond with depth where the key collides with the keyhole exterior.")]
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
    // NOTE: No need to allocate this every physics tick. Just make sure to use correct collision layers
    // NOTE C: so that the overlap sphere results can be contained in this.
    readonly Collider[] snplSearchResults = new Collider[16];
    SnpTgtSt st = SnpTgtSt.LookForSnpls;
    Keyhole_SnappedSnplSt snplSt = Keyhole_SnappedSnplSt.Outside;
    float interpTimer = 0;
    bool padlockUnlocked = false;

    // Snappable data updated every fixed update:
    Quaternion theoTipWldRot = Quaternion.identity;
    Quaternion snplWldTgtRot = Quaternion.identity;
    IGnrGrbData grb = null;
    Vector3 theoTipWldPos = Vector3.zero;
    float theoTipLclDepth;
    float clampedTheoTipLclDepth;
    Vector3 tipWldTgtPos = Vector3.zero;
    Vector3 snplWldTgtPos = Vector3.zero;
    float snplRollInKeyholeSpcRad;
    Vector3 tipLclTgtPos = Vector3.zero;
    float tipLclTgtDepth;
    float absSnplRollInKeyholeSpcDeg;
    bool linearMovAllowed;
    Vector3 theoFolTgtTipWldPos = Vector3.zero;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    void Awake() {
        Debug.Assert(
            snplTipMinLclDepth < snplTipMinRotDisabledLclDepth &&
            snplTipMinRotDisabledLclDepth < snplTipMaxRotDisabledLclDepth &&
            snplTipMaxRotDisabledLclDepth < snplTipMaxLclDepth,
            "Snap target depth values must satisfy: snplTipMinLclDepth " +
                "< snplTipMinRotDisabledLclDepth < snplTipMaxRotDisabledLclDepth < snplTipMaxLclDepth.",
            this
        );
    }

    void FixedUpdate() {
        // TODO: Maybe implement a generic FSM later for cleaner code.
        switch (st) {
            case SnpTgtSt.LookForSnpls:
                break;
            case SnpTgtSt.SnpInInterp:
                UpdateSnplData();
                interpTimer += Time.fixedDeltaTime;
                MathUtils.InterpRbSoChildAlignsWithTgtPose(
                    snpl.GnrGrblData.Rb,
                    snpl.KeyTipLclPos,
                    // NOTE: We use Quaternion.identity here since we expect rb forward to be
                    // NOTE C: the keyhole insertion direction.
                    Quaternion.identity,
                    // We offset the tip target by snplTipMinLclDepth.
                    transform.position + transform.forward * snplTipMinLclDepth,
                    // Allow roll input during snap-in interpolation.
                    snplWldTgtRot,
                    interpTimer / snpInInterpDur
                );
                if(interpTimer > snpInInterpDur)
                    st = SnpTgtSt.Snapped;
                    snplSt = Keyhole_SnappedSnplSt.Outside;
                break;
            case SnpTgtSt.Snapped:
                UpdateSnplData();
                FixedUpdate_Snapped();
                break;
            case SnpTgtSt.SnpOutInterp:
                UpdateSnplData();
                // TODO: You should probably have a separate interpolate for when the key is grabbed,
                // TODO : so that it interpolates to the theoretical grabbable pos.
                interpTimer += Time.fixedDeltaTime;
                MathUtils.InterpRbSoChildAlignsWithTgtPose(
                    snpl.GnrGrblData.Rb,
                    snpl.KeyTipLclPos,
                    Quaternion.identity,
                    transform.position + transform.forward * snplTipMinLclDepth,
                    // TODO: This will not work if the keyhole itself is rotating.
                    snpl.GnrGrblData.Rb.rotation,
                    interpTimer / snpOutInterpDur
                );
                if (interpTimer > snpOutInterpDur)
                    // TODO: You should give the snpl rb a vel that matches the previous snpl lerp vel.
                    EndSnp();
                break;
            default:
                Debug.LogError("Switch defaulted", this);
                break;
        }
    }

    void Update() {
        switch (st) {
            case SnpTgtSt.LookForSnpls:
                // We look for snappables in Update to ensure that rigidbodies and transforms are
                // synced in case we need to cache relative poses (I'm not sure if we do).
                SearchForKeyholeSnpls();
                break;
            case SnpTgtSt.SnpInInterp:
                break;
            case SnpTgtSt.Snapped:
                break;
            case SnpTgtSt.SnpOutInterp:
                break;
            default:
                Debug.LogError("Switch defaulted", this);
                break;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.greenYellow;
        // Draw snappable search sphere.
        Gizmos.DrawWireSphere(MathUtils.TrfPtUnscaled(transform, overlapSphereUnscaledLclPos), overlapSphereR);
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    /// <summary>
    /// Called when this decideds to end the snap and free the snappable.
    /// </summary>
    void EndSnp() {
        snpl.OnEndSnp();
        snpl = null;
        st = SnpTgtSt.LookForSnpls;
    }

    /// <summary>
    /// Searches for keyhole snappables and if finds one, initializes snap state.
    /// </summary>
    void SearchForKeyholeSnpls() {
        int hitCount = Physics.OverlapSphereNonAlloc(
            MathUtils.TrfPtUnscaled(transform, overlapSphereUnscaledLclPos),
            overlapSphereR,
            snplSearchResults,
            grbLayers,
            QueryTriggerInteraction.Ignore
        );
        for (int i = 0; i < hitCount; i++) {
            Collider col = snplSearchResults[i];
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
                foundSnpl.GnrGrblData.Rb.transform.forward) > requiredMaxAngForSnapping
            )
                continue;
            foundSnpl.InitSnp(this);
            snpl = foundSnpl;
            for (int j = 0; j < snpl.GnrGrblData.GnrGrbs.GrbCount; j++)
                snpl.GnrGrblData.GnrGrbs.GetGrb(j).GnrGrbData.gnrPhysHand.CtrlHapticImpPlr.SendHapticImpulse(
                    snpStartRumbleAmp,
                    snpStartRumbleDur,
                    snpStartRumbleFreq
                );
            interpTimer = 0;
            st = SnpTgtSt.SnpInInterp;
            return;
        }
    }

    private void UpdateSnappedSnpl() {
        switch (snplSt) {
            case Keyhole_SnappedSnplSt.Outside:
                    // TODO: Using tip tgt depth is unstable because we interpolate to it and so
                    // TODO C: it can be quite far from actualy tip. Use should probably use the actual current 
                    // TODO C: tip depth and roll (in keyhole space).
                if (tipLclTgtDepth > snplTipMinRotDisabledLclDepth && linearMovAllowed)
                    snplSt = Keyhole_SnappedSnplSt.InsideMiddle;
                else
                    tipLclTgtDepth = Mathf.Min(tipLclTgtDepth, snplTipMinRotDisabledLclDepth);
                break;
            case Keyhole_SnappedSnplSt.InsideMiddle:
                // TODO: Snap roll to local 0.
                snplWldTgtRot = snpl.GnrGrblData.Rb.rotation;
                if (tipLclTgtDepth < snplTipMinRotDisabledLclDepth)
                    snplSt = Keyhole_SnappedSnplSt.Outside;
                else if (tipLclTgtDepth > snplTipMaxRotDisabledLclDepth) {
                    snplSt = Keyhole_SnappedSnplSt.InsideEnd;
                    grb.GnrGrbData.gnrPhysHand.CtrlHapticImpPlr.SendHapticImpulse(
                        keyholeEndReachedRumbleAmp,
                        keyholeEndReachedRumbleDur,
                        keyholeEndReachedRumbleFreq
                    );
                }
                break;
            case Keyhole_SnappedSnplSt.InsideEnd:
                if (tipLclTgtDepth < snplTipMaxRotDisabledLclDepth && linearMovAllowed)
                    snplSt = Keyhole_SnappedSnplSt.InsideMiddle;
                else
                    tipLclTgtDepth = Mathf.Max(tipLclTgtDepth, snplTipMaxRotDisabledLclDepth);
                TryUnlockPadlock();
                break;
            default:
                Debug.LogError("Switch defaulted", this);
                break;
        }
        // Clamp tip depth target to the min and max depth.
        Mathf.Clamp(tipLclTgtDepth, snplTipMinLclDepth, snplTipMaxLclDepth);
        tipWldTgtPos = MathUtils.TrfPtUnscaled(transform, new Vector3(0, 0, tipLclTgtDepth));
        snplWldTgtPos = MathUtils.AlignLclPtToWldPt(
            tipWldTgtPos,
            snplWldTgtRot,
            snpl.KeyTipLclPos);
        // TODO: Only have speed in the keyhole insertion axis so that the keyhole can move while
        // TODO C: controlling snappable.
        snplWldTgtPos = Vector3.MoveTowards(
            snpl.GnrGrblData.Rb.position,
            snplWldTgtPos,
            snplLerpLinSpd * Time.fixedDeltaTime
        );
        // TODO: Only have speed around keyhole insertion axis so that the keyhole can move while
        // TODO C: controlling snappable.
        snplWldTgtRot = Quaternion.RotateTowards(
            snpl.GnrGrblData.Rb.rotation,
            snplWldTgtRot,
            snplLerpAngSpd * Time.fixedDeltaTime
        );
        snpl.GnrGrblData.Rb.Move(snplWldTgtPos, snplWldTgtRot);
    }

    void FixedUpdate_Snapped() {
        // Check if the snappable should snap out.
        if (ShouldSnplSnpOut()) {
            interpTimer = 0;
            st = SnpTgtSt.SnpOutInterp;
            return;
        }
        if (snpl.GnrGrblData.GnrGrbs.GrbCount != 0)
            UpdateSnappedSnpl();
    }

    private bool ShouldSnplSnpOut() {
        return snplSt == Keyhole_SnappedSnplSt.Outside &&
            (snpl.GnrGrblData.GnrGrbs.GrbCount == 0 || (
                GrblUtils.DistBetweenGrblRbPosNTheoFolTgtGrblPos(
                    snpl.GnrGrblData.Rb,
                    snpl.GnrGrblData.GnrGrbs.GetGrb(0)
                ) > snpOutDist ||
                // Key should be more easily pulled away from snap if pulled away from key insertion direction.
                MathUtils.InvrsTrfPtUnscaled(transform, theoFolTgtTipWldPos).z < snpOutDep)
            );
    }

    /// <summary>
    /// Updates the padlock lock system.
    /// </summary>
    void TryUnlockPadlock() {
        // If rotated key at keyhole end, unlock padlock.
        // TODO: I believe if the key enters the end of the keyhole when target rotation would rotate key
        // TODO C: over the padlock unlock range, the key can rotate over the unlock range and thus
        // TODO C: the padlock does not unlock even though it feels like it should.
        if (!padlockUnlocked && absSnplRollInKeyholeSpcDeg > 85 && absSnplRollInKeyholeSpcDeg < 95) {
            padlockUnlocked = true;
            StartCoroutine(UnlockShackleAnim());
            // Play a little haptic rumble when padlock unlocks.
            if (grb != null)
                grb.GnrGrbData.gnrPhysHand.CtrlHapticImpPlr.SendHapticImpulse(
                    padlockUnlockedRumbleAmp,
                    padlockUnlockedRumbleDur,
                    padlockUnlockedRumbleFreq
                );
        }
    }

    /// <summary>
    /// Opens the padlock shackle, i.e. moves it up.<br/>
    /// </summary>
    // TODO: There is no separate kinematic rb for the shackle currently so instead of moving it
    // TODO C: correctly with rb.Move, it is moved by changing its transform. Create a kinematic
    // TODO C: rb for it and move that instead.
    IEnumerator UnlockShackleAnim() {
        // TODO: You could cache these...
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

    /// <summary>
    /// Update data needed for snappable movement.
    /// </summary>
    void UpdateSnplData() {
        //snplWldTgtRot = transform.rotation;
        snplWldTgtRot = MathUtils.CalculateRelativeTwist(
            transform.rotation,
            theoTipWldRot,
            transform.forward
        );
        // If there's a snapped grabbed snappable:
        if (snpl != null && snpl.GnrGrblData.GnrGrbs.GrbCount != 0) {
            grb = snpl.GnrGrblData.GnrGrbs.GetGrb(0);
            theoTipWldRot = GrblUtils.TheoFolTgtGrblRot(snpl.GnrGrblData.GnrGrbs.GetGrb(0));
            theoFolTgtTipWldPos = MathUtils.TrfPt(
                GrblUtils.TheoFolTgtGrblPose(grb).Item1,
                GrblUtils.TheoFolTgtGrblPose(grb).Item2,
                snpl.KeyTipLclPos
            );
            // Find theoretical snappable depth and interpolate towards that.
            theoTipWldPos = theoFolTgtTipWldPos;
            theoTipLclDepth = MathUtils.InvrsTrfPtUnscaled(transform, theoTipWldPos).z;
            clampedTheoTipLclDepth = Mathf.Clamp(theoTipLclDepth, snplTipMinLclDepth, snplTipMaxLclDepth);
            tipWldTgtPos = MathUtils.TrfPt(
                transform.position,
                transform.rotation,
                new Vector3(0f, 0f, clampedTheoTipLclDepth)
            );
            // Calculate snappable position that places the local tip to the target depth.
            snplWldTgtPos = MathUtils.AlignLclPtToWldPt(
                tipWldTgtPos,
                transform.rotation,
                snpl.KeyTipLclPos
            );
            // We find rotation that starts with transform.rotation and then twists around transform.forward
            // based on the rotation of the theoretical tip world rotation.
            snplRollInKeyholeSpcRad = MathUtils.ExtractSignedTwistAng(
                transform.rotation,
                snpl.GnrGrblData.Rb.rotation,
                transform.forward
            );
            tipLclTgtPos = MathUtils.InvrsTrfPtUnscaled(transform, tipWldTgtPos);
            tipLclTgtDepth = tipLclTgtPos.z;
            absSnplRollInKeyholeSpcDeg = Mathf.Abs(snplRollInKeyholeSpcRad * Mathf.Rad2Deg);
            linearMovAllowed =
                absSnplRollInKeyholeSpcDeg < lowerAbsRollInsertionThreshold ||
                absSnplRollInKeyholeSpcDeg > upperAbsRollInsertionThreshold;
        }
    }
}
