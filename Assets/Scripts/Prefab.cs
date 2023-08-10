using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
// using UnityEngineExtensions;
using PrefabInterface;
using System.Reflection;

/// <summary>
/// A Prefab represents a GameObject that cna be spawned in an arena, it also contains the range of
/// values that the user can pass as parameters
/// </summary>
public class Prefab : MonoBehaviour, IPrefab
{
    public Vector2 rotationRange;
    public Vector3 sizeMin;
    public Vector3 sizeMax;
    public bool canRandomizeColor = true;
    public Vector3 ratioSize;
    public float sizeAdjustement = 0.999f;

    // To scale textures on dynamically-sized objects
    public bool textureUVOverride = false;

    public bool typicalOrigin = true;
    protected float _height;

    public virtual void SetColor(Vector3 color)
    {
        if (canRandomizeColor)
        {
            Color newColor = new Color();
            newColor.a = 1f;
            newColor.r = color.x >= 0 ? color.x / 255f : Random.Range(0f, 1f);
            newColor.g = color.y >= 0 ? color.y / 255f : Random.Range(0f, 1f);
            newColor.b = color.z >= 0 ? color.z / 255f : Random.Range(0f, 1f);

            if (GetComponent<Renderer>() != null)
            {
                GetComponent<Renderer>().material.color = newColor;
            }
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (
                    r.material.GetFloat("_Surface") != 1 /*=Transparent*/
                )
                    r.material.color = newColor;
            }
        }
    }

    public virtual void SetSize(Vector3 size)
    {
        Vector3 clippedSize = Vector3.Max(sizeMin, Vector3.Min(sizeMax, size)) * sizeAdjustement;
        float sizeX = size.x < 0 ? Random.Range(sizeMin[0], sizeMax[0]) : clippedSize.x;
        float sizeY = size.y < 0 ? Random.Range(sizeMin[1], sizeMax[1]) : clippedSize.y;
        float sizeZ = size.z < 0 ? Random.Range(sizeMin[2], sizeMax[2]) : clippedSize.z;

        _height = sizeY;
        transform.localScale = new Vector3(
            sizeX * ratioSize.x,
            sizeY * ratioSize.y,
            sizeZ * ratioSize.z
        );

        if (textureUVOverride)
        {
            RescaleUVs();
        }
    }

    public virtual Vector3 GetRotation(float rotationY)
    {
        return new Vector3(
            0,
            rotationY < 0 ? Random.Range(rotationRange.x, rotationRange.y) : rotationY,
            0
        );
    }

    public virtual Vector3 GetPosition(
        Vector3 position,
        Vector3 boundingBox,
        float rangeX,
        float rangeZ
    )
    {
        float xBound = boundingBox.x;
        float zBound = boundingBox.z;
        float xOut =
            position.x < 0
                ? Random.Range(xBound, rangeX - xBound)
                : Math.Max(0, Math.Min(position.x, rangeX));
        float yOut = Math.Max(position.y, 0);
        float zOut =
            position.z < 0
                ? Random.Range(zBound, rangeZ - zBound)
                : Math.Max(0, Math.Min(position.z, rangeZ));

        return new Vector3(xOut, AdjustY(yOut), zOut);
    }

    protected virtual float AdjustY(float yIn)
    {
        return yIn + (typicalOrigin ? (_height / 2) : 0) + 0.01f;
    }

    protected virtual void RescaleUVs(bool child = false, GameObject childOverride = null)
    {
        //if (this.name.ToLower().Contains("ramp")) { Debug.Log(this.name + " CALLING RescaleUVs() " + child + ", " + childOverride); }
        Renderer R =
            (child) ? childOverride.GetComponent<Renderer>() : this.GetComponent<Renderer>();
        MeshFilter MF =
            (child) ? childOverride.GetComponent<MeshFilter>() : this.GetComponent<MeshFilter>();
        if (R != null && R.material.GetTexture("_BaseMap") != null)
        {
            MF.sharedMesh = Instantiate<Mesh>(MF.mesh);
            Mesh MESH = MF.sharedMesh;

            Transform T = /*(child) ? transform.parent :*/
            transform;

            Vector2[] uvs = new Vector2[MESH.uv.Length];
            Dictionary<Vector3, Vector2Int> uvStretchLookup = new Dictionary<Vector3, Vector2Int>
            {
                { new Vector3(0f, 0f, 1f), new Vector2Int(0, 1) },
                { new Vector3(0f, 1f, 0f), new Vector2Int(0, 2) },
                { new Vector3(0f, 0f, -1f), new Vector2Int(0, 1) },
                { new Vector3(0f, -1f, 0f), new Vector2Int(0, 2) },
                { new Vector3(-1f, 0f, 0f), new Vector2Int(2, 1) },
                { new Vector3(1f, 0f, 0f), new Vector2Int(2, 1) }
            };
            Vector2Int n;
            bool b;
            Vector2Int d = new Vector2Int(0, 1);
            for (int i = 0; i < uvs.Length; ++i)
            {
                b = uvStretchLookup.TryGetValue(MESH.normals[i], out n);
                if (b)
                {
                    uvs[i].x = (MESH.uv[i].x > 0) ? T.localScale[n.x] : 0;
                    uvs[i].y = (MESH.uv[i].y > 0) ? T.localScale[n.y] : 0;
                }
                else
                {
                    uvs[i].x = (MESH.uv[i].x > 0) ? T.localScale[0] : 0;
                    uvs[i].y =
                        (MESH.uv[i].y > 0)
                            ? Mathf.Sqrt(
                                Mathf.Pow(T.localScale[1], 2) + Mathf.Pow(T.localScale[2], 2)
                            )
                            : 0;
                }
            }
            MESH.uv = uvs;
        }
        else if (!child)
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                RescaleUVs(true, (transform.GetChild(i).gameObject));
            }
        }
    }

    private void SetOptAbstract<Type>(Type v, MethodBase method)
    {
        Debug.Log(method.ToString() + " activated in Prefab with value " + v.ToString());
    }

    private MethodBase m;

    public virtual void SetDelay(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetInitialValue(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetFinalValue(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetChangeRate(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetSpawnCount(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetTimeBetweenSpawns(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetRipenTime(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetDoorDelay(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetTimeBetweenDoorOpens(float v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }

    public virtual void SetSpawnColor(Vector3 v)
    {
        m = MethodBase.GetCurrentMethod();
        SetOptAbstract(v, m);
    }
}
