using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PortalEnter : MonoBehaviour
{
    public float newFogEndDistance = 300.0f; // The new fog end distance you want to set
    [SerializeField] private GameObject portalObjectsOn;
    [SerializeField] private GameObject portalObjectsOff;
    [SerializeField] private GameObject tree;
    [SerializeField] private GameObject magicMusic;
    private ARMeshManager arMeshManager;


    private void Start()
    {
        // Find the ARMeshManager in the scene at the start.
        arMeshManager = FindObjectOfType<ARMeshManager>();
        if (arMeshManager == null)
        {
            Debug.LogError("ARMeshManager was not found in the scene.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("MainCamera"))
        {
            Camera cam = other.gameObject.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            portalObjectsOn.SetActive(true);
            portalObjectsOff.SetActive(false);
            tree.SetActive(false);
            magicMusic.SetActive(true);
            RenderSettings.fogEndDistance = newFogEndDistance;
        }
    }

    private void MeshManager()
    {
        // Check if ARMeshManager was found.
        if (arMeshManager != null)
        {
            // Set the mesh prefab of the ARMeshManager to 'null' effectively removing the mesh visualization.
            arMeshManager.meshPrefab = null;

            Debug.Log("AR Mesh Prefab changed to none.");
        }
        else
        {
            Debug.LogError("No ARMeshManager available to modify.");
        }
    }
}
