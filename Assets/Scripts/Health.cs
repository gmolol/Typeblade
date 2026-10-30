using UnityEngine;
using System.Collections;

// hp for both the player and the boss
public class Health : MonoBehaviour
{
    // shortcuts so any script can grab the player or boss health easily
    public static Health Player;
    public static Health Boss;

    public bool isPlayer;
    public int maxHP = 20;
    public SpriteRenderer flashTarget;   // sprite that flashes red when hit
    public Color flashColor = Color.red;
    public float flashTime = 0.12f;

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;
    public int minHP = 0;                // bossphases sets this to 1 so the crow cant die before transforming
    public event System.Action OnDamaged; // other scripts listen to this (sounds, shake, hurt anim)

    void Awake()
    {
        CurrentHP = maxHP;
        if (isPlayer) Player = this;
        else Boss = this;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Max(CurrentHP - amount, minHP); // never goes below minHP
        Debug.Log(name + " HP: " + CurrentHP + "/" + maxHP);

        if (flashTarget != null) StartCoroutine(Flash());
        if (IsDead) Debug.Log(name + " defeated!");
        OnDamaged?.Invoke();   // ?. so it doesnt crash if nobodys listening
    }

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(CurrentHP + amount, maxHP);
    }

    // quick red flash then back to normal
    IEnumerator Flash()
    {
        flashTarget.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        flashTarget.color = Color.white;
    }
}
