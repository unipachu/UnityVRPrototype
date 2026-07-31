using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared core features of grabbable's grab joint driven grabbables.
/// </summary>
// TODO: This doesn't need to be a monobehaviour...
public class GrblJntDriven_GrblCore : MonoBehaviour {
    public readonly List<GrblJntDriven_Grb> grbs = new(2);
}
