using UnityEngine;
using NUnit.Framework;
using Operations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tests for the OperationFromList class in edit mode.
/// </summary>
public class OperationFromListTests
{
    /// <summary>
    /// Dummy operation that tracks execution count for testing
    /// </summary>
    private class TestOperation : Operation
    {
        public int executionCount { get; private set; } = 0;
        public string operationName { get; private set; }

        public TestOperation(string name)
        {
            operationName = name;
        }

        public override void execute()
        {
            executionCount++;
        }

        public void ResetCount()
        {
            executionCount = 0;
        }
    }

    private OperationFromList CreateOperationFromList()
    {
        var gameObject = new GameObject();
        var operationFromList = gameObject.AddComponent<OperationFromList>();
        var details = new AttachedObjectDetails("test_object", Vector3.zero);
        operationFromList.Initialize(details);
        return operationFromList;
    }

    private TestOperation CreateTestOperation(string name)
    {
        var gameObject = new GameObject();
        var operation = gameObject.AddComponent<TestOperation>();
        operation.GetType().GetConstructor(new[] { typeof(string) })?.Invoke(operation, new object[] { name });
        return operation;
    }

    [Test]
    public void Execute_WithEqualWeights_DistributesExecutionsEvenly()
    {
        const int iterations = 1000;
        const float tolerance = 0.15f; // Allow 15% deviation from expected distribution

        var operationFromList = CreateOperationFromList();
        var operation1 = CreateTestOperation("Operation1");
        var operation2 = CreateTestOperation("Operation2");

        operationFromList.operations = new List<Operation> { operation1, operation2 };
        operationFromList.operationWeights = new List<float> { 1.0f, 1.0f };

        for (int i = 0; i < iterations; i++)
        {
            operationFromList.execute();
        }

        float expectedCount = iterations / 2.0f;
        float allowedDeviation = expectedCount * tolerance;

        Assert.That(operation1.executionCount, Is.InRange(expectedCount - allowedDeviation, expectedCount + allowedDeviation),
            $"Operation1 executed {operation1.executionCount} times, expected around {expectedCount}");
        Assert.That(operation2.executionCount, Is.InRange(expectedCount - allowedDeviation, expectedCount + allowedDeviation),
            $"Operation2 executed {operation2.executionCount} times, expected around {expectedCount}");
        Assert.AreEqual(iterations, operation1.executionCount + operation2.executionCount,
            "Total executions should equal the number of iterations");
    }

    [Test]
    public void Execute_WithWeightedDistribution_RespectsWeights()
    {
        const int iterations = 1000;
        const float tolerance = 0.2f; // Allow 20% deviation due to random nature

        var operationFromList = CreateOperationFromList();
        var operation1 = CreateTestOperation("HighWeight");
        var operation2 = CreateTestOperation("LowWeight");

        // Operation1 should be selected 3 times more often than Operation2
        operationFromList.operations = new List<Operation> { operation1, operation2 };
        operationFromList.operationWeights = new List<float> { 3.0f, 1.0f };

        for (int i = 0; i < iterations; i++)
        {
            operationFromList.execute();
        }

        float expectedOperation1Count = iterations * 0.75f; // 3/4 of total
        float expectedOperation2Count = iterations * 0.25f; // 1/4 of total
        float allowedDeviation1 = expectedOperation1Count * tolerance;
        float allowedDeviation2 = expectedOperation2Count * tolerance;

        Assert.That(operation1.executionCount, Is.InRange(expectedOperation1Count - allowedDeviation1, expectedOperation1Count + allowedDeviation1),
            $"High weight operation executed {operation1.executionCount} times, expected around {expectedOperation1Count}");
        Assert.That(operation2.executionCount, Is.InRange(expectedOperation2Count - allowedDeviation2, expectedOperation2Count + allowedDeviation2),
            $"Low weight operation executed {operation2.executionCount} times, expected around {expectedOperation2Count}");
        Assert.AreEqual(iterations, operation1.executionCount + operation2.executionCount,
            "Total executions should equal the number of iterations");
    }

    [Test]
    public void Execute_WithSingleOperation_AlwaysExecutesThatOperation()
    {
        const int iterations = 100;

        var operationFromList = CreateOperationFromList();
        var singleOperation = CreateTestOperation("OnlyOperation");

        operationFromList.operations = new List<Operation> { singleOperation };
        operationFromList.operationWeights = new List<float> { 1.0f };

        for (int i = 0; i < iterations; i++)
        {
            operationFromList.execute();
        }

        Assert.AreEqual(iterations, singleOperation.executionCount,
            "Single operation should be executed every time");
    }

    [Test]
    public void Execute_WithZeroWeight_NeverExecutesZeroWeightOperation()
    {
        const int iterations = 100;

        var operationFromList = CreateOperationFromList();
        var normalOperation = CreateTestOperation("NormalOperation");
        var zeroWeightOperation = CreateTestOperation("ZeroWeightOperation");

        operationFromList.operations = new List<Operation> { normalOperation, zeroWeightOperation };
        operationFromList.operationWeights = new List<float> { 1.0f, 0.0f };

        for (int i = 0; i < iterations; i++)
        {
            operationFromList.execute();
        }

        Assert.AreEqual(iterations, normalOperation.executionCount,
            "Normal weight operation should be executed every time");
        Assert.AreEqual(0, zeroWeightOperation.executionCount,
            "Zero weight operation should never be executed");
    }

    [Test]
    public void Execute_WithThreeOperations_DistributesAccordingToWeights()
    {
        const int iterations = 1200; // Divisible by 6 for cleaner expected values
        const float tolerance = 0.2f;

        var operationFromList = CreateOperationFromList();
        var operation1 = CreateTestOperation("Weight3");
        var operation2 = CreateTestOperation("Weight2");
        var operation3 = CreateTestOperation("Weight1");

        // Weights: 3:2:1 ratio
        operationFromList.operations = new List<Operation> { operation1, operation2, operation3 };
        operationFromList.operationWeights = new List<float> { 3.0f, 2.0f, 1.0f };

        for (int i = 0; i < iterations; i++)
        {
            operationFromList.execute();
        }

        float expectedOperation1Count = iterations * 0.5f;  // 3/6 of total
        float expectedOperation2Count = iterations * 0.333f; // 2/6 of total
        float expectedOperation3Count = iterations * 0.167f; // 1/6 of total

        Assert.That(operation1.executionCount, Is.InRange(expectedOperation1Count * (1 - tolerance), expectedOperation1Count * (1 + tolerance)),
            $"Weight 3 operation executed {operation1.executionCount} times, expected around {expectedOperation1Count}");
        Assert.That(operation2.executionCount, Is.InRange(expectedOperation2Count * (1 - tolerance), expectedOperation2Count * (1 + tolerance)),
            $"Weight 2 operation executed {operation2.executionCount} times, expected around {expectedOperation2Count}");
        Assert.That(operation3.executionCount, Is.InRange(expectedOperation3Count * (1 - tolerance), expectedOperation3Count * (1 + tolerance)),
            $"Weight 1 operation executed {operation3.executionCount} times, expected around {expectedOperation3Count}");
        Assert.AreEqual(iterations, operation1.executionCount + operation2.executionCount + operation3.executionCount,
            "Total executions should equal the number of iterations");
    }

    [Test]
    public void Initialize_SetsAttachedObjectDetails()
    {
        var operationFromList = CreateOperationFromList();
        var expectedDetails = new AttachedObjectDetails("test_id", new Vector3(1, 2, 3));

        operationFromList.Initialize(expectedDetails);

        Assert.AreEqual(expectedDetails.ID, operationFromList.attachedObjectDetails.ID);
        Assert.AreEqual(expectedDetails.location, operationFromList.attachedObjectDetails.location);
    }

    [Test]
    public void Execute_WithVerySmallWeights_StillFunctionsCorrectly()
    {
        const int iterations = 100;

        var operationFromList = CreateOperationFromList();
        var operation1 = CreateTestOperation("SmallWeight1");
        var operation2 = CreateTestOperation("SmallWeight2");

        operationFromList.operations = new List<Operation> { operation1, operation2 };
        operationFromList.operationWeights = new List<float> { 0.001f, 0.001f };

        for (int i = 0; i < iterations; i++)
        {
            operationFromList.execute();
        }

        Assert.AreEqual(iterations, operation1.executionCount + operation2.executionCount,
            "Should execute operations even with very small weights");
        Assert.That(operation1.executionCount, Is.GreaterThan(0),
            "First operation should be executed at least once");
        Assert.That(operation2.executionCount, Is.GreaterThan(0),
            "Second operation should be executed at least once");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up any GameObjects created during tests
        var gameObjects = Object.FindObjectsOfType<GameObject>();
        foreach (var go in gameObjects)
        {
            if (go.name.Contains("(Clone)") || go.GetComponent<TestOperation>() != null || go.GetComponent<OperationFromList>() != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}