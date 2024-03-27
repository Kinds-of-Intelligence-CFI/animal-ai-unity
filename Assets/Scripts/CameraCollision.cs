using UnityEngine;

/// <summary>
/// This script is used to prevent the camera from colliding with objects in the scene.
/// </summary>
public class CameraCollision : MonoBehaviour
{
	[Header("Camera Collision Settings")]
	public float minDistance = 1.0f;
	public float maxDistance = 4.0f;
	public float smooth = 10.0f;
	Vector3 dollyDir;
	public Vector3 dollyDistAdjusted;
	public float distance;

	void Awake()
	{
		dollyDir = transform.localPosition.normalized;
		distance = transform.localPosition.magnitude;
	}

	void Update()
	{
		Vector3 desiredCameraPos = transform.parent.TransformPoint(dollyDir * maxDistance);
		RaycastHit hit;

		if (Physics.Linecast(transform.parent.position, desiredCameraPos, out hit))
		{
			distance = Mathf.Clamp(hit.distance * 0.9f, minDistance, maxDistance);
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