using UnityEngine;
using NUnit.Framework;
using Operations;

/// <summary>
/// Tests for the LimitedInvocationsOperation class in edit mode.
/// </summary>
public class LimitedInvocationsOperationTests
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

    private LimitedInvocationsOperation CreateLimitedInvocationsOperation(Operation op, int invocations
    )
    {
        var gameObject = new GameObject();
        var limitedInvocationsOperation = gameObject.AddComponent<LimitedInvocationsOperation>();
        limitedInvocationsOperation.operation = op;
        limitedInvocationsOperation.maxInvocations = invocations;
        var details = new AttachedObjectDetails("test_object", Vector3.zero);
        limitedInvocationsOperation.Initialize(details);
        return limitedInvocationsOperation;
    }

    private TestOperation CreateTestOperation(string name)
    {
        var gameObject = new GameObject();
        var operation = gameObject.AddComponent<TestOperation>();
        operation.GetType().GetConstructor(new[] { typeof(string) })?.Invoke(operation, new object[] { name });
        return operation;
    }

    [Test]
    public void Execute_OnlyExecutesUntilTheLimit()
    {
        const int iterations = 101;
        int maxInvocations = 100;

        var operation1 = CreateTestOperation("Operation1");
        var limitedInvocationsOperation = CreateLimitedInvocationsOperation(operation1, maxInvocations);

        for (int i = 0; i < iterations; i++)
        {
            limitedInvocationsOperation.execute();
        }

        Assert.That(operation1.executionCount, Is.EqualTo(maxInvocations),
            $"Operation1 executed {operation1.executionCount} times, expected {maxInvocations}");
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