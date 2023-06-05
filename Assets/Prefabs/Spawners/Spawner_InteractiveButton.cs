using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner_InteractiveButton : MonoBehaviour
{
    public bool triggerActivated = false; // toggle to activate the trigger
    public SpawnerStockpiler spawnerStockpiler; // reference to the spawner script (SpawnerStockpiler.cs)
    public SpawnerStockpiler spawnerDisperser; // reference to the spawner script (SpawnerDisperser.cs)
    public GameObject childObjectToMove; // reference to the child object you want to move
    public Vector3 moveOffset; // the offset values to apply
    public float moveDuration = 1f; // duration of the move animation in seconds
    public float resetDuration = 1f; // duration of the reset animation in seconds
    public GameObject signPosterToInstantiate; // reference to the sign poster prefab
    public bool showSignPoster = false; // toggle to show the sign poster n top of the button prefab 

    void Start()
    {
        //spawnerStockpiler.triggerActivated = false;
        //spawnerDisperser.triggerActivated = false;

        UpdatePrefabVisibility(); // update the prefab visibility
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            triggerActivated = true;
            Debug.Log("Trigger activated. Debug coming from Spawner_InteractiveButton.cs");
            spawnerStockpiler.Activate(); // then enable the spawner script (SpawnerStockpiler.cs)
            spawnerDisperser.triggerActivated = true; // then enable the spawner script (SpawnerDisperser.cs)

            // Start the MoveAndReset coroutine
            StartCoroutine(MoveAndReset());

            if (showSignPoster == true) // if the toggle is true
            {
                InstantiatePrefabOnTop(); // then instantiate the sign poster prefab
            }
        }
        else
        {
            Debug.Log("Trigger NOT activated. Debug coming from Spawner_InteractiveButton.cs");
        }
    }

    private void InstantiatePrefabOnTop()
    {
        Vector3 prefabPosition = transform.position + Vector3.up * (transform.localScale.y / 2 + signPosterToInstantiate.transform.localScale.y / 2); // calculate the position of the prefab to instantiate
        Instantiate(signPosterToInstantiate, prefabPosition, Quaternion.identity, transform); // instantiate the prefab after calculating the position
    }

    private IEnumerator MoveAndReset()
    {
        // Store the original position
        Vector3 originalPosition = childObjectToMove.transform.position;

        // Move the child object
        Vector3 targetPosition = originalPosition + moveOffset;
        float startTime = Time.time;
        while (Time.time < startTime + moveDuration)
        {
            float t = (Time.time - startTime) / moveDuration; // calculate the time
            childObjectToMove.transform.position = Vector3.Lerp(originalPosition, targetPosition, t);
            yield return null; // wait for a frame
        }
        childObjectToMove.transform.position = targetPosition;

        // Wait for a moment (optional, remove this line if not needed)
        // yield return new WaitForSeconds(1f);

        // Reset the position
        startTime = Time.time;
        while (Time.time < startTime + resetDuration)
        {
            float t = (Time.time - startTime) / resetDuration;
            childObjectToMove.transform.position = Vector3.Lerp(targetPosition, originalPosition, t);
            yield return null;
        }
        childObjectToMove.transform.position = originalPosition;
    }

    private void UpdatePrefabVisibility() // update the prefab visibility for sign poster
    {
        if (signPosterToInstantiate != null)
        {
            signPosterToInstantiate.SetActive(showSignPoster);
        }
    }
}
