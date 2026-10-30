using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// handles the switch from crow form to the mask (true form) when his hp gets low
public class BossPhases : MonoBehaviour
{
    // other scripts check this to pause stuff during the transformation
    public static bool Transforming { get; private set; }

    [Header("Forms")]
    public GameObject crowForm;
    public GameObject maskForm;
    public AttackSpawner spawner;

    [Header("Phase Two Trigger")]
    public float phaseTwoAt = 0.2f;          // transforms at 20% hp

    [Header("Phase Two Difficulty")]
    public float phase2Interval = 1.8f;      // attacks come faster
    public float phase2SpeedBoost = 1.25f;   // and words move faster

    [Header("Moon")]
    public SpriteRenderer moon;
    public Color phase1MoonColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
    public Color phase2MoonColor = Color.white;
    public float moonFadeTime = 1f;
    public Color victoryMoonColor = new Color(0.85f, 0.85f, 0.9f, 0.1f);   // faint ghost moon
    public float victoryFadeTime = 2.5f;

    [Header("Banner")]
    public GameObject phaseText;
    public float bannerDelay = 0.3f;     // wait after the swap so the flash clears first
    public float bannerFadeIn = 0.25f;
    public float bannerHold = 1.2f;
    public float bannerFadeOut = 0.4f;

    [Header("Taunt")]
    public GameObject tauntBox;
    public TMP_Text tauntText;
    [TextArea] public string tauntLine = "Enough, poet. You have only woken the crimson moon.";
    public float tauntDelay = 0.3f;              // pause after the banner fades
    public float tauntCharDelay = 0.05f;         // typewriter speed
    public float tauntPunctuationPause = 0.3f;   // extra pause after . , ? !
    public float tauntHold = 1.5f;               // time to read it before the fight starts again
    public bool glowWhileTaunting = false;       // keep the mask glowing while he talks

    [Header("Transformation")]
    public Image whiteFlash;
    public float hitStopScale = 0.15f;   // slow mo speed during the break
    public float hitStopTime = 0.35f;    // real seconds of slow mo
    public float staggerTime = 1.0f;     // crow flickers before the swap
    public float flashInTime = 0.08f;
    public float flashOutTime = 0.6f;
    public float smallShake = 0.08f;
    public float bigShake = 0.35f;
    public float pauseAfterSwap = 1.4f;
    public float refillTime = 1f;

    Health health;
    Vector3 camHome;   // where the camera normally sits, so shakes go back to it
    bool started;

    void Start()
    {
        Transforming = false;   // static, so reset it in case a retry loaded mid-transform

        health = GetComponent<Health>();
        camHome = Camera.main.transform.localPosition;

        crowForm.SetActive(true);
        maskForm.SetActive(false);
        if (moon != null) moon.color = phase1MoonColor;
        if (phaseText != null) phaseText.SetActive(false);
        if (tauntBox != null) tauntBox.SetActive(false);
        if (whiteFlash != null) SetFlash(0f);

        health.minHP = 1;   // crow form cant actually die, he has to transform first
    }

    void Update()
    {
        if (started) return;

        if (health.CurrentHP <= Mathf.Max(1, health.maxHP * phaseTwoAt))
        {
            started = true;
            StartCoroutine(PhaseTwoRoutine());
        }
    }

    // bossdeath calls this when he dies
    public void FadeMoonForVictory()
    {
        if (moon != null) StartCoroutine(FadeMoonTo(victoryMoonColor, victoryFadeTime));
    }

    // the whole transformation, step by step
    IEnumerator PhaseTwoRoutine()
    {
        Transforming = true;

        TypingInput typing = FightManager.Instance != null ? FightManager.Instance.typing : null;
        if (typing != null) typing.enabled = false;           // no random typos during the cutscene

        // 1. break: stop attacks, clear the words, slow mo, crow flinches
        spawner.enabled = false;
        foreach (WordAttack w in WordAttack.All.ToArray())   // toarray bc destroying changes the list
            if (w.type != AttackType.Unleash) Destroy(w.gameObject);

        Animator crowAnim = crowForm.GetComponent<Animator>();
        SpriteRenderer crowSR = crowForm.GetComponent<SpriteRenderer>();
        if (crowAnim != null) crowAnim.SetTrigger("Hurt");

        StartCoroutine(Shake(staggerTime + 0.1f, smallShake));

        Time.timeScale = hitStopScale;
        yield return new WaitForSecondsRealtime(hitStopTime);
        if (Time.timeScale == hitStopScale) Time.timeScale = 1f;   // dont undo it if the player paused

        if (crowAnim != null) crowAnim.speed = 0f;   // freeze him in the flinch pose

        // 2. stagger: crow flickers faster and faster
        float t = 0f;
        float blink = 0.15f;
        while (t < staggerTime)
        {
            crowSR.enabled = !crowSR.enabled;
            yield return new WaitForSeconds(blink);
            t += blink;
            blink = Mathf.Max(0.03f, blink * 0.8f);   // each blink is 20% shorter
        }
        crowSR.enabled = true;

        // 3. flash: screen goes white and the forms swap behind it
        yield return Fade(0f, 1f, flashInTime);

        if (crowAnim != null) crowAnim.speed = 1f;
        crowForm.SetActive(false);
        maskForm.SetActive(true);
        health.flashTarget = maskForm.GetComponent<SpriteRenderer>();   // so hit flashes show on the new form

        // 4. emerge: mask glows, big shake, red moon, banner, hp refills
        Animator maskAnim = maskForm.GetComponent<Animator>();
        if (maskAnim != null) maskAnim.SetBool("Tell", true);

        StartCoroutine(Shake(0.5f, bigShake));
        StartCoroutine(FadeMoonTo(phase2MoonColor, moonFadeTime));
        StartCoroutine(RefillHealth());
        Coroutine banner = StartCoroutine(Banner());

        yield return Fade(1f, 0f, flashOutTime);
        yield return new WaitForSeconds(pauseAfterSwap);
        yield return banner;                                   // wait till TRUE FORM is gone

        // 5. taunt
        if (maskAnim != null && !glowWhileTaunting) maskAnim.SetBool("Tell", false);
        yield return Taunt();

        // 6. back to the fight, but harder
        if (maskAnim != null) maskAnim.SetBool("Tell", false);

        spawner.spawnInterval = phase2Interval;
        spawner.divesEnabled = true;
        foreach (AttackSpawner.AttackSettings a in spawner.attacks)
            a.speed *= phase2SpeedBoost;

        Transforming = false;
        health.minHP = 0;   // now he can actually die

        // only restart attacks if the player is still alive
        bool playerAlive = Health.Player != null && !Health.Player.IsDead;
        if (playerAlive)
        {
            spawner.enabled = true;
            if (typing != null) typing.enabled = true;
        }
    }

    IEnumerator Taunt()
    {
        if (tauntBox == null || tauntText == null || string.IsNullOrEmpty(tauntLine)) yield break;

        yield return new WaitForSeconds(tauntDelay);

        tauntText.text = tauntLine;
        tauntText.maxVisibleCharacters = 0;
        tauntBox.SetActive(true);

        for (int i = 0; i <= tauntLine.Length; i++)
        {
            tauntText.maxVisibleCharacters = i;               // typewriter: show one more letter each step
            if (i > 0) AudioManager.PlayVoice(tauntLine[i - 1], i);   // talking blip

            float wait = tauntCharDelay;
            if (i > 0 && ".,?!".IndexOf(tauntLine[i - 1]) >= 0) wait += tauntPunctuationPause;   // pause at punctuation like real talking
            yield return new WaitForSeconds(wait);
        }

        yield return new WaitForSeconds(tauntHold);
        tauntBox.SetActive(false);
    }

    // the TRUE FORM banner
    IEnumerator Banner()
    {
        if (phaseText == null) yield break;

        CanvasGroup g = phaseText.GetComponent<CanvasGroup>();
        if (g == null) g = phaseText.AddComponent<CanvasGroup>();
        Transform tr = phaseText.transform;

        g.alpha = 0f;
        phaseText.SetActive(true);
        yield return new WaitForSeconds(bannerDelay);

        // fade in while shrinking from a bit big to normal size
        float t = 0f;
        while (t < bannerFadeIn)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / bannerFadeIn);
            float e = 1f - (1f - p) * (1f - p);          // ease out, fast then slows down
            g.alpha = p;
            tr.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, e);
            yield return null;
        }
        g.alpha = 1f;
        tr.localScale = Vector3.one;

        yield return new WaitForSeconds(bannerHold);

        // fade out
        t = 0f;
        while (t < bannerFadeOut)
        {
            t += Time.deltaTime;
            g.alpha = 1f - Mathf.Clamp01(t / bannerFadeOut);
            yield return null;
        }

        phaseText.SetActive(false);
        g.alpha = 1f;
    }

    // fades the white flash image
    IEnumerator Fade(float from, float to, float time)
    {
        if (whiteFlash == null) yield break;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            SetFlash(Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        SetFlash(to);
    }

    void SetFlash(float alpha)
    {
        Color c = whiteFlash.color;
        c.a = alpha;
        whiteFlash.color = c;
    }

    // camera shake that gets weaker over time
    IEnumerator Shake(float time, float strength)
    {
        Transform cam = Camera.main.transform;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float s = strength * (1f - t / time);          // fades out
            cam.localPosition = camHome + (Vector3)(Random.insideUnitCircle * s);
            yield return null;
        }
        cam.localPosition = camHome;
    }

    // fades the moon from whatever color its at now to target
    IEnumerator FadeMoonTo(Color target, float time)
    {
        if (moon == null) yield break;
        Color start = moon.color;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            moon.color = Color.Lerp(start, target, t / time);
            yield return null;
        }
        moon.color = target;
    }

    // refills his hp 1 point at a time so the bar visibly fills back up
    IEnumerator RefillHealth()
    {
        int missing = health.maxHP - health.CurrentHP;
        if (missing <= 0) yield break;

        float step = refillTime / missing;
        for (int i = 0; i < missing; i++)
        {
            health.Heal(1);
            yield return new WaitForSeconds(step);
        }
    }
}