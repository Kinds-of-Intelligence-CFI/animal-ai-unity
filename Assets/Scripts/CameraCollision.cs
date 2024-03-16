using UnityEngine;

/// <summary>
/// Manages camera collision to prevent the camera from going through walls.
/// </summary>
public class CameraCollision : MonoBehaviour
{
	public float minDistance = 1.0f;
	public float maxDistance = 4.0f;
	public float smooth = 10.0f;
	private Vector3 dollyDir;
	private float distance;

	// Define a layer mask for collision checks if needed
	public LayerMask collisionLayerMask;

	// Use a constant for the adjustment factor to avoid magic numbers
	private const float DistanceAdjustmentFactor = 0.9f;

	void Awake()
	{
		dollyDir = transform.localPosition.normalized;
		distance = transform.localPosition.magnitude;
	}

	void Update()
	{
		Vector3 desiredCameraPos = transform.parent.TransformPoint(dollyDir * maxDistance);
		RaycastHit hit;

		if (Physics.Linecast(transform.parent.position, desiredCameraPos, out hit, collisionLayerMask))
		{
			distance = Mathf.Clamp(hit.distance * DistanceAdjustmentFactor, minDistance, maxDistance);
		}
		else
		{
			distance = maxDistance;
		}

		transform.localPosition = Vector3.Lerp(
			transform.localPosition,
			dollyDir * distance,
			Time.deltaTime * smooth
		);
	}
}
