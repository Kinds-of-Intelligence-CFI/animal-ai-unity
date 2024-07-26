using UnityEngine;

/// <summary>
/// DeathZone represents a rectangular game object with a collider.
/// </summary>
public class DeathZone : Goal
{
    public override void SetSize(Vector3 size)
    {
        /* Clip size to within specified limits */
        Vector3 clippedSize = Vector3.Max(sizeMin, Vector3.Min(sizeMax, size)) * sizeAdjustment;

        // Adjust individual size components if any are negative
        float sizeX = size.x < 0 ? Random.Range(sizeMin.x, sizeMax.x) : clippedSize.x;
        float sizeY = size.y < 0 ? Random.Range(sizeMin.y, sizeMax.y) : clippedSize.y;
        float sizeZ = size.z < 0 ? Random.Range(sizeMin.z, sizeMax.z) : clippedSize.z;

        /* Set height and scale of the deathzone */
        _height = sizeY;
        transform.localScale = new Vector3(
            sizeX * ratioSize.x,
            sizeY * ratioSize.y,
            sizeZ * ratioSize.z
        );

        /* Set object scale for shader effects */
        GetComponent<Renderer>().material.SetVector("_ObjScale", new Vector3(sizeX, sizeY, sizeZ));
    }

    protected override float AdjustY(float yIn)
    {
        return -0.15f;
    }
}
