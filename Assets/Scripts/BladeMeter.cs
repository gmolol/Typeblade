using UnityEngine;
using UnityEngine.UI;

// the blade meter. fills up as you finish words, when its full the "unleash" word shows up
public class BladeMeter : MonoBehaviour
{
    public static BladeMeter Instance;

    public float max = 100f;
    public float lightFill = 5f;      // how much each word type adds
    public float heavyFill = 15f;
    public float phraseFill = 25f;
    public float typoDrain = 2f;      // typos take a little away

    public Image fill;
    public WordAttack unleashPrefab;
    public Vector3 unleashPosition = new Vector3(-8.2f, -2f, 0f);
    public int unleashDamage = 20;
    public Color unleashColor = new Color(1f, 0.8f, 0.2f);

    public float Current { get; private set; }
    public bool IsFull => Current >= max;

    private WordAttack unleashWord;   // the unleash word on screen, null if there isnt one

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (fill != null) fill.fillAmount = Current / max;   // 0 to 1 for the bar
    }

    public void WordCompleted(AttackType type)
    {
        // typed unleash, so empty the meter
        if (type == AttackType.Unleash)
        {
            Current = 0f;
            unleashWord = null;
            return;
        }

        float amount = type == AttackType.Light ? lightFill :
            type == AttackType.Heavy ? heavyFill : phraseFill;

        Current = Mathf.Min(Current + amount, max);   // cant go over max

        if (IsFull && unleashWord == null) SpawnUnleash();
    }

    public void Typo()
    {
        if (IsFull) return;   // once its full a typo cant take it away
        Current = Mathf.Max(Current - typoDrain, 0f);
    }

    // spawns the gold "unleash" word next to the samurai, it doesnt move (speed 0)
    void SpawnUnleash()
    {
        unleashWord = Instantiate(unleashPrefab, unleashPosition, Quaternion.identity);
        unleashWord.word = "unleash";
        unleashWord.type = AttackType.Unleash;
        unleashWord.speed = 0f;
        unleashWord.Damage = unleashDamage;
        unleashWord.label.color = unleashColor;
    }
}
