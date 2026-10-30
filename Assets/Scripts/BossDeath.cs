using UnityEngine;

// on the mask form. plays the death anim once the boss is out of hp
public class BossDeath : MonoBehaviour
{
    public Animator animator;

    BossPhases phases;
    bool dead;   // so it only triggers once

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        phases = GetComponentInParent<BossPhases>();
    }

    void Update()
    {
        if (!dead && Health.Boss != null && Health.Boss.IsDead)
        {
            dead = true;
            animator.SetTrigger("Die");
            if (phases != null) phases.FadeMoonForVictory();   // the crimson moon fades out
        }
    }
}
