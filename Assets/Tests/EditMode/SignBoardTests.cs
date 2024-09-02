using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the SignBoard class in edit mode.
/// </summary>
public class SignBoardTests
{
    private GameObject _signBoardGameObject;
    private SignBoard _signBoard;

    [SetUp]
    public void SetUp()
    {
        _signBoardGameObject = new GameObject();
        _signBoard = _signBoardGameObject.AddComponent<SignBoard>();
        var meshRenderer = _signBoardGameObject.AddComponent<MeshRenderer>();
        Material symbolMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "symbolMaterial" };
        meshRenderer.sharedMaterials = new Material[]
        {
            new Material(Shader.Find("Standard")),
            new Material(Shader.Find("Standard")),
            symbolMaterial
        };
        _signBoard._symbolMat = symbolMaterial;
        _signBoard.textures = new Texture[] { Texture2D.whiteTexture };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_signBoardGameObject);
    }

    [Test]
    public void TexturesArray_ShouldNotBeNull()
    {
        Assert.NotNull(_signBoard.textures);
    }

    [Test]
    public void AssignedColourOverride_ShouldApplyCorrectly()
    {
        Color testColor = Color.red;
        _signBoard.assignedColourOverride = testColor;

        Assert.AreEqual(testColor, _signBoard.assignedColourOverride);
    }

    [Test]
    public void UpdateSignBoard_ShouldSetDefaultTexture_WhenSymbolNameIsInvalid()
    {
        string invalidSymbolName = "InvalidSymbol";
        _signBoard.SetSymbol(invalidSymbolName);

        _signBoard.UpdateSignBoard();

        Assert.AreEqual(_signBoard.textures[0], _signBoard._symbolMat.GetTexture("_BaseMap"));
    }

    [Test]
    public void TexturesArray_ShouldNotBeEmpty_ByDefault()
    {
        Assert.IsNotEmpty(_signBoard.textures);
    }

    [Test]
    public void SetSymbol_ShouldSetSymbolCorrectly()
    {
        string symbolName = "TestSymbol"; /* This symbol name is not in the textures array but checks if the SetSymbol method is working correctly */
        _signBoard.symbolNames = new string[] { symbolName };
        _signBoard.SetSymbol(symbolName);

        Assert.AreEqual(symbolName, _signBoard.selectedSymbolName);
    }

    [Test]
    public void UpdateSignBoard_ShouldSetSymbolTexture_WhenSymbolNameIsValid()
    {
        string symbolName = "default";
        Texture testTexture = new Texture2D(1, 1);
        _signBoard.symbolNames = new string[] { symbolName };
        _signBoard.textures = new Texture[] { testTexture };
        _signBoard.SetSymbol(symbolName);

        _signBoard.UpdateSignBoard();

        Assert.AreEqual(testTexture, _signBoard._symbolMat.GetTexture("_BaseMap"));
    }

    [Test]
    public void SetColourOverride_ShouldApplyCorrectly_WhenGivenColor()
    {
        Color testColor = Color.blue;
        _signBoard.SetColourOverride(testColor, true);

        Assert.AreEqual(testColor, _signBoard.assignedColourOverride);
        Assert.IsFalse(_signBoard.useDefaultColourArray);
    }

    [Test]
    public void SetColourOverride_ShouldApplyCorrectly_WhenGivenVector3()
    {
        Vector3 testColor = new Vector3(0, 255, 0);
        _signBoard.SetColourOverride(testColor, true);

        Color expectedColor = new Color(0, 1, 0);
        Assert.AreEqual(expectedColor, _signBoard.assignedColourOverride);
        Assert.IsFalse(_signBoard.useDefaultColourArray);
    }

    [Test]
    public void SetSize_ShouldClampSizeValues_WithinRange()
    {
        Vector3 testSize = new Vector3(0.2f, 3f, 1.5f);
        _signBoard.SetSize(testSize);

        Assert.AreEqual(new Vector3(0.5f, 2.5f, 1.5f), _signBoardGameObject.transform.localScale);
    }

    /* Test cases for each symbol name. Important: update the test cases if more symbols are added to the SignBoard class. */
    [Test]
    [TestCase("default")]
    [TestCase("right-arrow")]
    [TestCase("left-arrow")]
    [TestCase("up-arrow")]
    [TestCase("down-arrow")]
    [TestCase("u-turn-arrow")]
    [TestCase("letter-a")]
    [TestCase("letter-b")]
    [TestCase("letter-c")]
    [TestCase("square")]
    [TestCase("triangle")]
    [TestCase("circle")]
    [TestCase("star")]
    [TestCase("tick")]
    [TestCase("cross")]
    public void SetSymbol_ShouldApplySymbolCorrectly(string symbolName)
    {
        Texture testTexture = new Texture2D(1, 1);
        _signBoard.symbolNames = new string[] { symbolName };
        _signBoard.textures = new Texture[] { testTexture };
        _signBoard.SetSymbol(symbolName);

        _signBoard.UpdateSignBoard();

        Assert.AreEqual(testTexture, _signBoard._symbolMat.GetTexture("_BaseMap"));
    }
}