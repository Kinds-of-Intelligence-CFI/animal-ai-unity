using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

/// <summary>
/// Tests for the CameraCollision class.
/// </summary>
public class CameraCollisionPlayModeTests
{
    private GameObject _cameraObject;
    private CameraCollision _cameraCollision;
    private GameObject _cameraParent;

    [SetUp]
    public void SetUp()
    {
        _cameraParent = new GameObject("CameraParent");
        _cameraObject = new GameObject("Camera");

        _cameraObject.transform.SetParent(_cameraParent.transform);
        _cameraObject.transform.localPosition = new Vector3(0, 0, 4);

        _cameraCollision = _cameraObject.AddComponent<CameraCollision>();
        _cameraCollision.minDistance = 1.0f;
        _cameraCollision.maxDistance = 4.0f;
        _cameraCollision.smooth = 100.0f;
    }

    [UnityTest]
    public IEnumerator CameraMovesCloserOnCollision()
    {
        var blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObject.transform.position = new Vector3(0, 0, 2.5f);

        /* Rotate the parent to align with the dollyDir (might be necessary) */
        _cameraParent.transform.rotation = Quaternion.identity;

        yield return new WaitForSeconds(0.5f); /* Give time for the camera to potentially move */

        Assert.Less(_cameraCollision.distance, 4.0f, "Camera did not move closer after collision.");
    }

    [UnityTest]
    public IEnumerator CameraMaintainsMaxDistanceWithoutCollision()
    {
        yield return new WaitForSeconds(0.1f); /* Give time for the camera to potentially move (if it does, it should reset) */
        Assert.AreEqual(_cameraCollision.maxDistance, _cameraCollision.distance);
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_cameraObject);
        Object.Destroy(_cameraParent);
    }
}
