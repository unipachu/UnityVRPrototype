using UnityEngine;

/// <summary>
/// Controls ghost shader's dithering amount.
/// </summary>
public class GhostShdrCtlr : MonoBehaviour {
    [SerializeField] private Renderer tgtRenderer;
    //private MaterialPropertyBlock propertyBlock;

    private void Awake() {
        //propertyBlock = new MaterialPropertyBlock();
    }

    public void SetTransparency(float value) {
        tgtRenderer.material.SetFloat("_ditherAmount", value);
        // TODO: Property blocks are supposedly a more performant way to override
        // TODO C: per-renderer material properties. But they don't seem to work here for some reason.
        //Debug.Log($"Transparency set to: {value}");
        //tgtRenderer.GetPropertyBlock(propertyBlock);
        //Debug.Log("property block dithering: " + propertyBlock.GetFloat("_ditherAmount"));
        //propertyBlock.SetFloat("_ditherAmount", value);
        //tgtRenderer.SetPropertyBlock(propertyBlock);
        //Debug.Log("tgtRenderer dithering: " + tgtRenderer.material.GetFloat("_ditherAmount"));
    }
}
