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

    [Test]
    public void AddComponent_AlsoAddsTheTriggerVolume_ThePrefabCarries()
    {
        GameObject gameObject = new GameObject();
        DataZone dataZone = gameObject.AddComponent<DataZone>();

        Assert.IsNotNull(gameObject.GetComponent<TriggerVolume>(), "DataZone requires a TriggerVolume on its object");
        Assert.AreSame(gameObject.GetComponent<TriggerVolume>(), dataZone.Volume);
    }

    [Test]
    public void Configure_SetsTheIDAndOperations_Once()
    {
        GameObject gameObject = new GameObject();
        DataZone dataZone = gameObject.AddComponent<DataZone>();
        var operations = new System.Collections.Generic.List<Operations.Operation>();

        dataZone.Configure("D1", operations);

        Assert.AreEqual("D1", dataZone.TriggerZoneID);
        Assert.AreSame(operations, dataZone.Operations);
        Assert.Throws<System.InvalidOperationException>(
            () => dataZone.Configure("D2", operations),
            "A DataZone is spawned afresh each arena, so configuring it twice is a bug"
        );
        Assert.AreEqual("D1", dataZone.TriggerZoneID, "The second call must not change anything");
    }

    [Test]
    public void Configure_WithNoIDOrOperations_UsesEmptyDefaults()
    {
        GameObject gameObject = new GameObject();
        DataZone dataZone = gameObject.AddComponent<DataZone>();

        dataZone.Configure(null, null);

        Assert.AreEqual("", dataZone.TriggerZoneID);
        Assert.IsEmpty(dataZone.Operations);
    }

    private void InvokePrivateMethod(object obj, string methodName)
    {
        MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(obj, null);
    }
}
