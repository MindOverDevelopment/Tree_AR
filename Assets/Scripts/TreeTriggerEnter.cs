using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TreeTriggerEnter : MonoBehaviour
{
    private Camera mainCamera;
    [SerializeField] private Volume volume;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject tree;
    void Start()
    {
       mainCamera = Camera.main;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("MainCamera"))
        {
            volume.enabled = true;
            animator.enabled = true;
            tree.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
