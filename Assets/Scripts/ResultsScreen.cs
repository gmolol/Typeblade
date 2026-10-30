using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// end screen: victory/defeat, stats, the grade seal, and the haiku or taunt
public class ResultsScreen : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text statsText;
    public TMP_Text gradeText;            // big letter in the seal
    public TMP_Text quoteText;            // haiku or taunt
    public string newBestColor = "#FFD94D";   // gold for the NEW BEST tag

    [Header("Quotes")]
    [TextArea(3, 4)] public string victoryHaiku =
        "The crimson moon fades\nmy words were not wind after all\nfeathers on the grass";
    public string victoryAuthor = "the poet";
    public string[] defeatTaunts = {
        "\"Words are wind, little poet.\"",
        "\"Your ink runs dry.\"",
        "\"Silence, at last.\""
    };
    public string defeatAuthor = "the tengu";

    [Header("Grade Cutoffs")]
    public int minWordsForGrade = 5;                                  // less than this = automatic c (0 = f)
    public float accuracyS = 97f, accuracyA = 92f, accuracyB = 85f;   // worth 3 / 2 / 1 points
    public float wpmHigh = 35f, wpmMid = 25f;                         // worth 2 / 1
    public int comboHigh = 15, comboMid = 8;                          // worth 2 / 1

    [Header("Grade Colors")]
    public Color colorS = new Color(1f, 0.85f, 0.3f);      // gold
    public Color colorA = new Color(0.96f, 0.9f, 0.78f);   // cream
    public Color colorB = new Color(0.96f, 0.9f, 0.78f);
    public Color colorC = new Color(0.96f, 0.9f, 0.78f);
    public Color colorF = new Color(0.55f, 0.55f, 0.55f);  // ash gray

    void Start()
    {
        panel.SetActive(false);
    }

    // fightmanager calls this when the fight ends
    public void Show(bool won)
    {
        titleText.text = won ? "VICTORY" : "DEFEAT";

        FightStats s = FightStats.Instance;

        // grade + high score first so the stats can show NEW BEST
        string grade = CalculateGrade(s, won);

        bool newBest = false;
        if (s.WordsCompleted > 0) newBest = HighScores.Submit(s.Score, grade);   // dont save empty runs

        // stats
        bool typedAnything = (s.CorrectKeys + s.Typos) > 0;
        string accuracy = typedAnything ? $"{s.Accuracy:F1}%" : "—";   // dash instead of a fake 100%

        string scoreLine = $"Score: {s.Score}";
        if (newBest)
        {
            // invisible copy of the tag on the left balances the gold one on the right
            // so "Score: ..." stays centered like the other lines
            string tag = "<size=80%>NEW BEST!</size>";
            scoreLine = $"<color=#00000000>{tag}</color>  " + scoreLine + $"  <color={newBestColor}><voffset=0.125em>{tag}</voffset></color>";
        }

        statsText.text =
            scoreLine + "\n" +
            $"Accuracy: {accuracy}\n" +
            $"WPM: {s.WPM:F0}\n" +
            $"Max Combo: {s.MaxCombo}\n" +
            $"Words: {s.WordsCompleted}";

        // grade seal
        if (gradeText != null)
        {
            gradeText.text = grade;
            gradeText.color = grade == "S" ? colorS
                            : grade == "A" ? colorA
                            : grade == "B" ? colorB
                            : grade == "C" ? colorC
                            : colorF;
        }

        // haiku if you won, random taunt if you lost, plus who said it
        if (quoteText != null)
        {
            string quote, author;

            if (won)
            {
                quote = victoryHaiku;
                author = victoryAuthor;
            }
            else
            {
                quote = defeatTaunts.Length > 0
                      ? defeatTaunts[Random.Range(0, defeatTaunts.Length)]
                      : "";
                author = defeatAuthor;
            }

            quoteText.text = "<i>" + quote + "</i>\n<size=75%>— " + author + "</size>";
        }

        panel.SetActive(true);
    }

    // points system: accuracy, wpm and combo each give points, total points = letter grade
    string CalculateGrade(FightStats s, bool won)
    {
        if (s.WordsCompleted == 0) return "F";                  // didnt fight at all
        if (s.WordsCompleted < minWordsForGrade) return "C";   // barely fought

        int points = 0;

        if (s.Accuracy >= accuracyS) points += 3;
        else if (s.Accuracy >= accuracyA) points += 2;
        else if (s.Accuracy >= accuracyB) points += 1;

        if (s.WPM >= wpmHigh) points += 2;
        else if (s.WPM >= wpmMid) points += 1;

        if (s.MaxCombo >= comboHigh) points += 2;
        else if (s.MaxCombo >= comboMid) points += 1;

        // max is 7 points
        string grade = points >= 6 ? "S"
                     : points >= 4 ? "A"
                     : points >= 2 ? "B"
                     : "C";

        if (!won && (grade == "S" || grade == "A")) grade = "B";   // losing caps you at b
        return grade;
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
