using UnityEngine;
using NUnit.Framework;

/// <summary>
/// Tests for the Goal class in edit mode.
/// </summary>
public class GoalTests
{
    private class TestGoal : Goal
    {
        public void TestApplyColorToRenderer(Renderer renderer, Color color, bool colorSpecified)
        {
            /* Using shared material here to avoid issues with changing materials in the editor */
            if (colorSpecified)
            {
                renderer.sharedMaterial.color = color;
                if (renderer.sharedMaterial.HasProperty("_BaseColor"))
                    renderer.sharedMaterial.SetColor("_BaseColor", color);
                if (renderer.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    renderer.sharedMaterial.SetColor("_EmissionColor", color);
                    renderer.sharedMaterial.EnableKeyword("_EMISSION");
                }
            }
            else
            {
                if (renderer.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    Color originalEmissionColor = renderer.sharedMaterial.GetColor("_EmissionColor");
                    renderer.sharedMaterial.SetColor("_EmissionColor", originalEmissionColor);
                    if (renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"))
                        renderer.sharedMaterial.EnableKeyword("_EMISSION");
                    else
                        renderer.sharedMaterial.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private TestGoal CreateTestGoal()
    {
        var gameObject = new GameObject();
        return gameObject.AddComponent<TestGoal>();
    }

    [Test]
    public void ApplyColorToRenderer_ModifiesExistingMaterial()
    {
        var testGoal = CreateTestGoal();
        var renderer = new GameObject().AddComponent<MeshRenderer>();
        var originalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.sharedMaterial = originalMaterial;
        var color = Color.green;

        testGoal.TestApplyColorToRenderer(renderer, color, true);

        Assert.AreEqual(originalMaterial, renderer.sharedMaterial);
        Assert.AreEqual(color, renderer.sharedMaterial.GetColor("_BaseColor"));
        Assert.AreEqual(color, renderer.sharedMaterial.GetColor("_EmissionColor"));
        Assert.IsTrue(renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"));
    }

    [Test]
    public void ApplyColorToRenderer_AppliesColorToExistingMaterial()
    {
        var testGoal = CreateTestGoal();
        var renderer = new GameObject().AddComponent<MeshRenderer>();
        var originalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.sharedMaterial = originalMaterial;
        var color = Color.green;

        testGoal.TestApplyColorToRenderer(renderer, color, true);

        Assert.AreEqual(color, renderer.sharedMaterial.GetColor("_BaseColor"));
        Assert.AreEqual(color, renderer.sharedMaterial.GetColor("_EmissionColor"));
        Assert.IsTrue(renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"));
    }

    [Test]
    public void ApplyColorToRenderer_ColorSpecified_SetsBaseColor()
    {
        var testGoal = CreateTestGoal();
        var renderer = new GameObject().AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var color = Color.red;

        testGoal.TestApplyColorToRenderer(renderer, color, true);

        Assert.AreEqual(color, renderer.sharedMaterial.GetColor("_BaseColor"));
    }

    [Test]
    public void ApplyColorToRenderer_ColorSpecified_SetsEmissionColor()
    {
        var testGoal = CreateTestGoal();
        var renderer = new GameObject().AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var color = Color.blue;

        testGoal.TestApplyColorToRenderer(renderer, color, true);

        Assert.AreEqual(color, renderer.sharedMaterial.GetColor("_EmissionColor"));
        Assert.IsTrue(renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"));
    }

    [Test]
    public void ApplyColorToRenderer_ColorNotSpecified_DoesNotChangeColor()
    {
        var testGoal = CreateTestGoal();
        var renderer = new GameObject().AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var originalColor = renderer.sharedMaterial.color;

        testGoal.TestApplyColorToRenderer(renderer, Color.green, false);

        Assert.AreEqual(originalColor, renderer.sharedMaterial.color);
    }

    [Test]
    public void ApplyColorToRenderer_ColorNotSpecified_CopiesOriginalEmissionColor()
    {
        var testGoal = CreateTestGoal();
        var renderer = new GameObject().AddComponent<MeshRenderer>();
        var testMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.sharedMaterial = testMaterial;
        var originalEmissionColor = Color.yellow;
        testMaterial.SetColor("_EmissionColor", originalEmissionColor);
        testMaterial.EnableKeyword("_EMISSION");

        testGoal.TestApplyColorToRenderer(renderer, Color.green, false);

        Assert.AreEqual(originalEmissionColor, renderer.sharedMaterial.GetColor("_EmissionColor"));
        Assert.IsTrue(renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"));

        Object.DestroyImmediate(testMaterial);
    }

    [Test]
    public void ApplyColorToRenderer_MaterialWithoutBaseColorProperty_SetsColor()
    {
        var testGoal = CreateTestGoal();
        var renderer = new GameObject().AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Unlit/Color"));
        var color = Color.magenta;

        testGoal.TestApplyColorToRenderer(renderer, color, true);

        Assert.AreEqual(color, renderer.sharedMaterial.color);
    }
}
