using UnityEngine;

public class Portal : MonoBehaviour
{
    private RandomSpawner spawner;
    private ScoreManager scoreManager;
    void Start()
    {
        // Find the RandomSpawner in the scene
        spawner = FindObjectOfType<RandomSpawner>();
        scoreManager = FindObjectOfType<ScoreManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            scoreManager.ScorePoint();
            // Player has passed through the portal, spawn a new one
            if (spawner != null)
            {
                spawner.SpawnPortal();
            }

            // Optionally, destroy the current portal after some time or immediately
            Destroy(gameObject); // or use Destroy(gameObject, delayInSeconds);
        }
    }
}
