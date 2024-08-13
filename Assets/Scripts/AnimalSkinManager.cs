using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A class that manages the animal skins. It allows to change the animal skin by name or by ID.
/// </summary>
[System.Serializable]
public class MultiDimArray<T>
{
    public T[] array;
}

public class AnimalSkinManager : MonoBehaviour
{
    [Header("Animal Skins Settings")]
    public const int AnimalCount = 3;

    [Range(0, AnimalCount - 1)]
    public int AnimalSkinID;
    public string[] AnimalNames = new string[AnimalCount];
    public Mesh[] AnimalMeshes = new Mesh[AnimalCount];
    public MultiDimArray<Material>[] AnimalMaterials = new MultiDimArray<Material>[AnimalCount];

    private Dictionary<string, (Mesh mesh, Material[] materials)> animalDict =
        new Dictionary<string, (Mesh mesh, Material[] materials)>();

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        for (int i = 0; i < AnimalCount; i++)
        {
            animalDict[AnimalNames[i]] = (AnimalMeshes[i], AnimalMaterials[i].array);
        }

        RefreshSkin();
    }

    void RefreshSkin()
    {
        if (animalDict.TryGetValue(AnimalNames[AnimalSkinID], out var animalData))
        {
            meshFilter.mesh = animalData.mesh;
            meshRenderer.materials = animalData.materials;
        }
    }

    public void SetAnimalSkin(string skinName)
    {
        if (animalDict.ContainsKey(skinName))
        {
            AnimalSkinID = System.Array.IndexOf(AnimalNames, skinName);
            RefreshSkin();
        }
        else
        {
            SetAnimalSkin(Random.Range(0, AnimalCount));
        }
    }

    public void SetAnimalSkin(int skinID)
    {
        if (skinID >= 0 && skinID < AnimalCount)
        {
            AnimalSkinID = skinID;
            RefreshSkin();
        }
    }
}
