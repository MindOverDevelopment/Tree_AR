using System.Media;
using UnityEngine;

public class Portal : MonoBehaviour
{
    private RandomSpawner spawner;
    private ScoreManager scoreManager;
    private AudioSource audioSource;
    [SerializeField] private AudioClip portalEnterSFX;
    void Start()
    {
        // Find the RandomSpawner in the scene
        spawner = FindObjectOfType<RandomSpawner>();
        scoreManager = FindObjectOfType<ScoreManager>();
        audioSource = GameObject.FindGameObjectWithTag("AudioPlayer").GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            audioSource.PlayOneShot(portalEnterSFX);
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
