using UnityEngine;

/// <summary>
/// Causes this game object to follow the front facing direction of a target transform, e.g. makes an UI object follow the gaze of a camera/hmd. <br/>
/// NOTE: The scale of this object during Awake is the scale the object is displayed at min distance from player.
/// The scale gets larger the further the object moves from player gaze.
/// </summary>
public class FwdDirFollower : MonoBehaviour
{
    [Header("Box Check Settings")]
    [Tooltip("Default size of the checkbox, used to check if there's room for the follower object. " +
        "This should match with the default size of this game object (or its visuals we want to fit in the world).")]
    [SerializeField] Vector3 ChkBoxSz = new(0.15f, 0.15f, 0.02f);

    [Header("Target Direction Offset")]
    [Tooltip("Default target direction is forward of the hmd. This offsets the follower target direction")]
    [SerializeField] Vector3 tgtDirOffsetEuler;

    [Header("Target Distance From Target Object")]
    [Tooltip("Min distance of the follower obj from the target transform.")]
    [SerializeField] float minDistFromTgtObj = 0.2f;
    [Tooltip("Max distance of the follower obj from the target transform.")]
    [SerializeField] float maxDistFromTgtObj = 10f;
    [Tooltip("When raycast from target object towards target object forward direction hits an obstacle, " +
        "we use this distance from the object for the first check box.")]
    [SerializeField] float tgtDistPadding = 0.05f;

    [Header("Locked Distance")]
    [Tooltip("Should the target distance from the target object always be constant?")]
    [SerializeField] bool lockDist = false;
    [Tooltip("If lockDistance = true, what should the target distance between the follower object and the target object be?")]
    [SerializeField] float lockedDist = 2f;

    [Header("Smoothing")]
    [Tooltip("Should this object smoothly follow the target pos/rot/ with linear interpolation (instead of snapping to target pos/rot)?")]
    [SerializeField] bool smoothMvmt = true;
    [SerializeField] float posInterpFollowSpd = 12f;
    [SerializeField] float rotInterpFollowSpd = 10f;
    [SerializeField] float sclInterpFollowSpd = 12f;

    [Header("Physics")]
    [Tooltip("Min distance between CheckBox query steps.")]
    [SerializeField] float minBoxChkStepLen = 0.05f;
    [Tooltip("Scales CheckBox query step length by previous CheckBox distance times this.")]
    [Range(0.01f, 0.99f)]
    [SerializeField] float boxStepScaler = 0.1f;
    [Tooltip("Maximum amount of CheckBox queries per frame. Used as a failsafe for while-loop.")]
    [SerializeField] int maxBoxChks = 100;
    [Tooltip("Used for all physics checks.\n" +
        "NOTE: Should likely exclude layer of the target game object.")]
    [SerializeField] LayerMask collisionMask = ~0;
    [Tooltip("Used for all physics checks.")]
    [SerializeField] QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Refs")]
    [Tooltip("Transform of the target object whose forward direction we want to follow, e.g. camera transform.")]
    [SerializeField] Transform tgtObjTrf;

    Vector3 initialScale;

    void Awake()
    {
        initialScale = transform.localScale;
    }

    void OnValidate()
    {
        minDistFromTgtObj = Mathf.Max(0.01f, minDistFromTgtObj);
        maxDistFromTgtObj = Mathf.Max(minDistFromTgtObj, maxDistFromTgtObj);
        tgtDistPadding = Mathf.Max(0f, tgtDistPadding);
        minBoxChkStepLen = Mathf.Max(0.001f, minBoxChkStepLen);
        maxBoxChks = Mathf.Max(1, maxBoxChks);
        lockedDist = Mathf.Max(0.01f, lockedDist);
    }

    void LateUpdate()
    {
        if (tgtObjTrf == null)
            return;

        Vector3 origin = tgtObjTrf.position;
        Quaternion offsetRot = Quaternion.Euler(tgtDirOffsetEuler);
        Vector3 tgtDir = (tgtObjTrf.rotation * offsetRot) * Vector3.forward;
        float tgtDist;

        if (lockDist)
        {
            tgtDist = lockedDist;
        }
        else
        {
            tgtDist = FindTgtDist(origin, tgtDir);
        }

        Vector3 objTgtPos = origin + tgtDir * tgtDist;

        // Target rotation for this game object.
        Quaternion targetRotation = Quaternion.LookRotation(objTgtPos - origin, Vector3.up);
        float targetScaleFactor = Mathf.Max(0.01f, tgtDist / minDistFromTgtObj);
        Vector3 targetScale = initialScale * targetScaleFactor;

        if (smoothMvmt)
        {
            float posLerp = 1f - Mathf.Exp(-posInterpFollowSpd * Time.deltaTime);
            float rotLerp = 1f - Mathf.Exp(-rotInterpFollowSpd * Time.deltaTime);
            float scaleLerp = 1f - Mathf.Exp(-sclInterpFollowSpd * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, objTgtPos, posLerp);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotLerp);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleLerp);
        }
        else
        {
            transform.SetPositionAndRotation(objTgtPos, targetRotation);
            transform.localScale = targetScale;
        }
    }

    /// <summary>
    /// Finds target distance from target transform with a Raycast and interative CheckBoxes.
    /// </summary>
    float FindTgtDist(Vector3 origin, Vector3 tgtDir)
    {
        float tgtDist = maxDistFromTgtObj;
        if (Physics.Raycast(
            origin,
            tgtDir,
            out RaycastHit rayHit,
            maxDistFromTgtObj,
            collisionMask,
            triggerInteraction))
        {
            tgtDist = Mathf.Max(minDistFromTgtObj, rayHit.distance - tgtDistPadding);

            Debug.Log(
                $"Canvas ray hit: {rayHit.collider.name}\n" +
                $"Layer: {LayerMask.LayerToName(rayHit.collider.gameObject.layer)}\n" +
                $"Distance: {rayHit.distance}\n" +
                $"Point: {rayHit.point}");
        }
        bool foundValidPos = false;
        Quaternion chkBoxRot = Quaternion.LookRotation(-tgtDir, Vector3.up);
        float testDist = Mathf.Max(minDistFromTgtObj, tgtDist);
        int chks = 0;

        // We iteratively make CheckBox queries to find target position (and scale) for this object.
        while (testDist >= minDistFromTgtObj)
        {
            chks++;
            Vector3 testPosition = origin + tgtDir * testDist;
            float scaleFactor = testDist / minDistFromTgtObj;
            Vector3 halfExtents = GetHalfExtents(scaleFactor);

            bool blocked =
                Physics.CheckBox(
                    testPosition,
                    halfExtents,
                    chkBoxRot,
                    collisionMask,
                    triggerInteraction);

            if (!blocked)
            {
                tgtDist = testDist;
                foundValidPos = true;
                break;
            }
            
            float stepLength = Mathf.Max(minBoxChkStepLen, testDist * boxStepScaler);
            testDist -= stepLength;
            
            if (chks == maxBoxChks)
            {
                Debug.LogWarning("Max CheckBox steps reached! Debug HUD might not display properly!", this);
                break;
            }
        }

        if (!foundValidPos)
        {
            tgtDist = minDistFromTgtObj;
        }

        return tgtDist;
    }

    Vector3 GetHalfExtents(float scaleFactor)
    {
        return new Vector3(
            ChkBoxSz.x * initialScale.x * scaleFactor * 0.5f,
            ChkBoxSz.y * initialScale.y * scaleFactor * 0.5f,
            ChkBoxSz.z * initialScale.z * scaleFactor * 0.5f);
    }
}