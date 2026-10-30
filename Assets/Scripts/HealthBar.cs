using UnityEngine;
using UnityEngine.UI;
using TMPro;

// fills the hp bar for the player or the boss
public class HealthBar : MonoBehaviour
{
    public bool showPlayer;   // checked = player bar, unchecked = boss bar
    public Image fill;
    public TMP_Text hpText;

    void Update()
    {
        Health h = showPlayer ? Health.Player : Health.Boss;
        if (h == null) return;

        // (float) so it doesnt do integer division and always give 0
        fill.fillAmount = (float)h.CurrentHP / h.maxHP;

        if (hpText != null) hpText.text = h.CurrentHP + " / " + h.maxHP;
    }
}
