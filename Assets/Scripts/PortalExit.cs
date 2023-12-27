using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalExit : MonoBehaviour
{
    [SerializeField] private GameObject portalObjects;
    [SerializeField] private GameObject magicMusic;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("MainCamera"))
        {
            Camera cam = other.gameObject.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Nothing;
            portalObjects.SetActive(false);
            magicMusic.gameObject.transform.parent = null;
        }
    }
}
