using UnityEngine;
/// <summary>
/// Use this to add descriptions to game objects, prefabs etc.
/// </summary>
public class ReadmeComponent : MonoBehaviour
{
    [TextArea(5, 20)]
    public string description;
}
