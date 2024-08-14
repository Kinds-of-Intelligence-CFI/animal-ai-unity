using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for the SizeChangeGoal class.
/// Note: needs more tests to cover all cases.
/// </summary>
public class SizeChangeGoalTests
{
    private GameObject gameObject;
    private SizeChangeGoal sizeChangeGoal;

    [SetUp]
    public void Setup()
    {
        gameObject = new GameObject();
        sizeChangeGoal = gameObject.AddComponent<SizeChangeGoal>();

        sizeChangeGoal.initialSize = 5f;
        sizeChangeGoal.finalSize = 1f;
        sizeChangeGoal.sizeChangeRate = -0.1f;

        sizeChangeGoal.sizeMin = new Vector3(1f, 1f, 1f);
        sizeChangeGoal.sizeMax = new Vector3(5f, 5f, 5f);

        sizeChangeGoal.fixedFrameDelay = 5;

        sizeChangeGoal.InitializeValues();
        sizeChangeGoal.SetSize(sizeChangeGoal.initialSize * Vector3.one);
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(sizeChangeGoal);
        Object.Destroy(gameObject);
        Debug.Log("SizeChangeGoal destroyed");
    }

    [UnityTest]
    public IEnumerator VerifySizeChangeOverTime()
    {
        yield return new WaitForSeconds(0.1f);

        yield return new WaitForSeconds(1.5f);

        Assert.Less(
            sizeChangeGoal.transform.localScale.x,
            5f,
            "Scale should have decreased after delayCounter reached zero."
        );
    }
}
