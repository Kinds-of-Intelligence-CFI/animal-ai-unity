using UnityEngine;
using NUnit.Framework;
using System.Reflection;

/// <summary>
/// Tests for the DataZone class.
/// </summary>
public class DataZoneTests
{
    [Test]
    public void Start_SetsColliderToTrigger()
    {
        GameObject gameObject = new GameObject();
        DataZone dataZone = gameObject.AddComponent<DataZone>();
        BoxCollider collider = gameObject.AddComponent<BoxCollider>();

        InvokePrivateMethod(dataZone, "Start");

        Assert.IsTrue(collider.isTrigger);
    }

    [Test]
    public void Start_DoesNotThrowExceptionWhenNoCollider()
    {
        GameObject gameObject = new GameObject();
        DataZone dataZone = gameObject.AddComponent<DataZone>();

        Assert.DoesNotThrow(() => InvokePrivateMethod(dataZone, "Start"));
    }

    private void InvokePrivateMethod(object obj, string methodName)
    {
        MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(obj, null);
    }
}
