using UnityEngine;

public class CameraRotationUpdater : MonoBehaviour
{
    private Transform targetCamera; // Assign the target camera in the inspector
    private void Start()
    {
        targetCamera = GameObject.FindGameObjectWithTag("MainCamera").transform;
    }

    void Update()
    {
        // Check if the target camera is assigned
        if (targetCamera != null)
        {
            // Update the rotation of this camera to match the target camera's rotation
            transform.rotation = targetCamera.rotation;
        }
        else
        {
            Debug.LogWarning("Target camera is not assigned!");
        }
    }
}