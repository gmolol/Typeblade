using UnityEngine;

// plays the boss hurt anim whenever he takes damage. one on each form
public class BossHurt : MonoBehaviour
{
    public Animator animator;

    Health health;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        health = GetComponentInParent<Health>();   // health is on the parent boss object
    }

    // subscribe on enable / unsubscribe on disable, so the hidden form doesnt react
    void OnEnable()
    {
        if (health != null) health.OnDamaged += Hurt;
    }

    void OnDisable()
    {
        if (health != null) health.OnDamaged -= Hurt;
    }

    void Hurt()
    {
        if (health.IsDead) return;      // let the death anim play instead
        animator.SetTrigger("Hurt");
    }
}
