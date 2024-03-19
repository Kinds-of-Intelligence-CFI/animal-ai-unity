using UnityEngine;

/// <summary>
/// A BallGoal that bounces the ball depending on the forceToApply and maximumVelocity parameters.
public class BallGoalBounce : BallGoal
{
    [Header("Bounce Settings")]
    public float maximumVelocity = 20;
    public float forceToApply = 5;

    private Rigidbody rBody;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();

        rBody.AddForce(
            forceToApply * transform.forward * Time.fixedDeltaTime,
            ForceMode.VelocityChange
        );
    }
}
