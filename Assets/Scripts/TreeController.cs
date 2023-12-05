using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class TreeController : MonoBehaviour
{
    private bool isScale1;
    [SerializeField] private float timer1 = 7.5f;
    [SerializeField] private float timer2 = 5f;
    [SerializeField] private float scale = 0.35f;

    void Start()
    {
        // Make the tree face the main camera
        if (Camera.main != null)
        {
            Vector3 directionToCamera = Camera.main.transform.position - transform.position;
            Quaternion rotation = Quaternion.LookRotation(directionToCamera);
            // Optionally, if you only want to rotate around the Y axis
            rotation.x = 0;
            rotation.z = 0;
            transform.rotation = rotation;
        }

        StartCoroutine(Scale1());
    }

    IEnumerator Scale1()
    {
        transform.DOScale(0.1f, timer1).SetEase(Ease.InOutCubic);
        yield return new WaitForSeconds(timer1);
        transform.DOScale(scale, timer2).SetEase(Ease.InOutElastic);
    }
}
