using UnityEngine;

/// <summary>
/// This class is used to make the camera follow the player.
/// </summary>
public class CameraFollow : MonoBehaviour
{
	[Header("Followed Object")]
	public GameObject followObj;

	void Start()
	{
		transform.position = followObj.transform.position;
		transform.rotation = followObj.transform.rotation;
	}

	void Update()
	{
		transform.position = followObj.transform.position;
		transform.rotation = followObj.transform.rotation;
	}
}
