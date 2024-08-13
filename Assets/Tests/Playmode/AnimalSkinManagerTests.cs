using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for the AnimalSkinManager class.
/// </summary>
public class AnimalSkinManagerTests
{
    private GameObject testObject;
    private AnimalSkinManager skinManager;

    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject();
        testObject.AddComponent<MeshRenderer>();
        testObject.AddComponent<MeshFilter>();
        skinManager = testObject.AddComponent<AnimalSkinManager>();

        skinManager.AnimalNames = new string[] { "pig", "panda", "hedgehog" };
        skinManager.AnimalMeshes = new Mesh[]
        {
            /* Meshes for the animals, currently manually creating but should be in a loop in future maintenance */
            new Mesh(),
            new Mesh(),
            new Mesh()
        };

        skinManager.AnimalMaterials = new MultiDimArray<Material>[]
        {
            new MultiDimArray<Material>
            {
                array = new Material[] { new Material(Shader.Find("Standard")) }
            },
            new MultiDimArray<Material>
            {
                array = new Material[] { new Material(Shader.Find("Standard")) }
            },
            new MultiDimArray<Material>
            {
                array = new Material[] { new Material(Shader.Find("Standard")) }
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(testObject);
    }

    [UnityTest]
    public IEnumerator Initialization_InitializesCorrectly()
    {
        var actualMaterials = skinManager.GetComponent<MeshRenderer>().materials;

        Assert.AreEqual(
            skinManager.AnimalMaterials[0].array[0].shader,
            actualMaterials[0].shader,
            "Shaders do not match"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[0].array[0].color,
            actualMaterials[0].color,
            "Colors do not match"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[0].array[0].mainTexture,
            actualMaterials[0].mainTexture,
            "Main textures do not match"
        );

        yield return null;
    }

    [UnityTest]
    public IEnumerator SetAnimalSkin_ByName_ChangesSkin()
    {
        skinManager.SetAnimalSkin("panda");

        var actualMaterials = skinManager.GetComponent<MeshRenderer>().materials;

        /* Compare the properties of the materials (equals that of the shader materials in the Agent's skins section) */
        Assert.AreEqual(
            skinManager.AnimalMaterials[1].array[0].shader,
            actualMaterials[0].shader,
            "Shaders do not match"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[1].array[0].color,
            actualMaterials[0].color,
            "Colors do not match"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[1].array[0].mainTexture,
            actualMaterials[0].mainTexture,
            "Main textures do not match"
        );

        yield return null;
    }

    [UnityTest]
    public IEnumerator SetAnimalSkin_ByID_ChangesSkin()
    {
        skinManager.SetAnimalSkin(2);

        var actualMaterials = skinManager.GetComponent<MeshRenderer>().materials;

        /* Compare the properties of the materials (equals that of the shader materials in the Agent's skins section) */
        Assert.AreEqual(
            skinManager.AnimalMaterials[2].array[0].shader,
            actualMaterials[0].shader,
            "Shaders do not match"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[2].array[0].color,
            actualMaterials[0].color,
            "Colors do not match"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[2].array[0].mainTexture,
            actualMaterials[0].mainTexture,
            "Main textures do not match"
        );

        yield return null;
    }

    [UnityTest]
    public IEnumerator SetAnimalSkin_InvalidName_SelectsRandomSkin()
    {
        skinManager.SetAnimalSkin("InvalidName");

        var actualMaterials = skinManager.GetComponent<MeshRenderer>().materials;

        /* Check if the ID is valid and the materials are correctly applied */
        Assert.IsTrue(
            skinManager.AnimalSkinID >= 0
                && skinManager.AnimalSkinID < AnimalSkinManager.AnimalCount,
            "AnimalSkinID is out of range"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[skinManager.AnimalSkinID].array[0].shader,
            actualMaterials[0].shader,
            "Shaders do not match"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[skinManager.AnimalSkinID].array[0].color,
            actualMaterials[0].color,
            "Colors do not match"
        );
        Assert.AreEqual(
            skinManager.AnimalMaterials[skinManager.AnimalSkinID].array[0].mainTexture,
            actualMaterials[0].mainTexture,
            "Main textures do not match"
        );

        yield return null;
    }
}
