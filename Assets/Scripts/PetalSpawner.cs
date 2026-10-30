using UnityEngine;

// keeps spawning petals from the top and right edges of the screen
public class PetalSpawner : MonoBehaviour
{
    public GameObject petalPrefab;
    public float minDelay = 0.3f;              // time between petals
    public float maxDelay = 0.9f;
    public int prewarm = 8;                    // petals already on screen at the start
    public Vector2 scaleRange = new Vector2(0.7f, 1.2f);
    public Vector2 speedRange = new Vector2(0.8f, 1.3f);

    [Header("Spawn Area")]
    public float topY = 5.8f;                  // just above the screen
    public float rightX = 10f;                 // just right of the screen
    public Vector2 topXRange = new Vector2(-6f, 10f);
    public Vector2 rightYRange = new Vector2(-2f, 5.5f);

    float timer;

    void Start()
    {
        // put some petals around the screen already so it doesnt start empty
        for (int i = 0; i < prewarm; i++)
            Spawn(new Vector3(Random.Range(-9f, 10f), Random.Range(-3f, 5.5f), 0f));
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = Random.Range(minDelay, maxDelay);   // random gap so it doesnt look like a pattern
            Spawn(EdgePoint());
        }
    }

    // half come from the top, half from the right
    Vector3 EdgePoint()
    {
        if (Random.value < 0.5f)
            return new Vector3(Random.Range(topXRange.x, topXRange.y), topY, 0f);
        else
            return new Vector3(rightX, Random.Range(rightYRange.x, rightYRange.y), 0f);
    }

    void Spawn(Vector3 position)
    {
        Quaternion tilt = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        GameObject p = Instantiate(petalPrefab, position, tilt, transform);

        // random size and speed so they dont all look the same
        p.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);

        Petal petal = p.GetComponent<Petal>();
        if (petal != null) petal.velocity *= Random.Range(speedRange.x, speedRange.y);
    }
}
