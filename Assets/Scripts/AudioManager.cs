using System.Collections;
using UnityEngine;

// all the music + sound effects go through here. one per scene
public class AudioManager : MonoBehaviour
{
    // static so any script can do AudioManager.Play(...) without a reference
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource keySource;      // own source so changing the key pitch doesnt mess with other sounds

    [Header("Music")]
    public AudioClip music;
    [Range(0f, 1f)] public float musicBaseVolume = 0.3f;
    public bool playMusicOnStart = true;   // on for the title, off for the fight (starts at FIGHT!)

    [Header("Sound Effects")]
    public AudioClip key;
    public AudioClip typo;
    public AudioClip parry;
    public AudioClip bossHit;
    public AudioClip playerHit;
    public AudioClip playerDeath;
    public AudioClip bossAttack;
    public AudioClip unleash;
    public AudioClip transformation;
    public AudioClip victory;
    public AudioClip defeat;

    [Header("Per-Sound Volume")]
    [Range(0f, 1f)] public float keyVolume = 0.3f;
    [Range(0f, 1f)] public float typoVolume = 0.6f;
    [Range(0f, 1f)] public float hitVolume = 0.8f;
    [Range(0f, 1f)] public float parryVolume = 0.4f;
    [Range(0f, 1f)] public float unleashVolume = 0.8f;
    [Range(0f, 1f)] public float deathVolume = 0.8f;

    [Header("Boss Death")]
    public AudioClip bossDeath;                // screech when the boss actually dies
    [Range(0f, 1f)] public float bossDeathVolume = 0.8f;

    [Header("Ending Stings (play when the results screen appears)")]
    [Range(0f, 1f)] public float victoryVolume = 0.8f;
    [Range(0f, 1f)] public float defeatVolume = 0.8f;
    public bool fadeMusicOnEnd = true;         // fade the fight music out after someone dies
    public float musicFadeTime = 1.5f;

    [Header("Unleash Sequence (seconds after the cutscene starts)")]
    [Range(0f, 2f)] public float unleashDelay = 0.5f;      // windup ring
    public AudioClip unleashBoom;                           // drum hit when the dome bursts
    public float boomTime = 1.13f;
    [Range(0f, 1f)] public float boomVolume = 0.8f;
    public AudioClip unleashSpin;                           // sword spin whoosh
    public float spinTime = 2.0f;
    [Range(0f, 1f)] public float spinVolume = 0.7f;
    public AudioClip unleashDraw;                           // unsheathing
    public float drawTime = 3.0f;
    [Range(0f, 1f)] public float drawVolume = 0.8f;
    public AudioClip unleashSlash;                          // the big slash on the strike
    public float slashTime = 3.63f;
    [Range(0f, 1f)] public float slashVolume = 0.8f;

    [Header("Transformation (seconds after phase two begins)")]
    public float transformationDelay = 0f;     // to line the impact up with the white flash
    [Range(0f, 1f)] public float transformationVolume = 0.8f;
    public AudioClip transformationLaugh;      // boss laughs when the mask form shows up
    public float laughDelay = 1.5f;
    [Range(0f, 1f)] public float laughVolume = 0.8f;

    [Header("Countdown")]
    public TMPro.TMP_Text countdownText;       // the 3 2 1 FIGHT! text
    public AudioClip countdownTick;            // on 3, 2, 1
    public AudioClip countdownFight;           // gong on FIGHT!
    [Range(0f, 1f)] public float countdownVolume = 0.8f;

    [Header("Talking Voice")]
    public AudioClip voiceBlip;                // little blip per letter when the tengu talks (undertale style)
    [Range(0f, 1f)] public float voiceVolume = 0.5f;
    [Range(0.5f, 2f)] public float voicePitch = 1f;
    [Range(0f, 0.3f)] public float voicePitchVariation = 0.08f;   // random pitch so it sounds like talking
    public int voiceEveryNLetters = 2;         // 1 = every letter, 2 = every other letter

    [Header("Key Tick Pitch")]
    [Range(0.5f, 3f)] public float keyPitch = 1.8f;
    [Range(0f, 0.3f)] public float keyPitchVariation = 0.1f;   // random pitch so it doesnt sound robotic

    float sfxVolume = 1f;
    bool wasTransforming;
    bool bossDeathPlayed;
    bool defeatPlayed;
    AttackSpawner spawner;
    string lastCountdown = "";
    AudioSource voiceSource;   // made in code so it always exists and is turned on

    void Awake()
    {
        Instance = this;

        // own audio source just for the talking blips (pitch changes wont touch other sounds)
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        if (sfxSource != null) voiceSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;

        ApplyVolumes();
    }

    void Start()
    {
        if (playMusicOnStart) StartMusic();

        // listen for parries and hits so the right sound plays without those scripts knowing about audio
        WordAttack.OnParry += OnParry;
        if (Health.Boss != null) Health.Boss.OnDamaged += OnBossHit;
        if (Health.Player != null) Health.Player.OnDamaged += OnPlayerHit;
    }

    void Update()
    {
        ApplyVolumes();   // every frame so the options sliders update live

        // transformation sounds, the frame phase two starts (false -> true)
        if (BossPhases.Transforming && !wasTransforming)
        {
            StartCoroutine(PlayAfter(transformation, transformationVolume, transformationDelay));
            StartCoroutine(PlayAfter(transformationLaugh, laughVolume, laughDelay));
        }
        wasTransforming = BossPhases.Transforming;

        WatchCountdown();

        // boss death screech, once, only in phase two
        // (hp hits zero at the end of phase one too but thats the transformation not a real death)
        if (spawner == null) spawner = FindAnyObjectByType<AttackSpawner>();
        bool phaseTwo = spawner != null && spawner.divesEnabled;
        if (!bossDeathPlayed && phaseTwo && !BossPhases.Transforming
            && Health.Boss != null && Health.Boss.IsDead)
        {
            bossDeathPlayed = true;
            Play(bossDeath, bossDeathVolume);
            if (fadeMusicOnEnd) StartCoroutine(FadeOutMusic());
        }

        // player died, fade the music so the defeat sound stands out later
        if (!defeatPlayed && !bossDeathPlayed && Health.Player != null && Health.Player.IsDead)
        {
            defeatPlayed = true;
            if (fadeMusicOnEnd) StartCoroutine(FadeOutMusic());
        }
    }

    // fightmanager calls this right when the results screen shows up
    // so the sound and the screen always match
    public static void PlayEnding()
    {
        if (Instance == null) return;
        bool won = Health.Boss != null && Health.Boss.IsDead;
        if (won) Play(Instance.victory, Instance.victoryVolume);
        else     Play(Instance.defeat, Instance.defeatVolume);
    }

    // lowers the music to nothing over musicFadeTime
    IEnumerator FadeOutMusic()
    {
        float startBase = musicBaseVolume;
        float t = 0f;
        while (t < musicFadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicBaseVolume = Mathf.Lerp(startBase, 0f, t / musicFadeTime);
            yield return null;
        }
        musicBaseVolume = 0f;
    }

    // plays a sound whenever the countdown text changes (3, 2, 1, FIGHT!)
    // this way fightmanager didnt need any audio code
    void WatchCountdown()
    {
        if (countdownText == null) return;

        string now = countdownText.isActiveAndEnabled ? countdownText.text : "";
        if (now == lastCountdown) return;
        lastCountdown = now;

        if (string.IsNullOrWhiteSpace(now)) return;
        if (now.ToUpper().Contains("FIGHT")) Play(countdownFight, countdownVolume);
        else Play(countdownTick, countdownVolume);
    }

    void OnDestroy()
    {
        // unsubscribe so nothing tries to call this after the scene unloads
        WordAttack.OnParry -= OnParry;
        if (Health.Boss != null) Health.Boss.OnDamaged -= OnBossHit;
        if (Health.Player != null) Health.Player.OnDamaged -= OnPlayerHit;
    }

    public static void StartMusic()
    {
        if (Instance == null || Instance.music == null || Instance.musicSource == null) return;
        if (Instance.musicSource.isPlaying) return;   // dont restart it if its already going
        Instance.musicSource.clip = Instance.music;
        Instance.musicSource.loop = true;
        Instance.musicSource.Play();
    }

    // reads the options menu volumes (saved in playerprefs)
    public void ApplyVolumes()
    {
        float m = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (musicSource != null) musicSource.volume = musicBaseVolume * m;
    }

    // main way to play a sound effect. playoneshot lets sounds overlap instead of cutting each other off
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || clip == null) return;   // empty slot = just skip it
        Instance.sfxSource.PlayOneShot(clip, volume * Instance.sfxVolume);
    }

    public static void PlayKey()
    {
        if (Instance == null || Instance.key == null) return;
        AudioSource src = Instance.keySource != null ? Instance.keySource : Instance.sfxSource;
        src.pitch = Instance.keyPitch + Random.Range(-Instance.keyPitchVariation, Instance.keyPitchVariation);
        src.PlayOneShot(Instance.key, Instance.keyVolume * Instance.sfxVolume);
    }

    // called by the typewriter loops for each letter that shows up
    // skips spaces and punctuation, and only blips every few letters so it doesnt get noisy
    public static void PlayVoice(char letter, int index)
    {
        if (Instance == null || Instance.voiceBlip == null) return;
        if (!char.IsLetterOrDigit(letter)) return;
        if (index % Mathf.Max(1, Instance.voiceEveryNLetters) != 0) return;

        AudioSource src = Instance.voiceSource;
        src.pitch = Instance.voicePitch + Random.Range(-Instance.voicePitchVariation, Instance.voicePitchVariation);
        src.PlayOneShot(Instance.voiceBlip, Instance.voiceVolume * Instance.sfxVolume);
    }

    public static void PlayTypo()  { if (Instance != null) Play(Instance.typo, Instance.typoVolume); }
    public static void PlayDeath() { if (Instance != null) Play(Instance.playerDeath, Instance.deathVolume); }

    // called once when the unleash cutscene starts
    // lines up the ring, boom, spin, draw and slash with the animation
    public static void PlayUnleash()
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.PlayAfter(Instance.unleash, Instance.unleashVolume, Instance.unleashDelay));
        Instance.StartCoroutine(Instance.PlayAfter(Instance.unleashBoom, Instance.boomVolume, Instance.boomTime));
        Instance.StartCoroutine(Instance.PlayAfter(Instance.unleashSpin, Instance.spinVolume, Instance.spinTime));
        Instance.StartCoroutine(Instance.PlayAfter(Instance.unleashDraw, Instance.drawVolume, Instance.drawTime));
        Instance.StartCoroutine(Instance.PlayAfter(Instance.unleashSlash, Instance.slashVolume, Instance.slashTime));
    }

    // realtime wait bc the game is frozen (timescale 0) during the cutscene
    IEnumerator PlayAfter(AudioClip clip, float volume, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Play(clip, volume);
    }

    void OnParry()     { Play(parry, parryVolume); }
    void OnBossHit()   { Play(bossHit, hitVolume); }
    void OnPlayerHit() { Play(playerHit, hitVolume); }
}