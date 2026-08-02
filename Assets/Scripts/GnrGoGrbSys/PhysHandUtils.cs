using System;
using UnityEngine;

public static class PhysHandUtils {
    /// <summary>
    /// Sets the world joint to follow target pose, i.e.: tries to use joint drives
    /// to move the physics hand to the player's actual hand position and rotation.
    /// </summary>
    public static void SetWldJntTgtToFolTgt(IGnrPhysHand physHand){
        physHand.WldJnt.targetPosition = physHand.FollowTgtTrf.position;
        physHand.WldJnt.targetRotation = physHand.FollowTgtTrf.rotation;
    }

    public static void SetWldJntTgt(IGnrPhysHand physHand, Vector3 targetPos, Quaternion targetRot) {
        physHand.WldJnt.targetPosition = targetPos;
        physHand.WldJnt.targetRotation = targetRot;
    }

    /// <summary>
    /// Tries to find a grabbable component implementing <typeparamref name="TGrbl"/>
    /// on the collider's attached Rigidbody. Returns <see langword="null"/> if no
    /// matching component is found.<br/>
    /// NOTE: The <typeparamref name="TGrbl"/> implementation must be on the same
    /// GameObject as the collider's attached Rigidbody.
    /// </summary>
    public static TGrbl TryGetPhysicsHandGrabbableObject<TGrbl, TPhysHand>(Collider otherCollider)
        where TGrbl : class, IGnrGrbl<TPhysHand, TGrbl>
        where TPhysHand : IGnrPhysHand<TGrbl, TPhysHand>
    {
        TGrbl grabbable = null;
        Rigidbody otherRb = otherCollider.attachedRigidbody;
        if (otherRb)
            grabbable = otherRb.GetComponent<TGrbl>();
        return grabbable;
    }

    /// <summary>
    /// Searches for nearby objects with OverlapSphere and checks if any are eligible for grabbing.
    /// If so, grabs the closest <typeparamref name="TGrbl"/> and returns true, otherwise returns false.
    /// </summary>
    public static bool TryGrabbing<TGrbl, TPhysHand>(TPhysHand physHand, Vector3 grblSearchPos, GrbrData grbrData)
        where TGrbl : class, IGnrGrbl<TPhysHand, TGrbl>
        where TPhysHand : IGnrPhysHand<TGrbl, TPhysHand> 
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            grblSearchPos,
            grbrData.overlapSphereR,
            grbrData.grbLayers,
            QueryTriggerInteraction.Ignore
        );
        if (nearbyColliders.Length == 0)
            return false;
        //Debug.Log(
        //    $"Found colliders ({nearbyColliders.Length}): " +
        //    string.Join(", ", Array.ConvertAll(nearbyColliders, c => c.name))
        //);
        TGrbl closestGrabbable = null;
        float distanceToClosestGrabbable = 0;
        // Find closest grabbable object.
        foreach (Collider collider in nearbyColliders) {
            TGrbl grabbable = TryGetPhysicsHandGrabbableObject<TGrbl, TPhysHand>(collider);
            if (grabbable == null)
                continue;
            if (!grabbable.CanBeGrabbed(physHand))
                continue;
            if (closestGrabbable == null) {
                closestGrabbable = grabbable;
                continue;
            }
            float grabbableDistance = grabbable.GetDistToGrbPt(grblSearchPos);
            if (grabbableDistance < distanceToClosestGrabbable) {
                closestGrabbable = grabbable;
                distanceToClosestGrabbable = grabbableDistance;
            }
        }
        if (closestGrabbable == null)
            return false;
        // Found closest grabbable that can be grabbed!
        physHand.InitGrab(closestGrabbable);
        return true;
    }

    public static void UpdateTgtGhostShader(
        GhostShdrCtlr handGhostShdrCtrl,
        Vector3 physHandWorldPos,
        Vector3 folTgtPos,
        FolTgtGhostShdrData data
    ) {
        float newTransparency = Mathf.InverseLerp(
            data.invisibleDist,
            data.maxTransparencyDist,
            Vector3.Distance(folTgtPos, physHandWorldPos)
        ) * data.maxTransparency;
        handGhostShdrCtrl.SetTransparency(newTransparency);
    }
}
