using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// handles the title menu + the options panel (also reused for options in the pause menu)
public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Hidden While Options Is Open")]
    public GameObject[] hideWhenSettingsOpen;   // title text, buttons, hint, best record

    bool[] wasActive;                           // remembers what was showing before options opened

    void Start()
    {
        // load the saved volumes, defaults to full if nothing saved yet
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    // new run from the title, so the intro should play again
    public void startGame()
    {
        IntroCutscene.Played = false;
        SceneManager.LoadScene("GameScene");
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        HideMenu();
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        RestoreMenu();
    }

    // sliders call these, audiomanager reads the saved values every frame
    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed");   // quit doesnt do anything in the editor so this shows it worked
        Application.Quit();
    }

    // hide the menu stuff but remember what was visible first
    void HideMenu()
    {
        wasActive = new bool[hideWhenSettingsOpen.Length];
        for (int i = 0; i < hideWhenSettingsOpen.Length; i++)
        {
            if (hideWhenSettingsOpen[i] == null) continue;
            wasActive[i] = hideWhenSettingsOpen[i].activeSelf;
            hideWhenSettingsOpen[i].SetActive(false);
        }
    }

    // put everything back the way it was (so stuff that was already hidden stays hidden)
    void RestoreMenu()
    {
        if (wasActive == null) return;
        for (int i = 0; i < hideWhenSettingsOpen.Length; i++)
            if (hideWhenSettingsOpen[i] != null)
                hideWhenSettingsOpen[i].SetActive(wasActive[i]);
    }
}
