using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In the grabbable-joint-driven grab system, represents grabbable than can be grabbed by a physics hand.
/// </summary>
public interface IGrblJntDriven_Grbl : IGnrGrbl<GrblJntDriven_PhysHand, IGrblJntDriven_Grbl>
{
    ConfigurableJoint GrbJnt { get; }
    // NOTE: Sadly you cannot read this as List<GenericGrab> so you cannot have this in the IGnrGrbl class.
    List<GrblJntDriven_Grb> Grbs { get; }
}

