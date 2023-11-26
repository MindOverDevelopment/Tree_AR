using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class TreeController : MonoBehaviour
{
    private bool isScale1;
    [SerializeField]private float timer1 = 7.5f;
    [SerializeField] private float timer2 = 5f;
    [SerializeField] private float scale = 0.35f;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Scale1());
    }

    // Update is called once per frame

    IEnumerator Scale1()
    {
        transform.DOScale(0.1f,timer1).SetEase(Ease.InOutCubic);
        yield return new WaitForSeconds(timer1);
        transform.DOScale(scale, timer2).SetEase(Ease.InOutElastic);
    }
}
