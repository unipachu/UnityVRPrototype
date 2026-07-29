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
                LookForKeyholeSnpls();
                break;
            case KeyholeState.InterpSnplToSnpTgt:
                MathUtils.InterpToAlignChildWithTgt(
                    snpl.GrblCore.rb,
                    snpl.KeyTipTrf,
                    transform.position,
                    transform.rotation,
                    interpTimer / interpDur
                );
                interpTimer += Time.fixedDeltaTime;
                if(interpTimer > interpDur) {
                    MathUtils.InterpToAlignChildWithTgt(
                        snpl.GrblCore.rb,
                        snpl.KeyTipTrf,
                        transform.position,
                        transform.rotation,
                        1
                    );
                    st = KeyholeState.SnplInSnpTgt;
                }
                break;
            case KeyholeState.SnplInSnpTgt:
                SnplPhysicsTick();
                break;
            case KeyholeState.InterpSnplFromSnpTgt:
                MathUtils.InterpToAlignChildWithTgt(
                    snpl.GrblCore.rb,
                    snpl.KeyTipTrf,
                    transform.position,
                    transform.rotation,
                    interpTimer / interpDur
                );
                interpTimer += Time.fixedDeltaTime;
                if (interpTimer > interpDur) {
                    MathUtils.InterpToAlignChildWithTgt(
                        snpl.GrblCore.rb,
                        snpl.KeyTipTrf,
                        transform.position,
                        transform.rotation,
                        1
                    );
                    EndSnp();
                }
                break;
            default:
                Debug.LogError("Switch defaulted", this);
                break;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.greenYellow;
        Gizmos.DrawWireSphere(MathUtils.UnscaledTrfPt(transform, overlapSphereUnscaledLclPos), overlapSphereR);
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
        Vector3 worldPos = MathUtils.UnscaledTrfPt(transform, overlapSphereUnscaledLclPos);
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
            if (Vector3.Angle(transform.forward, foundSnpl.KeyTipTrf.forward) > requiredMaxAngForSnapping)
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
        snpl.GrblCore.rb.Move(transform.position, transform.rotation);
        if(
            snpl.GrblCore.grbs.Count == 0 ||
            snpl.GrblCore.DistBetweenRbNPhysHandFollowTgt(0) > snapEndDist
        ) {
            interpTimer = 0;
            st = KeyholeState.InterpSnplFromSnpTgt;
            return;
        }
    }
}
