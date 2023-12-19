using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

public class RandomizeGradient : MonoBehaviour
{
    // Public field for a list of VFX components
    public List<VisualEffect> vfxComponents = new List<VisualEffect>();

    public string gradientParameterName = "MyGradient";
    public List<Gradient> presetGradients = new List<Gradient>(); // List of preset gradients

    void Start()
    {
        // Apply a random gradient to each VFX graph in the list
        foreach (var vfxComponent in vfxComponents)
        {
            ApplyRandomGradientToVFX(vfxComponent);
        }
    }

    void ApplyRandomGradientToVFX(VisualEffect vfxComponent)
    {
        // Check if there are any gradients in the list
        if (presetGradients != null && presetGradients.Count > 0)
        {
            // Select a random gradient from the list
            Gradient selectedGradient = presetGradients[Random.Range(0, presetGradients.Count)];

            // Apply the selected gradient to the VFX graph
            if (vfxComponent != null)
            {
                vfxComponent.SetGradient(gradientParameterName, selectedGradient);
            }
        }
        else
        {
            Debug.LogWarning("No preset gradients are set in the list.");
        }
    }
}
