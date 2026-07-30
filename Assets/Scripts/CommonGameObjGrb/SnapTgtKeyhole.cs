using System.Collections.Generic;
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
    [SerializeField] float requiredMaxAngForSnapping = 45;

    [Header("Interpolation Settings")]
    [Tooltip("Duration for snappable interpolating into and away from snap during " +
        "snap initialization and end.")]
    [SerializeField] float interpDur = 0.1f;

    [Header("Rumble Settings")]
    [SerializeField] float snapStartRumbleAmp = 0.1f;
    [SerializeField] float snapStartRumbleDur = 0.1f;

    [Header("Snap End Settings")]
    [SerializeField] float snapEndDist = 0.4f;

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
                    transform.position,
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
                    transform.position,
                    transform.rotation,
                    interpTimer / interpDur
                );
                if (interpTimer > interpDur)
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
                // We look for snappables in Update to ensure that rigidbodies and transforms are synced.
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
            QueryTriggerInteraction.Ignore);
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

    void SnplPhysicsTick() {
        Rigidbody snplRb = snpl.GrblCore.rb;
        List<Grb> snplGrbs = snpl.GrblCore.grbs;
        snplRb.Move(transform.position, transform.rotation);
        if(
            snplGrbs.Count == 0 ||
            GrblUtils.DistBetweenGrblRbPosNTheoreticalFollowTgtGrblPos(snplRb, snplGrbs[0]) > snapEndDist
        ) {
            interpTimer = 0;
            st = KeyholeState.InterpSnplFromSnpTgt;
            return;
        }
    }
}
