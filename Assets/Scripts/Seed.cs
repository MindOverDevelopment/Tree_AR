using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for UI components

public class Seed : MonoBehaviour
{
    public GameObject treePrefab;
    public float maxProjectileForce = 15f;
    private Rigidbody rb;
    private string cameraChildName = "CameraChild";
    public Transform resetTransform;
    private bool hasBeenFired = false;
    private bool hasCollided = false;
    private bool treeSpawned = false;
    [SerializeField] private GameObject seedTxt;
    [SerializeField] private Slider forceSlider; // Reference to the UI Slider
    [SerializeField] private GameObject scalingObject; // GameObject to scale

    private float touchStartTime;
    private bool isTouching = false;
    [SerializeField] private float currentForce;
    [SerializeField] private float forceIncreaseRate = 2f; // Factor to control the rate of force increase

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
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
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!treeSpawned)
        {
            treeSpawned = true;
            hasCollided = true;
            Instantiate(treePrefab, transform.position, Quaternion.identity);
        }
    }

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
        }

        hasBeenFired = false;
        hasCollided = false;
        treeSpawned = false;
    }

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
            float scale = (force / maxProjectileForce) * 0.04f;
            scalingObject.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
