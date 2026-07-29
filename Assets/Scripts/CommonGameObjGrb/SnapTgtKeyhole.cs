using UnityEngine;

public class SnapTgtKeyhole : MonoBehaviour, ISnapTgt {
    [Header("Overlap Sphere Settings")]
    [SerializeField] Vector3 overlapSphereUnscaledLclPos;
    [Tooltip("Radius of the overlap sphere used for looking keyhole snappables.")]
    [Min(0f)]
    [SerializeField] float chkSphereR = 0.1f;
    [Tooltip("Layers used by the overlap sphere when searching for keyhole snappables.")]
    [SerializeField] LayerMask grbLayers;

    IKeyholeSnpl snpl = null;
    // No need to allocate this every physics tick...
    readonly Collider[] overlapResults = new Collider[8];

    // -----------------------------------------
    // UNITY CALLBACKS
    // -----------------------------------------

    //private void OnTriggerEnter(Collider other) {
    //    Debug.Log("Went here " + Time.time);
    //    // Only one snappable at a time!
    //    if (snpl != null || other.attachedRigidbody == null)
    //        return;
    //    IKeyholeSnpl foundSnpbl = other.attachedRigidbody.GetComponent<IKeyholeSnpl>();
    //    if (foundSnpbl == null)
    //        return;
    //    if (!foundSnpbl.CanSnp())
    //        return;
    //    foundSnpbl.InitSnp(this);
    //    snpl = foundSnpbl;
    //}

    private void FixedUpdate() {
        if (snpl == null)
            LookForKeyholeSnpls();
        else
            SnplPhysicsTick();
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.greenYellow;
        Gizmos.DrawWireSphere(MathUtils.UnscaledTrfPt(transform, overlapSphereUnscaledLclPos), chkSphereR);
    }

    // -----------------------------------------
    // PRIVATE METHODS
    // -----------------------------------------

    void EndSnp() {
        snpl.OnEndSnp();
        snpl = null;
    }

    void LookForKeyholeSnpls() {
        Vector3 worldPos = MathUtils.UnscaledTrfPt(transform, overlapSphereUnscaledLclPos);
        int hitCount = Physics.OverlapSphereNonAlloc(
            worldPos,
            chkSphereR,
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
            foundSnpl.InitSnp(this);
            snpl = foundSnpl;
            return;
        }
    }

    void SnplPhysicsTick() {
        snpl.GrblCore.rb.Move(transform.position, transform.rotation);
        if(snpl.GrblCore.grbs.Count == 0) {
            EndSnp();
            return;
        }
        if (snpl.GrblCore.DistBetweenRbNPhysHandFollowTgt(0) > 0.4f)
            EndSnp();
    }
}
