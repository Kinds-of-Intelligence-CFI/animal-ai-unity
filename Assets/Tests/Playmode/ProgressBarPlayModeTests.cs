using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Test class for the ProgressBar class.
/// </summary>
public class ProgressBarPlayModeTests
{
    private GameObject progressBarObject;
    private ProgressBar progressBar;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        progressBarObject = new GameObject("ProgressBar");

        CreateChildObject("Bar", typeof(Image));
        CreateChildObject("Text", typeof(TextMeshProUGUI));
        CreateChildObject("BarBackground", typeof(Image));
        CreateChildObject("PassMarker", typeof(RectTransform));

        progressBar = progressBarObject.AddComponent<ProgressBar>();

        yield return null;
    }

    private void CreateChildObject(string name, System.Type componentType)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(progressBarObject.transform);
        child.AddComponent(componentType);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(progressBarObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProgressBar_InitializesCorrectly()
    {
        yield return null;
        Assert.IsNotNull(progressBar);
        Assert.IsNull(progressBar.Title);  /* Expect null instead of empty string from ProgressBar.cs (Title is a string) */
        Assert.AreEqual(100, progressBar.MaxHealth);
        Assert.AreEqual(0, progressBar.MinHealth);
    }


    [UnityTest]
    public IEnumerator ProgressBar_UpdatesValueCorrectly()
    {
        progressBar.BarValue = 50;
        yield return null;
        Assert.AreEqual(50, progressBar.BarValue);
    }

    [UnityTest]
    public IEnumerator ProgressBar_ClampsValueWithinRange()
    {
        progressBar.BarValue = 150;
        yield return null;
        Assert.AreEqual(100, progressBar.BarValue);

        progressBar.BarValue = -10;
        yield return null;
        Assert.AreEqual(0, progressBar.BarValue);
    }

    [UnityTest]
    public IEnumerator ProgressBar_UpdatesTitleText()
    {
        progressBar.Title = "Health";
        progressBar.BarValue = 75;
        yield return null;
        TMP_Text titleText = progressBarObject.transform.Find("Text").GetComponent<TMP_Text>();
        Assert.AreEqual("Health: 75.0", titleText.text);
    }
}
