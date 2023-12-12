using UnityEngine;

public class Portal : MonoBehaviour
{
    private RandomSpawner spawner;

    void Start()
    {
        // Find the RandomSpawner in the scene
        spawner = FindObjectOfType<RandomSpawner>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
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
