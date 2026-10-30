using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// shows the BEST score + little rank seal on the title screen
public class BestScoreDisplay : MonoBehaviour
{
    public GameObject recordGroup;        // hidden if theres no record yet
    public TMP_Text scoreText;            // "BEST: 725"
    public TMP_Text rankText;             // the letter in the mini seal

    [Header("Seal Colors")]
    public Color colorS = new Color(1f, 0.85f, 0.3f);        // gold
    public Color colorOther = new Color(0.96f, 0.9f, 0.78f); // cream for a, b, c
    public Color colorF = new Color(0.55f, 0.55f, 0.55f);    // ash gray

    void Start()
    {
        Refresh();
    }

    void Refresh()
    {
        if (!HighScores.HasRecord)
        {
            if (recordGroup != null) recordGroup.SetActive(false);
            return;
        }

        if (recordGroup != null) recordGroup.SetActive(true);

        if (scoreText != null) scoreText.text = "BEST: " + HighScores.BestScore;

        if (rankText != null)
        {
            string g = HighScores.BestGrade;
            rankText.text = g;
            // s is gold, f is gray, everything else cream
            rankText.color = g == "S" ? colorS
                           : g == "F" ? colorF
                           : colorOther;
        }
    }

    // right click the component in the inspector to wipe scores while testing
    [ContextMenu("Reset High Scores")]
    void ResetScores()
    {
        HighScores.Clear();
        Refresh();
    }

    void Update()
    {
        // secret reset in the actual build: ctrl + shift + r on the title screen
        var kb = Keyboard.current;
        if (kb != null && kb.ctrlKey.isPressed && kb.shiftKey.isPressed && kb.rKey.wasPressedThisFrame)
        {
            HighScores.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // reload so the badge goes away
        }
    }
}
