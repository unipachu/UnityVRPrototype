using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEngine;

enum KeyholeState {
    LookForSnpls,
    InterpSnplToSnpTgt,
    SnplInSnpTgt,
    InterpSnplFromSnpTgt,
}

public class SnapTgtKeyhole : MonoBehaviour, ISnapTgt {
    [Header("Snappable Search Settings")]
    [SerializeField] Vector3 overlapSphereUnscaledLclPos;
    [Tooltip("Radius of the overlap sphere used for looking keyhole snappables.")]
    [Min(0f)]
    [SerializeField] float overlapSphereR = 0.1f;
    [Tooltip("Layers used by the overlap sphere when searching for keyhole snappables.")]
    [SerializeField] LayerMask grbLayers;
    [Tooltip("Max angle required for a snappable to snap to this snap target, in degrees.")]
    [SerializeField] float requiredMaxAngForSnapping = 10;

    [Header("Interpolation Settings")]
    [Tooltip("Duration for snappable interpolating into and away from snap during " +
        "snap initialization and end.")]
    [SerializeField] float interpDur = 0.1f;

    [Header("Rumble Settings")]
    [SerializeField] float snapStartRumbleAmp = 0.1f;
    [SerializeField] float snapStartRumbleDur = 0.1f;

    [Header("Snap End Settings")]
    [Tooltip("Distance from grabbable to theoretical follow target grabbable position required for " +
        "the snapped grabbable to exit the snap.")]
    // TODO: Snapping out could be easier when the key is pulled away from the hole (in lcl -Z direction)
    // TODO C: direction, and also easier when the key itself is fully out of the hole.
    [SerializeField] float snapOutDist = 0.2f;

    [Header("Snappable Movement In Keyhole Settings")]
    [SerializeField] float snplTipMinLclDepth = -0.01f;
    [SerializeField] float snplTipMaxLclDepth = 0.1f;
    [SerializeField] float snplTipMinRotEnabledLclDepth = 0.08f;
    // TODO: Do you want to lerp or not? With lerping you never reach the target.
    // TODO C: e.g.: Vector3 snplNewPos = Vector3.Lerp(snplRb.position, snplWldTgtPos, snplLerpSpd * Time.fixedDeltaTime);
    [SerializeField] float snplLerpSpd = 0.5f;

    IKeyholeSnpl snpl = null;
    // No need to allocate this every physics tick...
    readonly Collider[] overlapResults = new Collider[8];
    KeyholeState st = KeyholeState.LookForSnpls;
    float interpTimer = 0;

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    private void FixedUpdate() {
        switch (st) {
            case KeyholeState.LookForSnpls:
                break;
            case KeyholeState.InterpSnplToSnpTgt:
                interpTimer += Time.fixedDeltaTime;
                MathUtils.InterpRbSoChildAlignsWithTgtPose(
                    snpl.GrblCore.rb,
                    snpl.KeyTipLclPos,
                    // NOTE: We use Quaternion.identity here since we expect rb forward to be
                    // NOTE C: the keyhole insertion direction.
                    Quaternion.identity, 
                    transform.position + new Vector3(0, 0, snplTipMinLclDepth),
                    transform.rotation,
                    interpTimer / interpDur
                );
                if(interpTimer > interpDur)
                    st = KeyholeState.SnplInSnpTgt;
                break;
            case KeyholeState.SnplInSnpTgt:
                SnplPhysicsTick();
                break;
            case KeyholeState.InterpSnplFromSnpTgt:
                interpTimer += Time.fixedDeltaTime;
                MathUtils.InterpRbSoChildAlignsWithTgtPose(
                    snpl.GrblCore.rb,
                    snpl.KeyTipLclPos,
                    // NOTE: We use Quaternion.identity here since we expect rb forward to be
                    // NOTE C: the keyhole insertion direction.
                    Quaternion.identity,
                    transform.position + new Vector3(0, 0, snplTipMinLclDepth),
                    transform.rotation,
                    interpTimer / interpDur
                );
                if (interpTimer > interpDur)
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
            case KeyholeState.LookForSnpls:
                // We look for snappables in Update to ensure that rigidbodies and transforms are
                // synced in case we need to cache relative poses (I'm not sure if we do).
                LookForKeyholeSnpls();
                break;
            case KeyholeState.InterpSnplToSnpTgt:
                break;
            case KeyholeState.SnplInSnpTgt:
                break;
            case KeyholeState.InterpSnplFromSnpTgt:
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
        st = KeyholeState.LookForSnpls;
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
                foundSnpl.GrblCore.rb.transform.forward) > requiredMaxAngForSnapping
            )
                continue;
            foundSnpl.InitSnp(this);
            snpl = foundSnpl;
            foreach(Grb grb in snpl.GrblCore.grbs)
                grb.physHand.controllerHapticImpulsePlayer.SendHapticImpulse(snapStartRumbleAmp, snapStartRumbleDur);
            interpTimer = 0;
            st = KeyholeState.InterpSnplToSnpTgt;
            return;
        }
    }

    private void MoveSnappedSnpl() {
        Rigidbody snplRb = snpl.GrblCore.rb;
        Grb grb = snpl.GrblCore.grbs[0];
        // Find theoretical snappable depth and interpolate towards that.
        Vector3 theoTipWldPos = TheoFolTgtTipWldPos(grb);
        float theoTipLclDepth = MathUtils.InvrsTrfPtUnscaled(transform, theoTipWldPos).z;
        // TODO: Could we clamp later?
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
        if (TipPosInKeyholeSpc().z > snplTipMinRotEnabledLclDepth || TipPosInKeyholeSpc().z < 0) {
            // TODO: Allow the follow target to rotate the key.
        }
        // TODO: If snpl abs roll (normalized to +-180deg) in keyholespace is over 5 deg but under 180, do
        // NOT allow key depth to move below snplTipMinRotEnabledLclDepth if key depth was over
        // snplTipMinRotEnabledLclDepth, or if the key depth was below 0, do not allow key to move to depth over 0.
        // TODO: If snpl depth is below snplTipMinRotEnabledLclDepth, interpolate roll to 0 or 180 (which one is closer).
        
        snplRb.Move(snplWldTgtPos, transform.rotation);
    }

    void SnplPhysicsTick() {
        List<Grb> snplGrbs = snpl.GrblCore.grbs;
        if (
            snplGrbs.Count == 0 ||
            GrblUtils.DistBetweenGrblRbPosNTheoFolTgtGrblPos(snpl.GrblCore.rb, snplGrbs[0]) > snapOutDist
        ) {
            interpTimer = 0;
            st = KeyholeState.InterpSnplFromSnpTgt;
            return;
        }
        MoveSnappedSnpl();
    }

    Vector3 TheoFolTgtTipWldPos(Grb grb) {
        var theoPose = GrblUtils.TheoFolTgtGrblPose(grb);
        return MathUtils.TrfPt(theoPose.Item1, theoPose.Item2, snpl.KeyTipLclPos);
    }

    Vector3 TipPosInKeyholeSpc() {
        Vector3 tipWldPos = MathUtils.TrfPtUnscaled(snpl.GrblCore.rb, snpl.KeyTipLclPos);
        return MathUtils.InvrsTrfPtUnscaled(transform, tipWldPos);
    }
}
