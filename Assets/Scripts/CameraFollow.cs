using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject followObj;

    void Start()
    {
        transform.position = followObj.transform.position;
        transform.rotation = followObj.transform.rotation;
    }

    void Update()
    {
        transform.position = followObj.transform.position;
        transform.rotation = followObj.transform.rotation;
    }
}
