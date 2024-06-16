

/// <summary>
/// Represents the ground in the environment. This class is used to adjust the y position of the ground.
/// </summary>
public class Grounded : Prefab
{
	protected override float AdjustY(float yIn)
	{
		return 0;
	}
}
