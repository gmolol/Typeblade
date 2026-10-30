using UnityEngine;

// goes on the word attack prefab, plays the whoosh the moment a word gets thrown
public class AttackSound : MonoBehaviour
{
    [Range(0f, 1f)] public float volume = 0.6f;

    void Start()
    {
        // every spawned word is a new copy of the prefab so start runs once per attack
        if (AudioManager.Instance != null)
            AudioManager.Play(AudioManager.Instance.bossAttack, volume);
    }
}
