// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;

public class DeathZone : Goal
{

    public override void SetSize(Vector3 size)//This switches back to grandparent method @TODO change prefab.cs to allow direct access and have other children of Prefab.cs override separate method. (or refactor class heirarchy)
    {
        Vector3 clippedSize = Vector3.Max(sizeMin, Vector3.Min(sizeMax, size)) * sizeAdjustment;
        float sizeX = size.x < 0 ? Random.Range(sizeMin[0], sizeMax[0]) : clippedSize.x;
        float sizeY = size.y < 0 ? Random.Range(sizeMin[1], sizeMax[1]) : clippedSize.y;
        float sizeZ = size.z < 0 ? Random.Range(sizeMin[2], sizeMax[2]) : clippedSize.z;

        _height = sizeY;
        transform.localScale = new Vector3(sizeX * ratioSize.x,
                                            sizeY * ratioSize.y,
                                            sizeZ * ratioSize.z);

        GetComponent<Renderer>().material.SetVector("_ObjScale", new Vector3(sizeX, sizeY, sizeZ));
    }

    protected override float AdjustY(float yIn)
    {
        return -0.15f;
    }

}
