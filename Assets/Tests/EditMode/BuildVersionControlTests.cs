using NUnit.Framework;
using UnityEngine;
using TMPro;

/// <summary>
/// Tests for the BuildVersionControl class.
/// </summary>
public class BuildVersionControlTests
{
    private GameObject gameObject;
    private BuildVersionControl buildVersionControl;
    private TextMeshProUGUI tmpText;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject();
        tmpText = gameObject.AddComponent<TextMeshProUGUI>();
        buildVersionControl = gameObject.AddComponent<BuildVersionControl>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void SetBuildVersionText()
    {
        buildVersionControl.Awake();

        Assert.AreEqual("Build: " + Application.version, tmpText.text);
    }
}