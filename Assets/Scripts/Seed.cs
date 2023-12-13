using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Seed : MonoBehaviour
{
    public GameObject treePrefab;
    public float maxProjectileForce = 15f;
    [SerializeField] private float seedScale = 0.2f; // Factor to control the scale of the seed
    private Rigidbody rb;
    private string cameraChildName = "CameraChild";
    public Transform resetTransform;
    private bool hasBeenFired = false;
    private bool hasCollided = false;
    private bool treeSpawned = false;
    [SerializeField] private GameObject seedTxt;
    [SerializeField] private Slider forceSlider;
    [SerializeField] private GameObject scalingObject;
    [SerializeField] private GameObject launchVFX;
    [SerializeField] private TrailRenderer trailRenderer;

    private float touchStartTime;
    private bool isTouching = false;
    [SerializeField] private float currentForce;
    [SerializeField] private float forceIncreaseRate = 2f; // Factor to control the rate of force increase

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        // stores the reset transform for the seed to respawn
        if (resetTransform == null)
        {
            resetTransform = Camera.main.transform.Find(cameraChildName);
        }

        if (forceSlider != null)
        {
            forceSlider.minValue = 0;
            forceSlider.maxValue = 1;
        }
    }

    private void Update()
    {
        UserTouchAndHold();
    }
    /// <summary>
    /// Checks to see if the user has touched and how long they have held for.
    /// </summary>
    private void UserTouchAndHold()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartTime = Time.time;
                isTouching = true;
            }
            else if (touch.phase == TouchPhase.Ended && isTouching && !hasBeenFired)
            {
                FireProjectile(touch.position);
                StartCoroutine(WaitAndReset());
                isTouching = false;
            }

            if (isTouching)
            {
                currentForce = Mathf.Min((Time.time - touchStartTime) * forceIncreaseRate, maxProjectileForce);
                UpdateForceUI(currentForce); // Update UI based on current force
            }
        }
        else
        {
            if (isTouching)
            {
                // Reset force and UI when touch ends
                currentForce = 0;
                UpdateForceUI(currentForce);
            }
            isTouching = false;
        }
    }

    /// <summary>
    /// The length the player has held for is converted to a force that sends the seed in an arc 
    /// </summary>
    /// <param name="touchPosition"></param>
    private void FireProjectile(Vector2 touchPosition)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        Vector3 direction = (worldPosition - Camera.main.transform.position).normalized;

        // Add upward component to create an arc
        float upwardForceFactor = 0.5f;
        direction += Vector3.up * upwardForceFactor;
        direction.Normalize();

        rb.isKinematic = false;
        rb.AddForce(direction * currentForce, ForceMode.Impulse);
        hasBeenFired = true;

        seedTxt.SetActive(false); // Disable the text
        launchVFX.SetActive(true); // Enable the launch VFX
        trailRenderer.enabled = true; // Enable the trail renderer

    }

    /// <summary>
    /// For when the seed hits the mesh created by the AR camera
    /// </summary>
    /// <param name="collision">AR mesh</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!treeSpawned)
        {
            trailRenderer.enabled = false; // Disable the trail renderer
            treeSpawned = true;
            hasCollided = true;
            forceSlider.gameObject.SetActive(false);
            Instantiate(treePrefab, transform.position, Quaternion.identity);
            gameObject.SetActive(false);


        }
    }

    /// <summary>
    /// Used for when the player misses.
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitAndReset()
    {
        yield return new WaitForSeconds(3);

        if (!hasCollided)
        {
            transform.position = resetTransform.position;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            transform.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Reset the scale
            seedTxt.SetActive(true); // Re-enable the text
            launchVFX.SetActive(false);
            trailRenderer.enabled = false; 
        }

        hasBeenFired = false;
        hasCollided = false;
        treeSpawned = false;
    }

    /// <summary>
    /// Updates the slider so the player knows how much force they have applied.
    /// </summary>
    /// <param name="force"></param>
    private void UpdateForceUI(float force)
    {
        if (forceSlider != null)
        {
            // Update the slider's value based on the current force
            forceSlider.value = force / maxProjectileForce;
        }

        if (scalingObject != null)
        {
            // Scale the GameObject based on the current force
            float scale = (force / maxProjectileForce) * seedScale;
            scalingObject.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
