using UnityEngine;
using NUnit.Framework;
using PrefabInterface;

/// <summary>
/// Tests for the PrefabInterface class.
/// Tests the logic of the Prefab class.
/// </summary>
public class PrefabsInterfaceTests
{
    private class MockPrefab : IPrefab
    {
        public Vector3 Size { get; private set; }
        public Vector3 Color { get; private set; }

        public void SetSize(Vector3 size)
        {
            Size = size;
        }

        public void SetColor(Vector3 color)
        {
            Color = color;
        }

        public Vector3 GetRotation(float rotationY)
        {
            return new Vector3(0, rotationY, 0);
        }

        public Vector3 GetPosition(Vector3 position, Vector3 boundingBox, float rangeX, float rangeZ)
        {
            float x = Random.Range(position.x - rangeX, position.x + rangeX);
            float z = Random.Range(position.z - rangeZ, position.z + rangeZ);
            return new Vector3(x, position.y + boundingBox.y / 2, z);
        }
    }

    private MockPrefab prefab;

    [SetUp]
    public void SetUp()
    {
        prefab = new MockPrefab();
    }

    [Test]
    public void SetSize_SetsCorrectSize()
    {
        Vector3 expectedSize = new Vector3(1, 2, 3);
        prefab.SetSize(expectedSize);
        Assert.AreEqual(expectedSize, prefab.Size);
    }

    [Test]
    public void SetColor_SetsCorrectColor()
    {
        Vector3 expectedColor = new Vector3(0.5f, 0.7f, 0.9f);
        prefab.SetColor(expectedColor);
        Assert.AreEqual(expectedColor, prefab.Color);
    }

    [Test]
    public void GetRotation_ReturnsCorrectRotation()
    {
        float rotationY = 45f;
        Vector3 expectedRotation = new Vector3(0, rotationY, 0);
        Vector3 actualRotation = prefab.GetRotation(rotationY);
        Assert.AreEqual(expectedRotation, actualRotation);
    }

    [Test]
    public void GetPosition_ReturnsPositionWithinRange()
    {
        Vector3 position = new Vector3(0, 0, 0);
        Vector3 boundingBox = new Vector3(1, 2, 1);
        float rangeX = 5f;
        float rangeZ = 5f;

        Vector3 result = prefab.GetPosition(position, boundingBox, rangeX, rangeZ);

        Assert.GreaterOrEqual(result.x, position.x - rangeX);
        Assert.LessOrEqual(result.x, position.x + rangeX);
        Assert.AreEqual(position.y + boundingBox.y / 2, result.y);
        Assert.GreaterOrEqual(result.z, position.z - rangeZ);
        Assert.LessOrEqual(result.z, position.z + rangeZ);
    }
}
