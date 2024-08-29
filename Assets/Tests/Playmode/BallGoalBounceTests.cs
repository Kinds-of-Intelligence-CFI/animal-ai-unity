using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

/// <summary>
/// Tests for the BallGoalBounce class.
/// </summary>
public class BallGoalBounceTests
{
    private GameObject _ballGameObject;
    private BallGoalBounce _ballGoalBounce;
    private Rigidbody _rBody;

    [SetUp]
    public void Setup()
    {
        _ballGameObject = new GameObject("Ball");
        _rBody = _ballGameObject.AddComponent<Rigidbody>();
        _ballGoalBounce = _ballGameObject.AddComponent<BallGoalBounce>();

        _ballGoalBounce.forceToApply = 10f;
        _ballGoalBounce.maximumVelocity = 15f;
    }

    [UnityTest]
    public IEnumerator BallGoalBounce_ForceIsAppliedOnStart()
    {
        yield return new WaitForFixedUpdate();

        Assert.IsTrue(
            _rBody.velocity.magnitude > 0,
            "Rigidbody should have velocity after force is applied."
        );
    }

    [UnityTest]
    public IEnumerator BallGoalBounce_VelocityDoesNotExceedMaximumVelocity()
    {
        yield return new WaitForSeconds(0.1f);

        Assert.LessOrEqual(
            _rBody.velocity.magnitude,
            _ballGoalBounce.maximumVelocity,
            "Rigidbody velocity should not exceed the maximum velocity."
        );
    }

    [UnityTest]
    public IEnumerator BallGoalBounce_RigidbodyIsInitialized()
    {
        yield return null;

        Assert.IsNotNull(_rBody, "Rigidbody should be initialized.");
        Assert.AreEqual(
            _rBody,
            _ballGoalBounce.GetComponent<Rigidbody>(),
            "Rigidbody should be correctly assigned."
        );
    }

    [UnityTest]
    public IEnumerator BallGoalBounce_ForceDirectionIsForward()
    {
        /* Wait for the next fixed update to ensure the force is applied */
        yield return new WaitForFixedUpdate();
        yield return null;

        /* XZ directions only, no need to check Y */
        Vector3 expectedDirectionXZ = new Vector3(
            _ballGameObject.transform.forward.x,
            0,
            _ballGameObject.transform.forward.z
        ).normalized;
        Vector3 actualDirectionXZ = new Vector3(_rBody.velocity.x, 0, _rBody.velocity.z).normalized;

        Assert.AreEqual(
            expectedDirectionXZ,
            actualDirectionXZ,
            "Force should be applied in the forward direction in the XZ plane."
        );
    }

    [TearDown]
    public void Teardown()
    {
        GameObject.Destroy(_ballGameObject);
    }
}
