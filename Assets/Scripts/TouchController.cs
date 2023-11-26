using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchController : MonoBehaviour
{
    public GameObject projectilePrefab; // Assign your projectile prefab in the Unity Editor
    public float projectileForce = 10f;
    private Rigidbody rb;
    [SerializeField] private GameObject seedTxt;

    private void Start()
    {
        // Get the Rigidbody component of the projectile
            rb = projectilePrefab.GetComponent<Rigidbody>();
        rb.isKinematic = true;

    }

    void Update()
    {
        // Check if there is any touch input
        if (Input.touchCount > 0)
        {
            // Get the first touch
            Touch touch = Input.GetTouch(0);

            // Check if the touch phase is the beginning of a touch
            if (touch.phase == TouchPhase.Began)
            {
                // Call the method to fire the projectile
                FireProjectile(touch.position);
            }
        }
    }

    void FireProjectile(Vector2 touchPosition)
    {
            // Convert the touch position to a world point
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));

            // Instantiate the projectile prefab at the touch position
            //GameObject projectile = Instantiate(projectilePrefab, worldPosition, Quaternion.identity);


            // Check if the Rigidbody component is not null
            if (rb != null)
            {
                // Calculate the direction away from the camera
                Vector3 direction = (worldPosition - Camera.main.transform.position).normalized;
                rb.isKinematic = false;

                // Apply force to the projectile in the calculated direction
                rb.AddForce(direction * projectileForce, ForceMode.Impulse);
                 Destroy(seedTxt);

            }
        

    }
}