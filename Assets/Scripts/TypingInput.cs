using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

// reads the keyboard during the fight and types into the right word
public class TypingInput : MonoBehaviour
{

    private WordAttack target;   // the word youre locked onto
    private StringBuilder typed = new StringBuilder();

    // onTextInput gives real characters (handles shift, caps etc), collect them for update
    void OnEnable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnTextInput;
    }

    void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }

    void OnTextInput(char c)
    {
        typed.Append(c);
    }

    void Update()
    {
        string input = typed.ToString();
        typed.Clear();

        if (target == null) target = null;   // clears it out if the word got destroyed (hit you or got cleared)

        if (Keyboard.current.backspaceKey.wasPressedThisFrame) ClearTarget();   // backspace = drop the word

        foreach (char raw in input)
        {
            if (!char.IsLetter(raw) && raw != ' ') continue;   // spaces count bc phrases have them

            char c = char.ToLower(raw);

            // not locked on yet, pick the closest word starting with this letter
            if (target == null)
            {
                if (c == ' ') continue;
                target = FindClosestStartingWith(c);
            }

            // no word starts with that letter = typo
            if (target == null)
            {
                CountTypo();
                continue;
            }

            if (!target.TryType(c))
            {
                CountTypo();
                continue;
            }

            if (FightStats.Instance != null) FightStats.Instance.CorrectKey();

            // finished the word, parry it
            if (target.IsComplete)
            {
                if (FightStats.Instance != null) FightStats.Instance.WordCompleted(target.word);
                if (BladeMeter.Instance != null) BladeMeter.Instance.WordCompleted(target.type);
                target.Deflect();
                target = null;
            }
        }
    }

    void CountTypo()
    {
        if (FightStats.Instance != null) FightStats.Instance.Typo();
        if (BladeMeter.Instance != null) BladeMeter.Instance.Typo();
    }

    // closest = smallest x, the one nearest the samurai (most dangerous)
    WordAttack FindClosestStartingWith(char c)
    {
        WordAttack best = null;
        foreach (WordAttack w in WordAttack.All)
        {
            if (w.word[0] != c) continue;

            if (best == null || w.transform.position.x < best.transform.position.x)
                best = w;
        }
        return best;
    }

    void ClearTarget()
    {
        if (target != null) target.ResetWord();
        target = null;
    }
}
