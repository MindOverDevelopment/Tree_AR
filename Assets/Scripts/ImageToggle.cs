using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageToggle : MonoBehaviour
{
    [SerializeField] private RawImage image;

    public void EnableImage()
    {
        image.enabled = true;
    }

    public void DisableImage()
    {
        image.enabled = false;
    }
}
