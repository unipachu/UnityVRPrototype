using UnityEngine;

/// <summary>
/// Causes this game object to follow the front facing direction of a target transform, e.g. makes an UI object follow the gaze of a camera/hmd. <br/>
/// NOTE: The scale of this object during Awake is the scale the object is displayed at min distance from player.
/// The scale gets larger the further the object moves from player gaze.
/// </summary>
public class FwdDirFol : MonoBehaviour {
    enum FolSmoothingMode {
        SmoothMov,
        OnlySmoothDist,
        NoMovSmoothing
    }

    [Header("Box Check Settings")]
    [Tooltip("Default size of the checkbox, used to check if there's room for the follower object. " +
        "This should match with the default size of this game object (or its visuals we want to fit in the world).")]
    [SerializeField] Vector3 ChkBoxSz = new(0.3f, 0.3f, 0.02f);

    [Header("Target Direction Offset")]
    [Tooltip("Default target direction is forward of the hmd. This offsets the follower target direction")]
    [SerializeField] Vector3 tgtDirOffsetEuler;

    [Header("Target Distance From Target Object")]
    [Tooltip("Min distance of the follower obj from the target transform.")]
    [SerializeField] float minDistFromTgtObj = 0.2f;
    [Tooltip("Max distance of the follower obj from the target transform.")]
    [SerializeField] float maxDistFromTgtObj = 10f;
    [Tooltip("When raycast from target game object towards the target direction hits an obstacle, " +
        "we use this distance from the hit obstacle back towards the target obj for the first check box.")]
    [SerializeField] float tgtDistPadding = 0.05f;

    [Header("Locked Distance")]
    [Tooltip("Should the target distance from the target object always be constant?")]
    [SerializeField] bool lockDist = false;
    [Tooltip("If lockDistance = true, what should the target distance between the follower object and the target object be?")]
    [SerializeField] float lockedDist = 2f;

    [Header("Smoothing")]
    [Tooltip("Should this object smoothly follow the target pos/rot with linear interpolation (instead of snapping to target pos/rot)?")]
    [SerializeField] FolSmoothingMode smoothingMode = FolSmoothingMode.OnlySmoothDist;
    [SerializeField] float posInterpFolSpd = 12f;
    [SerializeField] float rotInterpFolSpd = 10f;
    [SerializeField] float sclInterpFolSpd = 12f;

    [Header("Physics")]
    [Tooltip("Min distance between CheckBox query steps.")]
    [SerializeField] float minChkBoxStepLen = 0.05f;
    [Tooltip("Scales CheckBox query step length by previous CheckBox distance times this.")]
    [Range(0.01f, 0.99f)]
    [SerializeField] float chkBoxStepScaler = 0.1f;
    [Tooltip("Maximum amount of CheckBox queries per frame. Used as a failsafe for while-loop.")]
    [SerializeField] int maxChkBoxQryAmt = 100;
    [Tooltip("Used for all physics checks.\n" +
        "NOTE: Should likely exclude layer of the target game object.")]
    [SerializeField] LayerMask colMask = ~0;
    [Tooltip("Used for all physics checks.")]
    [SerializeField] QueryTriggerInteraction trgIxn = QueryTriggerInteraction.Ignore;

    [Header("Refs")]
    [Tooltip("Transform of the target object whose forward direction we want to follow, e.g. camera transform.")]
    [SerializeField] Transform tgtObjTrf;

    Vector3 initScl;

    void Awake() {
        initScl = transform.localScale;
    }

    void OnValidate() {
        minDistFromTgtObj = Mathf.Max(0.01f, minDistFromTgtObj);
        maxDistFromTgtObj = Mathf.Max(minDistFromTgtObj, maxDistFromTgtObj);
        tgtDistPadding = Mathf.Max(0f, tgtDistPadding);
        minChkBoxStepLen = Mathf.Max(0.001f, minChkBoxStepLen);
        maxChkBoxQryAmt = Mathf.Max(1, maxChkBoxQryAmt);
        lockedDist = Mathf.Max(0.01f, lockedDist);
    }

    void LateUpdate() {
        if (tgtObjTrf == null)
            return;
        Vector3 origin = tgtObjTrf.position;
        Quaternion offsetRot = Quaternion.Euler(tgtDirOffsetEuler);
        Vector3 tgtDir = (tgtObjTrf.rotation * offsetRot) * Vector3.forward;
        float tgtDist;
        if (lockDist)
            tgtDist = lockedDist;
        else
            tgtDist = FindTgtDist(origin, tgtDir);
        Vector3 objTgtPos = origin + tgtDir * tgtDist;
        // Target rotation for this game object.
        Quaternion tgtRot = Quaternion.LookRotation(objTgtPos - origin, Vector3.up);
        float tgtSclFactor = Mathf.Max(0.01f, tgtDist / minDistFromTgtObj);
        Vector3 tgtScl = initScl * tgtSclFactor;
        float posLerp = 1f - Mathf.Exp(-posInterpFolSpd * Time.deltaTime);
        float rotLerp = 1f - Mathf.Exp(-rotInterpFolSpd * Time.deltaTime);
        float scaleLerp = 1f - Mathf.Exp(-sclInterpFolSpd * Time.deltaTime);
        switch (smoothingMode) {
            case FolSmoothingMode.SmoothMov:
                transform.position = Vector3.Lerp(transform.position, objTgtPos, posLerp);
                transform.rotation = Quaternion.Slerp(transform.rotation, tgtRot, rotLerp);
                transform.localScale = Vector3.Lerp(transform.localScale, tgtScl, scaleLerp);
                break;
            case FolSmoothingMode.OnlySmoothDist:
                // Snap rotation immediately.
                transform.rotation = tgtRot;
                float currentDist = Vector3.Distance(origin, transform.position);
                float smoothedDist = Mathf.Lerp(currentDist, tgtDist, posLerp);
                transform.position = origin + tgtDir * smoothedDist;
                transform.localScale = Vector3.Lerp(transform.localScale, tgtScl, scaleLerp);
                break;
            case FolSmoothingMode.NoMovSmoothing:
                transform.SetPositionAndRotation(objTgtPos, tgtRot);
                transform.localScale = tgtScl;
                break;
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        // Draw the CheckBox at the initial (min distance) size.
        Gizmos.DrawWireCube(Vector3.zero, Vector3.Scale(ChkBoxSz, transform.localScale));
        Gizmos.matrix = oldMatrix;
    }

    /// <summary>
    /// Finds target distance from target transform with a Raycast and interative CheckBoxes.
    /// </summary>
    float FindTgtDist(Vector3 origin, Vector3 tgtDir) {
        float tgtDist = maxDistFromTgtObj;
        if (Physics.Raycast(
            origin,
            tgtDir,
            out RaycastHit rayHit,
            maxDistFromTgtObj,
            colMask,
            trgIxn)
        ) {
            tgtDist = Mathf.Max(minDistFromTgtObj, rayHit.distance - tgtDistPadding);

            //Debug.Log(
            //    $"Canvas ray hit: {rayHit.collider.name}\n" +
            //    $"Layer: {LayerMask.LayerToName(rayHit.collider.gameObject.layer)}\n" +
            //    $"Distance: {rayHit.distance}\n" +
            //    $"Point: {rayHit.point}");
        }
        bool foundValidPos = false;
        Quaternion chkBoxRot = Quaternion.LookRotation(-tgtDir, Vector3.up);
        float testDist = Mathf.Max(minDistFromTgtObj, tgtDist);
        int chks = 0;
        // We iteratively make CheckBox queries to find target position (and scale) for this object.
        while (testDist >= minDistFromTgtObj) {
            chks++;
            Vector3 testPos = origin + tgtDir * testDist;
            float sclFactor = testDist / minDistFromTgtObj;
            Vector3 halfExtents = GetHalfExtents(sclFactor);
            bool blocked =
                Physics.CheckBox(
                    testPos,
                    halfExtents,
                    chkBoxRot,
                    colMask,
                    trgIxn
                );
            if (!blocked) {
                tgtDist = testDist;
                foundValidPos = true;
                break;
            }
            float stepLength = Mathf.Max(minChkBoxStepLen, testDist * chkBoxStepScaler);
            testDist -= stepLength;
            if (chks == maxChkBoxQryAmt) {
                Debug.LogWarning("Max CheckBox steps reached! Debug HUD might not display properly!", this);
                break;
            }
        }
        if (!foundValidPos) {
            tgtDist = minDistFromTgtObj;
        }
        return tgtDist;
    }

    Vector3 GetHalfExtents(float sclFactor) {
        return new Vector3(
            ChkBoxSz.x * initScl.x * sclFactor * 0.5f,
            ChkBoxSz.y * initScl.y * sclFactor * 0.5f,
            ChkBoxSz.z * initScl.z * sclFactor * 0.5f
        );
    }
}