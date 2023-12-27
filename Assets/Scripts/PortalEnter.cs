using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalEnter : MonoBehaviour
{
    public float newFogEndDistance = 300.0f; // The new fog end distance you want to set
    [SerializeField] private GameObject portalObjectsOn;
    [SerializeField] private GameObject portalObjectsOff;
    [SerializeField] private GameObject tree;
    [SerializeField] private GameObject magicMusic;
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
}
