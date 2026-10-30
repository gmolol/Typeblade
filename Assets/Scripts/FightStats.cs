using UnityEngine;

// keeps track of score, combo, accuracy, wpm etc for the results screen
public class FightStats : MonoBehaviour
{
    public static FightStats Instance;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public int WordsCompleted { get; private set; }
    public int CorrectKeys { get; private set; }
    public int Typos { get; private set; }
    public float FightTime { get; private set; }

    // correct keys out of all keys pressed. 100 if nothing was typed so it doesnt divide by 0
    public float Accuracy =>
        (CorrectKeys + Typos) == 0 ? 100f : 100f * CorrectKeys / (CorrectKeys + Typos);

    // standard wpm: every 5 letters counts as 1 word
    public float WPM =>
        FightTime <= 0f ? 0f : (CorrectKeys / 5f) / (FightTime / 60f);

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // only count time while actually fighting (not paused or in the countdown)
        if (FightManager.Instance != null &&
            FightManager.Instance.CurrentState == FightManager.State.Fighting)
        {
            FightTime += Time.deltaTime;
        }
    }

    public void CorrectKey()
    {
        CorrectKeys++;
        AudioManager.PlayKey();          // tick on every right letter
    }

    public void Typo()
    {
        Typos++;
        BreakCombo();
        AudioManager.PlayTypo();         // error beep
    }

    public void WordCompleted(string word)
    {
        WordsCompleted++;
        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;

        // longer words are worth more, and a higher combo adds a bonus
        Score += word.Length * 10 + Combo * 5;
    }

    public void BreakCombo()
    {
        Combo = 0;
    }

    // prints the stats to the console, was useful for testing
    public void LogSummary()
    {
        Debug.Log($"Score {Score} | Words {WordsCompleted} | Max Combo {MaxCombo} | " +
                  $"Accuracy {Accuracy:F1}% | WPM {WPM:F0} | Time {FightTime:F1}s");
    }
}
