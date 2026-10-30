using UnityEngine;

// clears any leftover animator triggers when this object turns on
// fixes the mask form attacking by itself right when it appeared (an old trigger was still queued)
public class ResetAnimatorOnEnable : MonoBehaviour
{
    void OnEnable()
    {
        Animator anim = GetComponent<Animator>();
        if (anim == null) return;

        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger)
                anim.ResetTrigger(p.name);
    }
}
