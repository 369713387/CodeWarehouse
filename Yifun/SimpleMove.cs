using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMove : MonoBehaviour
{
    private void Start()
    {
        transform.LookAt(GameObject.FindGameObjectWithTag("Player").transform);
    }

    private void Update()
    {       
        transform.Translate(2f * Vector3.up * Time.deltaTime);
    }
}
