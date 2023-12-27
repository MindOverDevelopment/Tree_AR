using System.Drawing.Text;
using UnityEngine;

public class AttachOnTrigger : MonoBehaviour
{
    // Public variable to assign the GameObject you want to attach
    public GameObject objectToAttach;

    // Tag for the camera
    private const string cameraTag = "MainCamera";

    void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the main camera
        if (other.CompareTag(cameraTag) && objectToAttach != null)
        {
            // Set the specified GameObject's parent to the colliding object (which should be the camera)
            objectToAttach.transform.SetParent(other.gameObject.transform);
            gameObject.GetComponent<Collider>().enabled = false;

            // Optional: Set the local position and rotation relative to the camera
            // objectToAttach.transform.localPosition = new Vector3(0, 0, 0);
            // objectToAttach.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("Either the colliding object is not the MainCamera or the object to attach is not assigned.");
        }
    }
}
