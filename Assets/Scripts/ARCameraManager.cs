using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;

public class ARCameraTextureUpdater : MonoBehaviour
{
    private ARCameraManager cameraManager;
    public Material targetMaterial; // Assign this in the inspector
    private bool isProcessingImage = false; // To prevent overloading with requests

    void Awake()
    {
        // Attempt to find the ARCameraManager in the scene
        cameraManager = FindObjectOfType<ARCameraManager>();

        if (cameraManager != null)
        {
            cameraManager.frameReceived += OnCameraFrameReceived;
            Debug.Log("ARCameraManager found and event registered.");
        }
        else
        {
            Debug.LogError("ARCameraManager not found in the scene.");
        }
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!isProcessingImage && cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            isProcessingImage = true;
            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorY
            };

            var request = image.ConvertAsync(conversionParams);
            StartCoroutine(WaitForConversion(request));
            image.Dispose();
        }
    }

    private IEnumerator WaitForConversion(XRCpuImage.AsyncConversion request)
    {
        while (!request.status.IsDone())
        {
            yield return null;
        }

        if (request.status == XRCpuImage.AsyncConversionStatus.Ready)
        {
            ApplyTexture(request);
        }
        else
        {
            Debug.LogError("Failed to convert camera image: " + request.status);
        }
        isProcessingImage = false;
        request.Dispose();
    }

    void ApplyTexture(XRCpuImage.AsyncConversion request)
    {
        Texture2D texture = new Texture2D(request.conversionParams.outputDimensions.x, request.conversionParams.outputDimensions.y, request.conversionParams.outputFormat, false);
        texture.LoadRawTextureData(request.GetData<byte>());
        texture.Apply();

        if (targetMaterial != null && targetMaterial.HasProperty("_CameraTexture"))
        {
            targetMaterial.SetTexture("_CameraTexture", texture);
        }
    }

    void OnDestroy()
    {
        if (cameraManager != null)
        {
            cameraManager.frameReceived -= OnCameraFrameReceived;
            Debug.Log("Event unregistered from ARCameraManager.");
        }
    }
}
