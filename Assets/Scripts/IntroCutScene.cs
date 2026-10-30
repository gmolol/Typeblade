using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// the opening: fade in, title card, samurai runs in, tengu talks, you type "you" back
public class IntroCutscene : MonoBehaviour
{
    public static bool Playing { get; private set; }
    public static bool Played;   // mainmenu sets this back to false when a new run starts

    [Header("Run-in")]
    public Transform player;
    public Animator samuraiAnimator;
    public float startX = -11f;              // off screen to the left
    public float runSpeed = 4f;

    [Header("Opening")]
    public Image fadeBlack;
    public CanvasGroup titleCard;
    public float fadeInTime = 1.2f;
    public float titleFadeTime = 0.6f;
    public float titleHoldTime = 1.6f;

    [Header("Boss")]
    public Animator bossAnimator;            // crow form (leave empty for no glow)

    [Header("Dialogue")]
    public GameObject introPanel;
    public TMP_Text dialogueText;
    [TextArea] public string bossLine = "A poet... with a sword? Words are wind, little poet. What could they possibly cut?";
    public float charDelay = 0.05f;          // typewriter speed
    public float punctuationPause = 0.3f;    // extra pause after . , ? !
    public float lastSentenceDelay = 0.07f;  // last sentence types slower for drama
    public float pauseBeforeLine = 1.0f;

    [Header("Reply")]
    public GameObject replyGroup;
    public Transform replyWord;              // the "you" text
    public GameObject hintText;              // "type your answer"
    public float pauseBeforeReply = 0.8f;    // silence after he finishes talking
    public float slamTime = 0.8f;            // how long "you" takes to settle in
    public float slamScale = 1.15f;          // how big it starts
    public float slamShake = 0f;
    public float hintDelay = 0.6f;
    public float hintFadeTime = 0.6f;
    public float pauseAfterReply = 0.4f;

    System.Action onDone;   // what to run when the intro ends (fightmanager's countdown)
    bool replied;

    void Awake()
    {
        if (introPanel != null) introPanel.SetActive(false);
        if (titleCard != null) titleCard.alpha = 0f;
        SetBlack(Played ? 0f : 1f);          // first run starts black, retries start clear
    }

    public void Play(System.Action whenDone)
    {
        onDone = whenDone;

        if (Played)                          // retry: skip straight to the countdown
        {
            SetBlack(0f);
            onDone?.Invoke();
            return;
        }
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        Playing = true;

        // 0. opening: fade from black + title card (both run at the same time as the run in)
        StartCoroutine(Fade(1f, 0f, fadeInTime, SetBlack));
        StartCoroutine(TitleCard());

        // 1. run in
        Vector3 end = player.position;       // where he normally stands
        Vector3 pos = end;
        pos.x = startX;
        player.position = pos;               // start him off screen

        samuraiAnimator.SetBool("Running", true);
        while (player.position.x < end.x)
        {
            pos.x = Mathf.MoveTowards(player.position.x, end.x, runSpeed * Time.deltaTime);
            player.position = pos;
            yield return null;
        }
        samuraiAnimator.SetBool("Running", false);

        // 2. boss talks
        yield return new WaitForSeconds(pauseBeforeLine);
        if (bossAnimator != null) bossAnimator.SetBool("Tell", true);

        introPanel.SetActive(true);
        replyGroup.SetActive(false);
        dialogueText.text = bossLine;

        // find where the last sentence starts (right after the second to last . ? or !)
        string trimmed = bossLine.TrimEnd('.', '?', '!');
        int slowFrom = trimmed.LastIndexOfAny(new[] { '.', '?', '!' }) + 1;

        for (int i = 0; i <= bossLine.Length; i++)
        {
            dialogueText.maxVisibleCharacters = i;   // typewriter effect
            if (i > 0) AudioManager.PlayVoice(bossLine[i - 1], i);   // talking blip for the letter that just showed up

            float wait = (i > slowFrom) ? lastSentenceDelay : charDelay;
            if (i > 0 && ".,?!".IndexOf(bossLine[i - 1]) >= 0) wait += punctuationPause;
            yield return new WaitForSeconds(wait);
        }

        if (bossAnimator != null) bossAnimator.SetBool("Tell", false);

        // 3. the reply
        yield return new WaitForSeconds(pauseBeforeReply);   // a beat of silence

        replied = false;
        if (hintText != null) hintText.SetActive(false);
        replyGroup.SetActive(true);

        yield return SlamIn();                                // "you" fades in
        if (slamShake > 0f) StartCoroutine(Shake(0.25f, slamShake));

        float waited = 0f;                                    // hint fades in a bit later...
        while (waited < hintDelay && !replied)                // ...unless they already started typing
        {
            waited += Time.deltaTime;
            yield return null;
        }
        if (hintText != null && !replied) StartCoroutine(FadeInHint());

        yield return new WaitUntil(() => replied);           // wait for the player to type "you"

        // 4. close it and start the fight
        introPanel.SetActive(false);
        yield return new WaitForSeconds(pauseAfterReply);

        Played = true;
        Playing = false;
        onDone?.Invoke();
    }

    IEnumerator FadeInHint()
    {
        CanvasGroup g = hintText.GetComponent<CanvasGroup>();
        if (g == null) g = hintText.AddComponent<CanvasGroup>();

        g.alpha = 0f;
        hintText.SetActive(true);
        yield return Fade(0f, 1f, hintFadeTime, a => g.alpha = a);
    }

    IEnumerator TitleCard()
    {
        if (titleCard == null) yield break;
        yield return Fade(0f, 1f, titleFadeTime, a => titleCard.alpha = a);
        yield return new WaitForSeconds(titleHoldTime);
        yield return Fade(1f, 0f, titleFadeTime, a => titleCard.alpha = a);
    }

    // general fade: moves a number from 'from' to 'to' over 'time' and hands each step to 'apply'
    // so the same function works for the black screen, title card, hint etc
    IEnumerator Fade(float from, float to, float time, System.Action<float> apply)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            apply(Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        apply(to);
    }

    void SetBlack(float alpha)
    {
        if (fadeBlack == null) return;
        Color c = fadeBlack.color;
        c.a = alpha;
        fadeBlack.color = c;
    }

    // "you" starts a little big and see through, then settles to normal
    IEnumerator SlamIn()
    {
        CanvasGroup group = replyGroup.GetComponent<CanvasGroup>();
        if (group == null) group = replyGroup.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < slamTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / slamTime);
            float e = 1f - (1f - p) * (1f - p);              // ease out so it settles gently

            if (replyWord != null) replyWord.localScale = Vector3.one * Mathf.Lerp(slamScale, 1f, e);
            group.alpha = p;
            yield return null;
        }

        if (replyWord != null) replyWord.localScale = Vector3.one;
        group.alpha = 1f;
    }

    IEnumerator Shake(float time, float strength)
    {
        Transform cam = Camera.main.transform;
        Vector3 home = cam.localPosition;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float s = strength * (1f - t / time);
            cam.localPosition = home + (Vector3)(Random.insideUnitCircle * s);
            yield return null;
        }
        cam.localPosition = home;
    }

    // the reply word's menuword calls this once "you" is fully typed
    public void OnReply()
    {
        replied = true;
    }
}