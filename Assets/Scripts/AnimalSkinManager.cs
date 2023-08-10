using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MultiDimArray<T>
{
    public T[] array;
}

public class AnimalSkinManager : MonoBehaviour
{
    public const int AnimalCount = 3;

    [Range(0, AnimalCount - 1)]
    public int AnimalSkinID;
    public string[] AnimalNames = new string[AnimalCount];
    public Mesh[] AnimalMeshes = new Mesh[AnimalCount];
    public MultiDimArray<Material>[] AnimalMaterials = new MultiDimArray<Material>[AnimalCount];

    private Dictionary<string, KeyValuePair<Mesh, Material[]>> animalDict =
        new Dictionary<string, KeyValuePair<Mesh, Material[]>>();

    void Awake()
    {
        for (int i = 0; i < AnimalCount; ++i)
        {
            animalDict[AnimalNames[i]] = new KeyValuePair<Mesh, Material[]>(
                AnimalMeshes[i],
                AnimalMaterials[i].array
            );
        }

        RefreshSkin();
    }

    void RefreshSkin()
    {
        this.GetComponent<MeshFilter>().mesh = animalDict[AnimalNames[AnimalSkinID]].Key;
        this.GetComponent<MeshRenderer>().materials = animalDict[AnimalNames[AnimalSkinID]].Value;
    }

    public void SetAnimalSkin(string skin)
    {
        int id = -1;
        for (int x = 0; x < AnimalCount; ++x)
        {
            id = (skin == AnimalNames[x]) ? x : id;
        }
        SetAnimalSkin((id == -1) ? Random.Range(0, AnimalCount) : id);
    }

    void SetAnimalSkin(int skinID)
    {
        if (skinID >= 0 && skinID < AnimalCount)
            AnimalSkinID = skinID;
        RefreshSkin();
    }
}
