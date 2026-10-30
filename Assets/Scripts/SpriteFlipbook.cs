using UnityEngine;

// plays a list of sprites like a flipbook. used for effects like the unleash burst
// simpler than making a whole animator for a one time effect
public class SpriteFlipbook : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 12f;
    public bool loop = false;
    public bool destroyWhenDone = true;

    SpriteRenderer sr;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames.Length > 0) sr.sprite = frames[0];
    }

    void Update()
    {
        t += Time.deltaTime;
        int i = (int)(t * fps);              // which frame were on (time x frames per second)

        if (i >= frames.Length)
        {
            if (loop) { t = 0f; i = 0; }
            else
            {
                if (destroyWhenDone) Destroy(gameObject);
                return;
            }
        }

        sr.sprite = frames[i];
    }
}
