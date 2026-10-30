using UnityEngine;
using System.Collections;

// splits the unleash damage into a few quick hits instead of one big one
public class BurstDamage : MonoBehaviour
{
    public int hits = 4;
    public float interval = 0.12f;   // time between hits

    int totalDamage;

    // wordattack calls this when the unleash lands
    public void Begin(int damage)
    {
        totalDamage = damage;
        StartCoroutine(HitRoutine());
    }

    IEnumerator HitRoutine()
    {
        int perHit = totalDamage / hits;      // ex. 20 / 4 = 5 each
        int leftover = totalDamage % hits;    // ex. 21 / 4 has 1 left over

        for (int i = 0; i < hits; i++)
        {
            // stop if hes already dead or transforming
            if (Health.Boss == null || Health.Boss.IsDead || BossPhases.Transforming) yield break;

            int damage = perHit + (i < leftover ? 1 : 0);   // first hits take the leftover so none gets lost
            if (damage > 0) Health.Boss.TakeDamage(damage);

            yield return new WaitForSeconds(interval);
        }
    }
}
