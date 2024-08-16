using UnityEngine;

/// <summary>
/// A BallGoal that bounces the ball depending on the forceToApply and maximumVelocity parameters.
/// </summary>
public class BallGoalBounce : BallGoal
{
    [Header("Bounce Settings")]
    public float maximumVelocity = 20;
    public float forceToApply = 5;

    private Rigidbody rBody;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        if (rBody != null)
        {
            rBody.AddForce(transform.forward * forceToApply, ForceMode.Impulse);
        }
    }

    void Update()
    {
        /* Cap the velocity to the maximum velocity */
        if (rBody != null && rBody.velocity.magnitude > maximumVelocity)
        {
            rBody.velocity = rBody.velocity.normalized * maximumVelocity;
        }
    }
}
