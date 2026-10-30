using UnityEngine;
using System.Collections;

// keeps the hp/blade bars hidden during the intro and fades them in after
public class HUDReveal : MonoBehaviour
{
    public CanvasGroup[] groups;              // player bar, blade bar, boss bar
    public bool waitForFight = false;         // false = show at the countdown, true = show at FIGHT!
    public float fadeTime = 0.6f;

    bool shown;

    void Start()
    {
        SetAlpha(0f);                         // hidden until the fight is about to start
    }

    void Update()
    {
        if (shown) return;

        bool ready;
        if (waitForFight)
            ready = FightManager.Instance != null
                    && FightManager.Instance.CurrentState == FightManager.State.Fighting;
        else
            ready = !IntroCutscene.Playing;   // intro is done (or got skipped on retry)

        if (ready)
        {
            shown = true;
            StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(t / fadeTime));
            yield return null;
        }
        SetAlpha(1f);
    }

    void SetAlpha(float a)
    {
        foreach (CanvasGroup g in groups)
            if (g != null) g.alpha = a;
    }
}
