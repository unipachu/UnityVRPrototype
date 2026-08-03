using UnityEngine;

public class HandAnimCtrl : MonoBehaviour {
    [Header("Settings")]
    [Tooltip("Should start with grip pose (instead of neutral pose)?")]
    [SerializeField] bool startWithGripAnim;

    [Header("Refs")]
    [SerializeField] Animator anim;
    [SerializeField] PlrCtrl plrCtrl;
    [SerializeField] Side handSide;

    private void OnEnable() {
        if (startWithGripAnim)
            anim.Play("Grip");
        if (plrCtrl == null)
            return;
        if (handSide == Side.Left) {
            plrCtrl.LGrabPressed += CrossfadeToGrip;
            plrCtrl.LGrabReleased += CrossfadeToNeutral;
        }
        else {
            plrCtrl.RGrabPressed += CrossfadeToGrip;
            plrCtrl.RGrabReleased += CrossfadeToNeutral;
        }
    }

    private void OnDisable() {
        if (plrCtrl == null)
            return;
        if (handSide == Side.Left) {
            plrCtrl.LGrabPressed -= CrossfadeToGrip;
            plrCtrl.LGrabReleased -= CrossfadeToNeutral;
        }
        else {
            plrCtrl.RGrabPressed -= CrossfadeToGrip;
            plrCtrl.RGrabReleased -= CrossfadeToNeutral;
        }
    }

    public void CrossfadeToNeutral() {
        if(anim.gameObject.activeInHierarchy)
            anim.CrossFade("Neutral", 0.1f);
    }

    public void CrossfadeToGrip() {
        if (anim.gameObject.activeInHierarchy)
            anim.CrossFade("Grip", 0.1f);
    }
}
