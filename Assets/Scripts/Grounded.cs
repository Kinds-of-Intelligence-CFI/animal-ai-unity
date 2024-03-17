using UnityEngine;

/// <summary>
/// Represents the ground in the environment.
/// </summary>
public class Grounded : Prefab
{
	protected override float AdjustY(float yIn)
	{
		return 0;
	}
}
