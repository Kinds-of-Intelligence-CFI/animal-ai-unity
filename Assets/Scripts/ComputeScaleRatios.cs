using UnityEngine;
using UnityEngineExtensions;

/// <summary>
/// Computes the scale ratios of the object and its children.
/// </summary>
public class ComputeScaleRatios : MonoBehaviour
{
    void Start()
    {
        /* Compute the real size of the object including its children */
        Vector3 sizeReal = gameObject.GetBoundsWithChildren().extents * 2;
        Vector3 scaleReal = transform.localScale;

        /* Initialize ratioSize with scaleReal to handle potential division by zero */
        Vector3 ratioSize = scaleReal;

        /* Ensure we're not dividing by zero */
        ratioSize.x = sizeReal.x != 0 ? scaleReal.x / sizeReal.x : 0;
        ratioSize.y = sizeReal.y != 0 ? scaleReal.y / sizeReal.y : 0;
        ratioSize.z = sizeReal.z != 0 ? scaleReal.z / sizeReal.z : 0;
    }
}
