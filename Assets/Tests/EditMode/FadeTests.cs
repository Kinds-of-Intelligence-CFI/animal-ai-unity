using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Tests for the Fade class.
/// </summary>
public class FadeTests
{
    private GameObject gameObject;
    private Fade fade;
    private Image image;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject("TestObject");
        fade = gameObject.AddComponent<Fade>();

        GameObject imageObject = new GameObject("TestImage");
        imageObject.AddComponent<RectTransform>();
        image = imageObject.AddComponent<Image>();
        image.transform.SetParent(gameObject.transform, false);

        SetPrivateField(fade, "_image", image);
        fade.fadeSpeed = 1.0f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void ResetFade_ShouldSetAlphaToZero()
    {
        fade.ResetFade();
        Assert.AreEqual(0f, image.color.a);
    }

    [Test]
    public void StartFade_ShouldSetAlphaBasedOnFadeDirection()
    {
        fade.ResetFade();
        fade.StartFade();

        Assert.AreEqual(1f, image.color.a);

        fade.StartFade();
        Assert.AreEqual(0f, image.color.a);
    }

    [UnityTest]
    [UnityPlatform(
        RuntimePlatform.OSXPlayer,
        RuntimePlatform.WindowsPlayer,
        RuntimePlatform.LinuxPlayer
    )]
    public IEnumerator FadeOutEnum_ShouldChangeAlphaOverTime()
    {
        fade.StartFade();

        float initialAlpha = image.color.a;

        yield return new WaitForSeconds(0.5f);

        Assert.AreNotEqual(initialAlpha, image.color.a);
    }

    [Test]
    public void StartFade_ShouldHandleNullImageGracefully()
    {
        SetPrivateField(fade, "_image", null);

        Assert.DoesNotThrow(() => fade.StartFade());
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType()
            .GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
        field.SetValue(obj, value);
    }
}
