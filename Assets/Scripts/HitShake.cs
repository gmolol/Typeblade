using UnityEngine;
using System.Collections;

// little camera shake whenever this character takes damage
public class HitShake : MonoBehaviour
{
    public float duration = 0.2f;
    public float strength = 0.15f;

    Health health;
    Transform cam;
    Vector3 home;       // cameras normal spot
    Coroutine running;

    void Start()
    {
        health = GetComponent<Health>();
        cam = Camera.main.transform;
        home = cam.localPosition;

        if (health != null) health.OnDamaged += Shake;
    }

    void OnDestroy()
    {
        if (health != null) health.OnDamaged -= Shake;
    }

    void Shake()
    {
        if (UnleashCutscene.Playing) return;          // the cutscene is moving the camera then

        if (running != null) StopCoroutine(running);  // new hit restarts the shake instead of stacking
        running = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float s = strength * (1f - t / duration);   // gets weaker over time
            cam.localPosition = home + (Vector3)(Random.insideUnitCircle * s);
            yield return null;
        }
        cam.localPosition = home;
        running = null;
    }
}
