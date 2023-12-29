using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalExit : MonoBehaviour
{
    [SerializeField] private GameObject portalObjects;
    [SerializeField] private GameObject magicMusic;
    private GameObject portalExit;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("MainCamera"))
        {
            portalExit = GameObject.FindGameObjectWithTag("PortalEffectExit");
            portalExit.transform.GetChild(0).gameObject.SetActive(true);
            Camera cam = other.gameObject.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Nothing;
            portalObjects.SetActive(false);
            magicMusic.gameObject.transform.parent = null;
        }
    }
}
