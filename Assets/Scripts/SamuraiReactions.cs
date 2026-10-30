using UnityEngine;

// samurai's parry anim when a word gets deflected, and his death anim + grunt when he dies
public class SamuraiReactions : MonoBehaviour
{
    public Animator animator;

    bool dead;   // so death only plays once

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void OnEnable()  { WordAttack.OnParry += Parry; }
    void OnDisable() { WordAttack.OnParry -= Parry; }

    void Update()
    {
        if (!dead && Health.Player != null && Health.Player.IsDead)
        {
            dead = true;
            animator.SetTrigger("Die");
            AudioManager.PlayDeath();
        }
    }

    void Parry()
    {
        if (dead) return;   // no parrying after dying
        animator.SetTrigger("Parry");
    }
}
