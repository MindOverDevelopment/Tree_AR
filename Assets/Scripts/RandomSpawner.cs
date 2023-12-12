using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
    public GameObject portalPrefab; // Assign the portal prefab in the inspector
    public Collider spawnArea; // Assign a collider that defines the spawn area
    public float minDistanceBetweenPortals = 10f; // Minimum distance between portals

    private Vector3 lastSpawnPosition;

    void Start()
    {
        // Spawn the first portal as the game starts
        SpawnPortal();
    }

    public void SpawnPortal()
    {
        if (portalPrefab == null || spawnArea == null)
        {
            Debug.LogError("Portal prefab or spawn area is not set.");
            return;
        }

        Vector3 randomPosition = GenerateRandomPositionFarFromLast();

        // Random rotation on the Z-axis
        float randomZRotation = Random.Range(0f, 360f);

        // Define the rotation
        Quaternion rotation = Quaternion.Euler(90, 0, randomZRotation);

        // Spawn the portal at the random position with the specified rotation
        GameObject spawnedPortal = Instantiate(portalPrefab, randomPosition, rotation);

        // Update the last spawn position
        lastSpawnPosition = spawnedPortal.transform.position;
    }

    Vector3 GenerateRandomPositionFarFromLast()
    {
        Vector3 randomPosition;
        int safetyCounter = 0;

        do
        {
            Bounds bounds = spawnArea.bounds;
            randomPosition = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            safetyCounter++;
            if (safetyCounter > 100)
            {
                Debug.LogWarning("Couldn't find a position far enough from the last portal. Spawning at a random position.");
                break;
            }

        } while (Vector3.Distance(randomPosition, lastSpawnPosition) < minDistanceBetweenPortals);

        return randomPosition;
    }
}
