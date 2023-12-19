using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The trigger in which the player is to walk into.
/// </summary>
public class TreeTriggerEnter : MonoBehaviour
{
    private Camera mainCamera;
    [SerializeField] private Volume volume;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject tree;
    [SerializeField] private GameObject strangeParticles;
    [SerializeField] private AudioSource harpSound;
    [SerializeField] private GameObject miniGame;
    [SerializeField] private GameObject leaves;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip portalEnterSound;
    void Start()
    {
       mainCamera = Camera.main;
    }

    /// <summary>
    /// Controls various objects to turn off and on. Also deletes the background sound and replaces.
    /// </summary>
    /// <param name="other">Player / Maincamera</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("MainCamera"))
        {
            volume.enabled = true;
            animator.enabled = true;
            harpSound.enabled = true;
            miniGame.SetActive(true);
            tree.SetActive(false);
            strangeParticles.SetActive(true);
            leaves.SetActive(false);
            //StartCoroutine(AfterSwirlSound());
            audioSource.PlayOneShot(portalEnterSound);
            Destroy(GameObject.FindWithTag("SoundManager"));

        }
    }

    IEnumerator AfterSwirlSound()
    {
        yield return new WaitForSeconds(8);
    }
}
