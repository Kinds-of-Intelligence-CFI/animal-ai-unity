using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CollisionImpulseTracker : MonoBehaviour
{
    public float impulseMagnitude;

    private void FixedUpdate()
    {
        impulseMagnitude = 0;
    }

    void OnCollisionEnter(Collision col)
    {
        print("OnCollisionEnter activated");
        impulseMagnitude += col.impulse.magnitude;
    }

    void OnCollisionStay(Collision col)
    {
        impulseMagnitude -= col.impulse.magnitude;
    }
}
