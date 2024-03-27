using UnityEngine;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// BallGoal represents a spherical game object with a collider.
/// </summary>
public class BallGoal : Goal
{
    // Adjusts the size of the goal within specified limits
    public override void SetSize(Vector3 size)
    {
        // Clip size to within specified limits
        Vector3 clippedSize = Vector3.Max(sizeMin, Vector3.Min(sizeMax, size)) * sizeAdjustment;

        // If any size component is negative, choose a random size
        if (size.x < 0 || size.y < 0 || size.z < 0)
        {
            float randomSize = Random.Range(sizeMin.x, sizeMax.x);
            clippedSize = Vector3.one * randomSize;
        }

        // Set height and scale of the goal
        float scaledSize = clippedSize.x * ratioSize.x;
        _height = scaledSize;
        transform.localScale = new Vector3(scaledSize, scaledSize, scaledSize);

        // Update reward based on size
        reward = Math.Sign(reward) * scaledSize;
    }
}
