using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// the boss's brain for attacks. picks what to throw, which word, and where it comes from
public class AttackSpawner : MonoBehaviour
{
    [System.Serializable]
    public class AttackSettings
    {
        public AttackType type;
        public float laneY;
        public float speed;
        public int damage;
        public int weight;              // higher = shows up more often
        public string[] words;
        [System.NonSerialized] public List<string> bag = new List<string>();   // shuffle bag, see PickWord
    }

    // word lists live here in code so the inspector cant override them by accident (set in awake)
    static readonly string[] LightWords = {
        "ash", "beak", "claw", "dusk", "fan", "gale", "hush", "ink", "mask",
        "moon", "bough", "quill", "red", "talon", "verse", "wing" };

    static readonly string[] HeavyWords = {
        "arrogance", "blackwing", "crimson", "daitengu", "eclipse", "feathers",
        "hurricane", "inkstone", "karasu", "moonlight", "nightfall", "parchment",
        "ravenous", "shadows", "tempest", "whirlwind" };

    static readonly string[] PhraseWords = {
        "words are wind", "silence the poet", "no verse can save you",
        "your ink runs dry", "crimson moon", "beneath the red moon",
        "the crow descends", "storm of feathers", "wings of ash",
        "fear the mask", "kneel to the tengu", "the wind obeys me",
        "lost in the silver grass" };

    public WordAttack attackPrefab;
    public float spawnInterval = 2.5f;
    public float spawnX = 3.5f;
    public float laneGap = 0.8f;              // how much room a lane needs before another word can go in it
    public GameObject phraseWarning;          // the blinking !!!
    public float warningTime = 0.8f;
    public float blinkRate = 0.12f;
    public Animator[] bossAnimators;          // crow form and mask form
    public float phraseReleaseDelay = 0.5f;   // time until the slash frame of the attack anim
    public float firstAttackDelay = 1f;       // little pause before the first attack after turning on
    public float maxTimeWithoutPhrase = 20f;  // pity timer, forces a phrase if its been too long

    [Header("Phrase Caw")]
    public AudioClip phraseCaw;                       // crow caw before a phrase
    [Range(0f, 1f)] public float cawVolume = 0.7f;
    public AudioClip maskPhraseSound;                 // growl for phase two, if empty it just keeps the caw
    [Range(0f, 1f)] public float maskPhraseVolume = 0.7f;

    [Header("Phase Two: Diving Words")]
    public bool divesEnabled = false;                         // bossphases flips this on
    [Range(0f, 1f)] public float diveChance = 0.35f;          // how many light/heavy attacks dive instead
    [Range(0f, 1f)] public float straightDownShare = 0.4f;    // out of the dives, how many drop straight down
    public Vector2 diveSpawnXRange = new Vector2(-1f, 3f);    // where diagonal dives can start along the top
    public float diveSpawnY = 4.3f;                           // top of the screen
    public float diveTargetY = -2.3f;                         // samurai's head height, above all the lanes
    public float diveSpeedMultiplier = 1f;
    public float straightDownNudgeX = 0f;                     // small left/right fix if the drop looks off

    public AttackSettings[] attacks = {
        new AttackSettings { type = AttackType.Light,  laneY = -3.0f, speed = 1.6f, damage = 1, weight = 6 },
        new AttackSettings { type = AttackType.Heavy,  laneY = -4.4f, speed = 1.0f, damage = 3, weight = 3 },
        new AttackSettings { type = AttackType.Phrase, laneY = -3.7f, speed = 0.7f, damage = 5, weight = 1 }
    };

    private float timer;
    private float sincePhrase;          // seconds since the last phrase
    private bool warning;               // true while the !!! is blinking, pauses normal spawning

    void Awake()
    {
        // give each attack type its word list
        foreach (AttackSettings a in attacks)
        {
            if (a.type == AttackType.Light)  a.words = LightWords;
            if (a.type == AttackType.Heavy)  a.words = HeavyWords;
            if (a.type == AttackType.Phrase) a.words = PhraseWords;
            a.bag = new List<string>();
        }
    }

    void Start()
    {
        // when the boss gets hit he flinches, so hold off attacking for a sec
        Health h = GetComponent<Health>();
        if (h != null) h.OnDamaged += () => FlinchDelay(0.3f);
    }

    void OnEnable()
    {
        // set the timer so the first attack comes after firstAttackDelay instead of a full interval
        timer = spawnInterval - firstAttackDelay;
        sincePhrase = 0f;
    }

    void OnDisable()
    {
        // clean up if we get turned off mid warning
        StopAllCoroutines();
        warning = false;
        SetTell(false);
        if (phraseWarning != null) phraseWarning.SetActive(false);
    }

    void Update()
    {
        if (warning) return;

        sincePhrase += Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            TrySpawn();
        }
    }

    // pushes the next attack back a bit without ever making it sooner
    void FlinchDelay(float seconds)
    {
        timer = Mathf.Min(timer, spawnInterval - seconds);
    }

    void TrySpawn()
    {
        AttackSettings a = null;

        // pity timer: no phrase in a while? force one if its lane is free
        if (sincePhrase >= maxTimeWithoutPhrase && LaneIsClear(AttackType.Phrase))
            a = FindAttack(AttackType.Phrase);

        if (a == null) a = PickAttack();      // otherwise normal weighted random
        if (a == null) return;                // every lane is full, try again next time

        string w = PickWord(a);
        if (w == null) return;

        if (a.type == AttackType.Phrase)
        {
            sincePhrase = 0f;                 // reset the pity timer
            StartCoroutine(PhraseRoutine(a, w));
        }
        else if (ShouldDive()) SpawnDive(a, w);
        else Spawn(a, w);
    }

    bool ShouldDive()
    {
        if (!divesEnabled) return false;
        if (Random.value >= diveChance) return false;

        foreach (WordAttack w in WordAttack.All)          // only one diving word at a time so its readable
            if (w.Diving) return false;

        return true;
    }

    AttackSettings FindAttack(AttackType type)
    {
        foreach (AttackSettings a in attacks)
            if (a.type == type) return a;
        return null;
    }

    // phrases get a warning first: tell glow + sound + blinking !!!, then the slash
    IEnumerator PhraseRoutine(AttackSettings a, string w)
    {
        warning = true;
        SetTell(true);                  // boss glows
        PlayPhraseWarningSound();       // caw in phase one, growl in phase two

        float elapsed = 0f;
        while (elapsed < warningTime)
        {
            if (phraseWarning != null) phraseWarning.SetActive(!phraseWarning.activeSelf);   // blink
            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate;
        }

        if (phraseWarning != null) phraseWarning.SetActive(false);
        SetTell(false);                 // back to normal idle

        PlayAttack(a.type);             // start the sword swing
        yield return new WaitForSeconds(phraseReleaseDelay);

        warning = false;
        Spawn(a, w, false);             // phrase comes out right on the slash
    }

    void PlayPhraseWarningSound()
    {
        // divesEnabled only turns on in phase two so it doubles as a "which form is he" check
        if (divesEnabled && maskPhraseSound != null)
            AudioManager.Play(maskPhraseSound, maskPhraseVolume);
        else
            AudioManager.Play(phraseCaw, cawVolume);
    }

    void SetTell(bool on)
    {
        foreach (Animator anim in bossAnimators)
            if (anim != null && anim.isActiveAndEnabled) anim.SetBool("Tell", on);
    }

    void PlayAttack(AttackType type)
    {
        string trigger = "Attack" + type;   // AttackLight, AttackHeavy, AttackPhrase

        foreach (Animator anim in bossAnimators)
        {
            if (anim == null || !anim.isActiveAndEnabled) continue;

            // if a form doesnt have that attack anim just use the light one
            if (HasParam(anim, trigger)) anim.SetTrigger(trigger);
            else if (HasParam(anim, "AttackLight")) anim.SetTrigger("AttackLight");
        }
    }

    bool HasParam(Animator anim, string name)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.name == name) return true;
        return false;
    }

    // normal attack, comes in from the right in its lane
    void Spawn(AttackSettings a, string w, bool playAnimation = true)
    {
        Vector3 position = new Vector3(spawnX, a.laneY, 0f);
        WordAttack attack = Instantiate(attackPrefab, position, Quaternion.identity);
        attack.word = w;
        attack.type = a.type;
        attack.speed = a.speed;
        attack.Damage = a.damage;

        if (playAnimation) PlayAttack(attack.type);   // phrases already started theirs
    }

    // phase two: word shows up near the top and dives at the samurai's head
    void SpawnDive(AttackSettings a, string w)
    {
        WordAttack attack = Instantiate(attackPrefab, Vector3.zero, Quaternion.identity);
        attack.word = w;
        attack.type = a.type;
        attack.speed = a.speed * diveSpeedMultiplier;
        attack.Damage = a.damage;

        Vector3 start, target;

        if (Random.value < straightDownShare)
        {
            // straight down over the samurai. the word is positioned by its left edge
            // so shift it left by half its width to actually center it
            float centerX = Health.Player != null ? Health.Player.transform.position.x : attack.playerX;
            float width = attack.label.GetPreferredValues(w).x;
            float x = centerX - width / 2f + straightDownNudgeX;

            start = new Vector3(x, diveSpawnY, 0f);
            target = new Vector3(x, diveTargetY, 0f);
        }
        else
        {
            // diagonal: random spot along the top, aimed at the front of the samurai
            start = new Vector3(Random.Range(diveSpawnXRange.x, diveSpawnXRange.y), diveSpawnY, 0f);
            target = new Vector3(attack.playerX, diveTargetY, 0f);
        }

        attack.transform.position = start;
        attack.SetTarget(target, true);

        PlayAttack(attack.type);
    }

    // weighted random: like a raffle where light gets 6 tickets, heavy 3, phrase 1
    // types with a blocked lane dont get any tickets
    AttackSettings PickAttack()
    {
        int total = 0;
        foreach (AttackSettings a in attacks)
            if (LaneIsClear(a.type)) total += a.weight;

        if (total == 0) return null;

        int roll = Random.Range(0, total);
        foreach (AttackSettings a in attacks)
        {
            if (!LaneIsClear(a.type)) continue;
            if (roll < a.weight) return a;
            roll -= a.weight;
        }
        return null;
    }

    // lane is clear if the last word in it has moved far enough left (diving words dont count)
    bool LaneIsClear(AttackType type)
    {
        foreach (WordAttack w in WordAttack.All)
            if (!w.Diving && w.type == type && w.RightEdge > spawnX - laneGap) return false;
        return true;
    }

    // shuffle bag: every word comes out once before any repeat, like dealing from a deck
    string PickWord(AttackSettings a)
    {
        if (a.bag == null) a.bag = new List<string>();
        if (a.bag.Count == 0) RefillBag(a);

        for (int i = 0; i < a.bag.Count; i++)
        {
            string w = a.bag[i];
            // skip words whose first letter is already on screen so typing never gets confused
            if (!FirstLetterOnScreen(w[0]))
            {
                a.bag.RemoveAt(i);      // take the card out of the deck
                return w;
            }
        }
        return null;                    // everything left is blocked, try again next spawn
    }

    void RefillBag(AttackSettings a)
    {
        a.bag.AddRange(a.words);

        // fisher-yates shuffle: go backwards and swap each card with a random one before it
        for (int i = a.bag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = a.bag[i];
            a.bag[i] = a.bag[j];
            a.bag[j] = temp;
        }
    }

    bool FirstLetterOnScreen(char c)
    {
        foreach (WordAttack a in WordAttack.All)
            if (a.word[0] == c) return true;
        return false;
    }
}
