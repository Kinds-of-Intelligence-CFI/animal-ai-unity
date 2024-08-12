using UnityEngine;

/// <summary>
/// This class is used to make the camera follow the player. It is attached to the camera object.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Followed Object")]
    public GameObject followObj;

    void Start()
    {
        if (followObj == null)
        {
            throw new MissingReferenceException("The followObj is not assigned.");
        }

        transform.position = followObj.transform.position;
        transform.rotation = followObj.transform.rotation;
    }

    void Update()
    {
        if (followObj == null)
        {
            throw new MissingReferenceException("The followObj is not assigned.");
        }

        transform.position = followObj.transform.position;
        transform.rotation = followObj.transform.rotation;
    }
}
