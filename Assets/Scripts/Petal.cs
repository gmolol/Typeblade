using UnityEngine;

// one falling petal. drifts left and down, bobs a little, spins, then deletes itself off screen
public class Petal : MonoBehaviour
{
    public Vector2 velocity = new Vector2(-1.6f, -0.7f);   // drift left and down
    public float swayAmount = 0.6f;                         // up/down bob
    public float swaySpeed = 2f;
    public float spin = 60f;                                // degrees per second
    public float killX = -11f;                              // off screen left
    public float killY = -6f;                               // off screen bottom

    float phase;

    void Start()
    {
        phase = Random.value * 10f;                 // so every petal sways at a different time
        spin *= Random.Range(-1f, 1f);              // random spin direction and speed
    }

    void Update()
    {
        float sway = Mathf.Sin((Time.time + phase) * swaySpeed) * swayAmount;
        Vector2 move = velocity + new Vector2(0f, sway);

        transform.position += (Vector3)move * Time.deltaTime;
        transform.Rotate(0f, 0f, spin * Time.deltaTime);

        // gone off screen, delete it so they dont pile up forever
        if (transform.position.x < killX || transform.position.y < killY)
            Destroy(gameObject);
    }
}
