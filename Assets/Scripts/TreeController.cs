using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class TreeController : MonoBehaviour
{
    private bool isScale1;
    [SerializeField] private float timer1 = 7.5f;
    [SerializeField] private float timer2 = 5f;
    [SerializeField] private float ringTimer = 2f;
    [SerializeField] private float treeScale = 0.35f;
    [SerializeField] private float ringScale = 1f;

    [SerializeField] private GameObject leavesFalling;
    [SerializeField] private GameObject ringObj;
    [SerializeField] private GameObject miniGame;
    [SerializeField] private GameObject strangeBits;
    
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

    /// <summary>
    /// Controls the animations for the tree growing.
    /// </summary>
    /// <returns>Waits to do the big growth effect</returns>
    IEnumerator Scale1()
    {
        transform.DOScale(0.1f, timer1).SetEase(Ease.InOutCubic);
        yield return new WaitForSeconds(timer1);
        transform.DOScale(treeScale, timer2).SetEase(Ease.InOutElastic);
        StartCoroutine(ActivateLeavesAndRing());
    }

    /// <summary>
    /// turns on the falling leaves once the growth is done. Also turns on the ring
    /// </summary>
    /// <returns></returns>
    IEnumerator ActivateLeavesAndRing()
    {
        leavesFalling.SetActive(true);

        yield return new WaitForSeconds(timer2);
        leavesFalling.transform.parent = null;
        miniGame.transform.parent = null;
        strangeBits.transform.parent = null;
        ringObj.SetActive(true);

    }
}
