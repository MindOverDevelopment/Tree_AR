using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalEnter : MonoBehaviour
{
    [SerializeField] GameObject portalObjectsOn;
    [SerializeField] GameObject portalObjectsOff;
    [SerializeField] GameObject tree;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("MainCamera"))
        {
            Camera cam = other.gameObject.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            portalObjectsOn.SetActive(true);
            portalObjectsOff.SetActive(false);
            tree.SetActive(false);
        }
    }
}
