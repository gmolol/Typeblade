using UnityEngine;
using TMPro;
using System.Collections.Generic;

public enum AttackType { Light, Heavy, Phrase, Unleash }

// one word the boss throws. moves at the samurai, and if you type it, it gets parried back at the boss
public class WordAttack : MonoBehaviour
{
    public static List<WordAttack> All = new List<WordAttack>();         // every word still coming at you
    public static List<WordAttack> Deflected = new List<WordAttack>();   // words already typed
    public static event System.Action OnParry;                           // for the parry sound + samurai anim
    public static event System.Action<WordAttack> OnUnleash;             // starts the unleash cutscene

    // incoming = flying at you, rushing = typed and zipping to the sword,
    // reflected = flying back at the boss, held = unleash waiting on the cutscene
    public enum State { Incoming, Rushing, Reflected, Held }
    public State CurrentState { get; private set; } = State.Incoming;

    public string word = "flame";
    public AttackType type = AttackType.Light;
    public float speed = 1f;
    public int Damage = 1;
    public float playerX = -7f;
    public float bossX = 5.2f;
    public float rushSpeed = 25f;
    public float reflectSpeed = 18f;
    public Color deflectColor = new Color(1f, 0.85f, 0.3f, 0.8f);
    public TMP_Text label;

    [Header("Effects")]
    public GameObject unleashHitEffect;           // burst when the unleash hits
    public float hitEffectYOffset = 0f;
    public GameObject reflectTrail;               // wind slash that carries reflected words
    public float trailOffsetX = 0.2f;             // gap between the front of the word and the slash

    public int Progress { get; private set; }     // how many letters typed so far
    public bool IsComplete => Progress == word.Length;
    public float RightEdge => transform.position.x + label.textBounds.size.x;

    // path stuff: where the word is flying to
    public bool Diving { get; private set; }      // true for the phase two words from the sky
    Vector3 hitPoint;
    bool hasTarget;

    // lane words go straight left at their own height, diving words get a custom target
    Vector3 HitPoint => hasTarget ? hitPoint : new Vector3(playerX, transform.position.y, 0f);

    public void SetTarget(Vector3 target, bool diving)
    {
        hitPoint = target;
        hasTarget = true;
        Diving = diving;
    }

    // add/remove from the list automatically so other scripts can loop over all the words
    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); Deflected.Remove(this); }
    void Start() { ResetWord(); }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Incoming:  UpdateIncoming();  break;
            case State.Rushing:   UpdateRushing();   break;
            case State.Reflected: UpdateReflected(); break;
            case State.Held:      break;               // waiting for the cutscene
        }
    }

    void UpdateIncoming()
    {
        if (type == AttackType.Unleash) return;   // unleash word just sits there

        Vector3 target = HitPoint;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (transform.position == target)              // made it to the samurai, he takes the hit
        {
            if (Health.Player != null) Health.Player.TakeDamage(Damage);
            if (FightStats.Instance != null) FightStats.Instance.BreakCombo();
            Destroy(gameObject);
        }
    }

    // after you type it the word zooms to the sword fast so the parry feels instant
    void UpdateRushing()
    {
        Vector3 target = HitPoint;
        transform.position = Vector3.MoveTowards(transform.position, target, rushSpeed * Time.deltaTime);

        if (transform.position == target)              // hits the sword
        {
            BecomeReflected();
            OnParry?.Invoke();
        }
    }

    void UpdateReflected()
    {
        transform.position += Vector3.right * reflectSpeed * Time.deltaTime;
        if (RightEdge < bossX) return;   // hasnt reached the boss yet

        // unleash: spawn the burst effect and let it do the damage in a few hits
        if (type == AttackType.Unleash && unleashHitEffect != null)
        {
            Vector3 hitPos = new Vector3(RightEdge, transform.position.y + hitEffectYOffset, 0f);
            GameObject fx = Instantiate(unleashHitEffect, hitPos, Quaternion.identity);

            BurstDamage burst = fx.GetComponent<BurstDamage>();
            if (burst != null)
            {
                burst.Begin(Damage);
                Destroy(gameObject);
                return;
            }
        }

        // everything else is just one normal hit
        if (Health.Boss != null) Health.Boss.TakeDamage(Damage);
        Destroy(gameObject);
    }

    // start flying back at the boss with the wind slash in front
    void BecomeReflected()
    {
        CurrentState = State.Reflected;

        if (reflectTrail != null)
        {
            GameObject trail = Instantiate(reflectTrail, transform);   // child so it moves and dies with the word
            trail.transform.localPosition = new Vector3(label.textBounds.size.x + trailOffsetX, 0f, 0f);
        }
    }

    // typinginput calls this when the word is fully typed
    public void Deflect()
    {
        All.Remove(this);
        Deflected.Add(this);
        label.text = word;
        label.color = deflectColor;
        label.GetComponent<MeshRenderer>().sortingOrder = 9;

        if (type == AttackType.Unleash)
        {
            if (OnUnleash != null)
            {
                CurrentState = State.Held;      // the cutscene calls Launch() later
                OnUnleash.Invoke(this);
            }
            else BecomeReflected();             // no cutscene in the scene, just fire it
        }
        else CurrentState = State.Rushing;
    }

    public void Launch()
    {
        BecomeReflected();
    }

    // clears every word, used when the fight ends
    public static void DestroyAll()
    {
        foreach (WordAttack w in All.ToArray()) Destroy(w.gameObject);
        foreach (WordAttack w in Deflected.ToArray()) Destroy(w.gameObject);
    }

    // checks the next letter. right letter = move forward, wrong = false
    public bool TryType(char c) { if (c != word[Progress]) return false; Progress++; UpdateLabel(); return true; }
    public void ResetWord() { Progress = 0; UpdateLabel(); }

    // typed part green, rest normal
    void UpdateLabel()
    {
        label.text = "<color=green>" + word.Substring(0, Progress) + "</color>" + word.Substring(Progress);
    }
}
