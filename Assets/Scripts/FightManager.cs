using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

// runs the fight: countdown, pause menu, checking who died, showing results
public class FightManager : MonoBehaviour
{
    public static FightManager Instance;

    public enum State { Countdown, Fighting, Paused, Won, Lost }
    public State CurrentState { get; private set; } = State.Countdown;

    public AttackSpawner spawner;
    public TypingInput typing;
    public ResultsScreen results;
    public float victoryDelay = 1.5f;   // wait before results so the death anim can play
    public float defeatDelay = 3.2f;

    public TMP_Text countdownText;
    public float countdownStep = 0.8f;

    public GameObject pausePanel;
    public MainMenu pauseSettings;          // the options panel you open from pause
    public IntroCutscene intro;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;   // in case we came back from a paused game
        pausePanel.SetActive(false);

        // nothing happens until the countdown finishes
        spawner.enabled = false;
        typing.enabled = false;

        // play the intro first if there is one, it calls BeginCountdown when its done
        if (intro != null) intro.Play(BeginCountdown);
        else BeginCountdown();
    }

    public void BeginCountdown()
    {
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        countdownText.gameObject.SetActive(true);

        string[] steps = { "3", "2", "1" };
        foreach (string s in steps)
        {
            countdownText.text = s;
            yield return new WaitForSeconds(countdownStep);
        }

        countdownText.text = "FIGHT!";
        AudioManager.StartMusic();           // music kicks in with the fight
        CurrentState = State.Fighting;
        spawner.enabled = true;
        typing.enabled = true;

        yield return new WaitForSeconds(countdownStep);
        countdownText.gameObject.SetActive(false);
    }

    void Update()
    {
        // no pausing during cutscenes
        if (UnleashCutscene.Playing || IntroCutscene.Playing || BossPhases.Transforming) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (CurrentState == State.Fighting) Pause();
            else if (CurrentState == State.Paused)
            {
                // if options is open esc goes back to the pause menu first
                if (pauseSettings != null && pauseSettings.settingsPanel.activeSelf)
                    pauseSettings.CloseSettings();
                else
                    Resume();
            }
        }

        if (CurrentState != State.Fighting) return;

        // check if somebody died
        if (Health.Boss != null && Health.Boss.IsDead) EndFight(true);
        else if (Health.Player != null && Health.Player.IsDead) EndFight(false);
    }

    public void Pause()
    {
        CurrentState = State.Paused;
        Time.timeScale = 0f;   // freezes everything that uses deltaTime
        typing.enabled = false;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (pauseSettings != null && pauseSettings.settingsPanel.activeSelf)
            pauseSettings.CloseSettings();

        pausePanel.SetActive(false);
        typing.enabled = true;
        Time.timeScale = 1f;
        CurrentState = State.Fighting;
    }

    // retry button, reloads this scene (intro gets skipped since it already played)
    public void RestartFight()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    void EndFight(bool won)
    {
        CurrentState = won ? State.Won : State.Lost;

        spawner.enabled = false;
        typing.enabled = false;

        WordAttack.DestroyAll();   // clear any words still flying

        if (FightStats.Instance != null) FightStats.Instance.LogSummary();

        // wait a bit so the death anim plays before the results show up
        Invoke(nameof(ShowResults), won ? victoryDelay : defeatDelay);
    }

    void ShowResults()
    {
        AudioManager.PlayEnding();   // victory or defeat sound, same moment as the screen
        if (results != null) results.Show(CurrentState == State.Won);
    }
}
