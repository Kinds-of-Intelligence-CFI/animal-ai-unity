using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngineExtensions;

/// <summary>
/// Tests for the TransformExtensions class.
/// </summary>
public class TransformExtensionsTests
{
    private GameObject parent;
    private GameObject child1;
    private GameObject child2;

    [SetUp]
    public void SetUp()
    {
        parent = new GameObject("Parent");

        child1 = new GameObject("Child1");
        child2 = new GameObject("Child2");

        child1.transform.SetParent(parent.transform);
        child2.transform.SetParent(parent.transform);

        /* A few tags for testing as if these work then the rest of the tags can be found */
        child1.tag = "agent";
        child2.tag = "arena";
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(parent);
    }

    [Test]
    public void FindChildrenWithTag_ReturnsCorrectChildren()
    {
        List<GameObject> taggedChildren = parent.transform.FindChildrenWithTag("agent");

        Assert.AreEqual(1, taggedChildren.Count);
        Assert.AreEqual(child1, taggedChildren[0]);
    }

    [Test]
    public void FindChildWithTag_ReturnsCorrectChild()
    {
        GameObject foundChild = parent.transform.FindChildWithTag("arena");

        Assert.AreEqual(child2, foundChild);
    }

    public class GameObjectExtensionsTests
    {
        private GameObject parent;
        private GameObject child1;
        private GameObject child2;

        [SetUp]
        public void SetUp()
        {
            parent = new GameObject("Parent");

            child1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            child1.transform.SetParent(parent.transform);
            child2.transform.SetParent(parent.transform);

            child1.transform.position = new Vector3(1, 0, 0);
            child2.transform.position = new Vector3(-1, 0, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void SetLayer_SetsLayerForAllChildren()
        {
            parent.SetLayer(5);

            Assert.AreEqual(5, parent.layer);
            Assert.AreEqual(5, child1.layer);
            Assert.AreEqual(5, child2.layer);
        }
    }
}