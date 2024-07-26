using UnityEngine;

/// <summary>
/// Tracks the cumulative impulse magnitude from collisions this frame.
/// </summary>
public class CollisionImpulseTracker : MonoBehaviour
{
    public float impulseMagnitude;

    private void FixedUpdate()
    {
        impulseMagnitude = 0;
    }

    private void OnCollisionEnter(Collision col)
    {
        impulseMagnitude += col.impulse.magnitude;
    }

    private void OnCollisionStay(Collision col)
    {
        impulseMagnitude += col.impulse.magnitude;
    }
}
