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
    [SerializeField] private GameObject strangeParticles;
    [SerializeField] private AudioSource swirlSound;
    [SerializeField] private AudioSource bgStrangeSound;
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
            swirlSound.enabled = true;
            tree.SetActive(false);
            strangeParticles.SetActive(true);
            StartCoroutine(AfterSwirlSound());
            Destroy(GameObject.FindWithTag("SoundManager"));

        }
    }

    IEnumerator AfterSwirlSound()
    {
        yield return new WaitForSeconds(8);
        bgStrangeSound.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
