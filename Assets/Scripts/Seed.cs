using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seed : MonoBehaviour
{
    [SerializeField] private GameObject tree;

    private void Start()
    {
        StartCoroutine(SeedLife());
    }

    private void OnCollisionEnter(Collision collision)
    {
        Instantiate(tree, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    IEnumerator SeedLife()
    {
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }
}
