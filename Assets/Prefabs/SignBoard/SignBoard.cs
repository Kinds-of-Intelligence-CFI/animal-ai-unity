using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A prefab for the SignBoard game object that can display a symbol and a colour.
/// </summary>
public class SignBoard : Prefab
{
    [Header("SignBoard Properties")]
    private Material _symbolMat;
    private MeshRenderer _meshRenderer;
    public string selectedSymbolName;
    public string[] symbolNames;
    public Texture[] textures;
    public Color[] colours;
    private int texIndex;
    public bool useDefaultColourArray;

    public Color assignedColourOverride;
    private System.Random RNG = new System.Random();

    void Awake()
    {
        _symbolMat = gameObject.GetComponent<MeshRenderer>().materials[2];
        if (!_symbolMat.name.Contains("symbol"))
        {
            Debug.Log("WARNING: a SignBoard may not have found the correct symbol material!!");
        }
        SetSymbol(selectedSymbolName);
        _meshRenderer = gameObject.GetComponent<MeshRenderer>();
    }

    public void SetSymbol(string s, bool needsUpdating = false)
    {
        selectedSymbolName = s;
        texIndex = Array.IndexOf(symbolNames, selectedSymbolName);
        if (needsUpdating)
        {
            UpdateSignBoard();
        }
    }

    public void SetColourOverride(
        Color c,
        bool activateOverride = false,
        bool needsUpdating = false
    )
    {
        assignedColourOverride = c;
        if (activateOverride)
        {
            useDefaultColourArray = false;
            if (needsUpdating)
            {
                UpdateSignBoard();
            }
        }
    }

    public void SetColourOverride(
        Vector3 v,
        bool activateOverride = false,
        bool needsUpdating = false
    )
    {
        Color c = new Color(v.x / 255.0f, v.y / 255.0f, v.z / 255.0f);
        SetColourOverride(c, activateOverride, needsUpdating);
    }

    public void UpdateSignBoard()
    {
        bool specialCodeCase = false;

        if (texIndex == -1)
        {
            char c = selectedSymbolName[0];
            if ((c >= '0' && c <= '9') || c == '*')
            {
                Texture2D tex;
                specialCodeCase = parseSpecialTextureCode(selectedSymbolName, out tex);
                if (specialCodeCase)
                {
                    _symbolMat.SetTexture("_BaseMap", tex);
                }
            }
        }

        if (useDefaultColourArray)
        {
            if (!specialCodeCase)
            {
                KeyValuePair<Texture, Color> texture_colour_pair = getTextureAndColourByIndex(
                    texIndex
                );
                _symbolMat.SetTexture("_BaseMap", texture_colour_pair.Key);
                _symbolMat.color = texture_colour_pair.Value;
            }
            else
            {
                _symbolMat.color = Color.white;
            }
        }
        else
        {
            if (!specialCodeCase)
            {
                Texture texture = getTextureByIndex(texIndex);
                _symbolMat.SetTexture("_BaseMap", texture);
            }

            _symbolMat.color = assignedColourOverride;
        }
    }

    private Texture getTextureByIndex(int index)
    {
        if (index == -1)
        {
            Debug.Log(
                "WARNING: a SignBoard has not been given a valid symbol name! Defaulting to empty texture."
            );
        }
        index = (index >= 0 && index < textures.Length) ? index : 0;
        return textures[index];
    }

    private KeyValuePair<Texture, Color> getTextureAndColourByIndex(int index)
    {
        if (index == -1)
        {
            Debug.Log(
                "WARNING: a SignBoard has not been given a valid symbol name! Defaulting to empty texture..."
            );
        }
        index = (index >= 0 && index < symbolNames.Length) ? index : 0;
        return new KeyValuePair<Texture, Color>(textures[index], colours[index]);
    }

    public override void SetSize(Vector3 size)
    {
        for (int i = 0; i < 3; ++i)
        {
            if (size[i] != -1)
            {
                size[i] = Mathf.Clamp((float)size[i], 0.5f, 2.5f);
            }
        }
        base.SetSize((size == Vector3.one * -1) ? Vector3.one : size);
    }

    bool parseSpecialTextureCode(string texCode, out Texture2D tex)
    {
        int pixelWidth,
            pixelHeight;
        Color[] texCols;

        int xIndex = texCode.IndexOf('x');
        if (!(xIndex == -1 || xIndex == texCode.Length))
        {
            string[] splitCode = texCode.Split('x');
            if (splitCode.Length != 2)
            {
                tex = null;
                return false;
            }
            bool dimensionParseSuccess = int.TryParse(splitCode[0], out pixelWidth);
            dimensionParseSuccess =
                int.TryParse(splitCode[1], out pixelHeight) && dimensionParseSuccess;
            if (!dimensionParseSuccess)
            {
                tex = null;
                return false;
            }
            texCols = generateSpecialSymbolByDims(pixelWidth, pixelHeight);
        }
        else
        {
            int k = 0;
            char c = texCode[k];
            while ((c == '0' || c == '1' || c == '*') && c != '/')
            {
                k++;
                c = texCode[k];
            }
            if (c != '/')
            {
                tex = null;
                return false;
            }
            pixelWidth = k;
            pixelHeight = (texCode.Length + 1) / (pixelWidth + 1);
            if ((texCode.Length + 1) % (pixelWidth + 1) != 0)
            {
                tex = null;
                return false;
            }
            texCols = specialCodeToTextureColours(texCode, pixelHeight, pixelWidth);
        }

        if (texCols == null)
        {
            tex = null;
            return false;
        }

        Texture2D specialSymbolTex = new Texture2D(pixelWidth, pixelHeight);
        specialSymbolTex.SetPixels(0, 0, pixelWidth, pixelHeight, texCols);
        specialSymbolTex.filterMode = FilterMode.Point;
        specialSymbolTex.Apply();

        tex = specialSymbolTex;
        return true;
    }

    Color[] specialCodeToTextureColours(string texCode, int pH, int pW)
    {
        char[,] texBinary = new char[pH, pW];
        char c;
        for (int i = 0; i < pH; ++i)
        {
            for (int j = 0; j < pW; ++j)
            {
                c = texCode[(pW + 1) * i + j];
                if (c != '0' && c != '1' && c != '*')
                {
                    return null;
                }
                texBinary[i, j] = c;
            }
        }

        Color[] texCols = new Color[pW * pH];
        int k = 0;
        Color col = Color.cyan;
        for (int i = 0; i < pH; ++i)
        {
            for (int j = 0; j < pW; ++j)
            {
                switch (texBinary[pH - 1 - i, j])
                {
                    case '0':
                        col = Color.black;
                        break;
                    case '1':
                        col = Color.white;
                        break;
                    case '*':
                        col = (RNG.Next(0, 2) == 0) ? Color.black : Color.white;
                        break;
                    default:
                        break;
                }
                texCols[k] = col;
                k++;
            }
        }

        return texCols;
    }

    Color[] generateSpecialSymbolByDims(int pW, int pH)
    {
        int k = 0;
        Color[] texCols = new Color[pW * pH];

        for (int i = 0; i < pH; ++i)
        {
            for (int j = 0; j < pW; ++j)
            {
                texCols[k] = (RNG.Next(0, 2) == 0) ? Color.black : Color.white;
                k++;
            }
        }

        return texCols;
    }
}
