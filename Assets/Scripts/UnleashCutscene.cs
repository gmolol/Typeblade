using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// the unleash cutscene: world freezes, screen dims, camera zooms on the samurai, then the strike
public class UnleashCutscene : MonoBehaviour
{
    public static bool Playing { get; private set; }

    [Header("References")]
    public Animator samuraiAnimator;
    public SpriteRenderer samuraiSprite;
    public SpriteRenderer dimOverlay;
    public Image whiteFlash;
    public GameObject unleashText;
    public TypingInput typing;

    [Header("Timing (real seconds, matches Samples 12)")]
    public float domeTime = 0.75f;     // frame 9 / 12
    public float strikeTime = 1.58f;   // frame 19 / 12
    public float zoomInTime = 0.6f;

    [Header("Look")]
    public float dimAlpha = 0.75f;
    public float zoomAmount = 0.55f;   // 0.55 = camera shows 55% as much (zoomed in)
    public Vector2 focusOffset = new Vector2(0f, 0.5f);
    public float domeShake = 0.08f;
    public float strikeShake = 0.3f;
    public float flashOutTime = 0.4f;

    Camera cam;
    Vector3 camHome;
    float sizeHome;

    // wordattack fires OnUnleash when you finish typing "unleash"
    void OnEnable()  { WordAttack.OnUnleash += Begin; }
    void OnDisable() { WordAttack.OnUnleash -= Begin; }

    void Start()
    {
        // remember the normal camera so we can snap back after
        cam = Camera.main;
        camHome = cam.transform.localPosition;
        sizeHome = cam.orthographicSize;
        if (unleashText != null) unleashText.SetActive(false);
        SetDim(0f);
    }

    void Begin(WordAttack word)
    {
        StartCoroutine(Cutscene(word));
    }

    IEnumerator Cutscene(WordAttack word)
    {
        // wait if the game is paused or in a hit stop
        yield return new WaitUntil(() => Time.timeScale == 1f);

        Playing = true;
        bool typingWasOn = typing != null && typing.enabled;
        if (typing != null) typing.enabled = false;

        // freeze: timescale 0 stops everything, but the samurai animator runs on unscaled time so he keeps moving
        Time.timeScale = 0f;
        samuraiAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        samuraiAnimator.SetTrigger("Unleash");
        AudioManager.PlayUnleash();

        // bring the samurai and word above the dark overlay (save the old order to put back later)
        int samuraiOrder = samuraiSprite.sortingOrder;
        MeshRenderer wordRenderer = word.label.GetComponent<MeshRenderer>();
        int wordOrder = wordRenderer.sortingOrder;
        samuraiSprite.sortingOrder = 20;   // above the dim
        wordRenderer.sortingOrder = 21;
        word.label.enabled = false;        // hide the word during the cutscene

        float start = Time.unscaledTime;   // everything below is timed from here

        // darken + zoom in
        Vector3 focus = ClampToHomeView(samuraiSprite.transform.position + (Vector3)focusOffset);
        yield return Over(zoomInTime, p =>
        {
            float e = Mathf.SmoothStep(0f, 1f, p);
            SetDim(dimAlpha * e);
            cam.orthographicSize = Mathf.Lerp(sizeHome, sizeHome * zoomAmount, e);
            cam.transform.localPosition = Vector3.Lerp(camHome, focus, e);
        });

        // dome burst: UNLEASH banner + little flash
        yield return new WaitUntil(() => Time.unscaledTime - start >= domeTime);
        if (unleashText != null) unleashText.SetActive(true);
        StartCoroutine(FlashPop());
        StartCoroutine(Shake(0.3f, domeShake, focus));

        // strike
        yield return new WaitUntil(() => Time.unscaledTime - start >= strikeTime);

        SetFlash(1f);                                   // full white hides the camera snapping back
        if (unleashText != null) unleashText.SetActive(false);
        cam.orthographicSize = sizeHome;
        cam.transform.localPosition = camHome;
        SetDim(0f);
        samuraiSprite.sortingOrder = samuraiOrder;
        wordRenderer.sortingOrder = wordOrder;

        // unfreeze and send the word flying at the boss
        Time.timeScale = 1f;
        samuraiAnimator.updateMode = AnimatorUpdateMode.Normal;
        word.label.enabled = true;
        word.Launch();

        StartCoroutine(Shake(0.4f, strikeShake, camHome));
        if (typing != null && typingWasOn) typing.enabled = true;
        Playing = false;

        yield return Over(flashOutTime, p => SetFlash(1f - p));
    }

    // runs step(0 to 1) every frame for 'time' real seconds
    // uses unscaled time since the game is frozen during this
    IEnumerator Over(float time, System.Action<float> step)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            step(Mathf.Clamp01(t / time));
            yield return null;
        }
        step(1f);
    }

    // quick half white flash in and out
    IEnumerator FlashPop()
    {
        yield return Over(0.05f, p => SetFlash(0.6f * p));
        yield return Over(0.2f, p => SetFlash(0.6f * (1f - p)));
    }

    IEnumerator Shake(float time, float strength, Vector3 center)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float s = strength * (1f - t / time);
            cam.transform.localPosition = center + (Vector3)(Random.insideUnitCircle * s);
            yield return null;
        }
        cam.transform.localPosition = center;
    }

    // keeps the zoomed camera inside the normal view so it never shows past the background edges
    Vector3 ClampToHomeView(Vector3 target)
    {
        float aspect = cam.aspect;
        float bigH = sizeHome;               float bigW = sizeHome * aspect;
        float smallH = sizeHome * zoomAmount; float smallW = smallH * aspect;

        // how far the small view can move before its edge goes past the big view's edge
        float maxX = bigW - smallW;
        float maxY = bigH - smallH;

        target.x = Mathf.Clamp(target.x, camHome.x - maxX, camHome.x + maxX);
        target.y = Mathf.Clamp(target.y, camHome.y - maxY, camHome.y + maxY);
        target.z = camHome.z;
        return target;
    }

    void SetDim(float a)
    {
        if (dimOverlay == null) return;
        Color c = dimOverlay.color; c.a = a; dimOverlay.color = c;
    }

    void SetFlash(float a)
    {
        if (whiteFlash == null) return;
        Color c = whiteFlash.color; c.a = a; whiteFlash.color = c;
    }
}
