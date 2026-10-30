using UnityEngine;
using TMPro;

// the "x12 COMBO" counter. pops on every word and changes color the higher it gets
public class ComboDisplay : MonoBehaviour
{
    public TMP_Text label;
    public int showAt = 3;                // dont show tiny combos
    public float punchScale = 1.4f;       // how big it pops on each new word
    public float settleSpeed = 8f;        // how fast it shrinks back

    [Header("Tiers")]
    public int hotAt = 10;
    public int blazingAt = 25;
    public Color normalColor = Color.white;
    public Color hotColor = new Color(1f, 0.85f, 0.3f);      // gold
    public Color blazingColor = new Color(0.9f, 0.2f, 0.2f); // crimson

    int lastCombo;
    float scale = 1f;
    bool waitingForNextWord;              // true after a cutscene until the combo goes up again

    void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        label.enabled = false;
    }

    void Update()
    {
        int combo = FightStats.Instance != null ? FightStats.Instance.Combo : 0;

        bool increased = combo > lastCombo;
        if (combo != lastCombo)
        {
            if (increased) scale = punchScale;   // pop every time it goes up
            lastCombo = combo;
        }

        // cutscenes hide the counter until the next correct word
        bool cutscene = UnleashCutscene.Playing || BossPhases.Transforming;
        if (cutscene) waitingForNextWord = true;
        else if (increased) waitingForNextWord = false;

        bool fighting = FightManager.Instance != null
                        && FightManager.Instance.CurrentState == FightManager.State.Fighting;

        bool show = combo >= showAt
                    && fighting
                    && !cutscene
                    && !waitingForNextWord;

        label.enabled = show;
        if (!show) return;

        label.text = "x" + combo + "\n<size=45%>COMBO</size>";

        if (combo >= blazingAt)  label.color = blazingColor;
        else if (combo >= hotAt) label.color = hotColor;
        else                     label.color = normalColor;

        // lerp back toward normal size every frame, thats the pop then settle
        scale = Mathf.Lerp(scale, 1f, settleSpeed * Time.deltaTime);
        transform.localScale = Vector3.one * scale;
    }
}
