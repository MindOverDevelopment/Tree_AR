using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public GameObject treePrefab; // Assign in Unity Editor
    private bool hasBeenFired = false;
    private bool hasCollided = false;
    private Vector3 originalPosition;
    private Rigidbody rb;

    void Start()
    {
        originalPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Update()
    {
        if (hasBeenFired && !hasCollided)
        {
            StartCoroutine(WaitAndReset());
        }
    }

    public void Fire(Vector3 direction, float force)
    {
        rb.isKinematic = false;
        rb.AddForce(direction * force, ForceMode.Impulse);
        hasBeenFired = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        hasCollided = true;
        Instantiate(treePrefab, transform.position, Quaternion.identity);
    }

    IEnumerator WaitAndReset()
    {
        yield return new WaitForSeconds(3);
        if (!hasCollided)
        {
            transform.position = originalPosition;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            hasBeenFired = false; // Reset the flag for next fire
        }
    }
}