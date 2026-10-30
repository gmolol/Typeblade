using UnityEngine;
using UnityEngine.UI;

// red vignette that pulses like a heartbeat when the player is low on hp
public class LowHPWarning : MonoBehaviour
{
    public Image vignette;
    [Range(0f, 1f)] public float threshold = 0.25f;   // starts at 25% hp
    public float minAlpha = 0.2f;                      // faintest part of the pulse
    public float maxAlpha = 0.55f;                     // strongest part of the pulse
    public float pulseSpeed = 4f;                      // heartbeat speed right at the threshold
    public float fadeSpeed = 2f;                       // how fast it shows up / goes away

    [Header("Generated Vignette")]
    [Range(0f, 1f)] public float clearRadius = 0.55f;  // fully clear inside this
    [Range(0f, 1.5f)] public float solidRadius = 1.0f; // fully solid past this

    float current;

    void Awake()
    {
        if (vignette == null) vignette = GetComponent<Image>();
        // the vignette png kept downloading broken so the image just gets made in code
        if (vignette.sprite == null) vignette.sprite = MakeVignette(480, 270);
        SetAlpha(0f);
    }

    void Update()
    {
        float target = 0f;

        Health p = Health.Player;
        bool fighting = FightManager.Instance != null
                        && FightManager.Instance.CurrentState == FightManager.State.Fighting
                        && !UnleashCutscene.Playing;

        if (p != null && !p.IsDead && fighting)
        {
            float ratio = (float)p.CurrentHP / p.maxHP;      // 1 = full, 0 = dead

            if (ratio <= threshold)
            {
                float danger = 1f - ratio / threshold;        // 0 at the threshold, 1 almost dead

                // heartbeat: sin wave squished into 0..1, and it speeds up the closer you are to dying
                float beat = (Mathf.Sin(Time.time * pulseSpeed * (1f + danger)) + 1f) / 2f;

                target = Mathf.Lerp(minAlpha, maxAlpha, beat) * (0.6f + 0.4f * danger);
            }
        }

        // ease toward the target so it doesnt pop on and off
        current = Mathf.MoveTowards(current, target, fadeSpeed * Time.unscaledDeltaTime);
        SetAlpha(current);
    }

    void SetAlpha(float a)
    {
        Color c = vignette.color;
        c.a = a;
        vignette.color = c;
    }

    // makes a white image thats clear in the middle and fades to solid at the edges
    // (the image color in the inspector tints it red)
    Sprite MakeVignette(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // -1 to 1 across the screen both ways
                float nx = (x + 0.5f) / width * 2f - 1f;
                float ny = (y + 0.5f) / height * 2f - 1f;

                // distance from the center (0 in the middle, around 1.25 in the corners)
                float r = Mathf.Sqrt(nx * nx + ny * ny) / Mathf.Sqrt(2f) * 1.25f;

                float t = Mathf.InverseLerp(clearRadius, solidRadius, r);
                float a = t * t * (3f - 2f * t);           // smoothstep for a soft edge

                pixels[y * width + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}
