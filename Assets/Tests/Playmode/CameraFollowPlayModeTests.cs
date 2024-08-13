using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

/// <summary>
/// Tests for the CameraFollow class.
/// </summary>
public class CameraFollowPlayModeTests
{
    private GameObject _cameraObject;
    private CameraFollow _cameraFollow;
    private GameObject _followedObject;

    [SetUp]
    public void SetUp()
    {
        _cameraObject = new GameObject("Camera");
        _cameraFollow = _cameraObject.AddComponent<CameraFollow>();

        _followedObject = new GameObject("FollowedObject");
        _followedObject.transform.position = new Vector3(5, 5, 5);
        _followedObject.transform.rotation = Quaternion.Euler(45, 45, 45);

        _cameraFollow.followObj = _followedObject;
    }

    [UnityTest]
    public IEnumerator CameraFollowsObjectPositionAndRotation()
    {
        yield return null;

        Assert.AreEqual(
            _followedObject.transform.position,
            _cameraObject.transform.position,
            "Camera did not match the followed object's position."
        );
        Assert.AreEqual(
            _followedObject.transform.rotation,
            _cameraObject.transform.rotation,
            "Camera did not match the followed object's rotation."
        );
    }

    [UnityTest]
    public IEnumerator CameraThrowsExceptionWhenFollowObjIsNull()
    {
        _cameraFollow.followObj = null;

        LogAssert.Expect(
            LogType.Exception,
            "MissingReferenceException: The followObj is not assigned."
        );

        yield return null;

        /* Note: the test will pass if the expected exception is thrown */
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_cameraObject);
        Object.Destroy(_followedObject);
    }
}
