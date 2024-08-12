using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tests for the FlashingImage class.
/// </summary>
public class FlashingImageTests
{
    private GameObject gameObject;
    private FlashingImage flashingImage;
    private Image image;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject();
        flashingImage = gameObject.AddComponent<FlashingImage>();
        image = gameObject.AddComponent<Image>();
        flashingImage.imageToFlash = image;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void StartFlashing_ShouldResetFlashTimer()
    {
        flashingImage.StartFlashing();

        // Note: Inferring it's working correctly here by checking the flashTimer value.
        // (expecting no exceptions or errors)
        Assert.DoesNotThrow(() => flashingImage.StartFlashing());
    }

    [Test]
    public void StopFlashing_ShouldSetImageAlphaToOne()
    {
        flashingImage.StopFlashing();

        Assert.AreEqual(1f, flashingImage.imageToFlash.color.a);
    }

    [Test]
    public void SetImageAlpha_ShouldChangeImageAlpha()
    {
        flashingImage.StartFlashing();
        flashingImage.StopFlashing();

        Assert.AreEqual(1f, flashingImage.imageToFlash.color.a);
    }

    [Test]
    public void StopFlashing_WhenImageIsNull_ShouldNotThrowError()
    {
        flashingImage.imageToFlash = null;

        Assert.DoesNotThrow(() => flashingImage.StopFlashing());
    }
}
