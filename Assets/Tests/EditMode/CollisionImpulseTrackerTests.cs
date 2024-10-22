using UnityEngine;
using NUnit.Framework;
using System.Reflection;

/// <summary>
/// Tests for the CollisionImpulseTracker class.
/// This class is important to test because it is responsible for tracking the cumulative impulse magnitude from collisions this frame.
/// In detail, it has a public float impulseMagnitude field that is set to zero in the FixedUpdate method.
/// The OnCollisionEnter and OnCollisionStay methods increment the impulseMagnitude field by the magnitude of the impulse from the collision.
/// </summary>
public class CollisionImpulseTrackerTests
{
    private CollisionImpulseTracker collisionImpulseTracker;
    private MethodInfo fixedUpdateMethod;

    [SetUp]
    public void SetUp()
    {
        GameObject gameObject = new GameObject();
        collisionImpulseTracker = gameObject.AddComponent<CollisionImpulseTracker>();
        fixedUpdateMethod = typeof(CollisionImpulseTracker).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(collisionImpulseTracker.gameObject);
    }

    [Test]
    public void FixedUpdate_SetsImpulseMagnitudeToZero()
    {
        collisionImpulseTracker.impulseMagnitude = 10f;
        fixedUpdateMethod.Invoke(collisionImpulseTracker, null);
        Assert.AreEqual(0f, collisionImpulseTracker.impulseMagnitude);
    }

    [Test]
    public void FixedUpdate_ResetsImpulseMagnitudeToZeroMultipleTimes()
    {
        collisionImpulseTracker.impulseMagnitude = 5f;
        for (int i = 0; i < 3; i++)
        {
            fixedUpdateMethod.Invoke(collisionImpulseTracker, null);
            Assert.AreEqual(0f, collisionImpulseTracker.impulseMagnitude);
            collisionImpulseTracker.impulseMagnitude = 10f * (i + 1);
        }
    }

    [Test]
    public void FixedUpdate_HandlesNegativeImpulseMagnitude()
    {
        collisionImpulseTracker.impulseMagnitude = -15f;
        fixedUpdateMethod.Invoke(collisionImpulseTracker, null);
        Assert.AreEqual(0f, collisionImpulseTracker.impulseMagnitude);
    }
}
