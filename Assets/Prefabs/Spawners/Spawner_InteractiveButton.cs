using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner_InteractiveButton : MonoBehaviour
{
    /// variables
    public Rigidbody Prefab; // the reward to spawn when the player interacts with the button.
    public Transform spawnPoint; // the spawn point for the reward.

   private void OnTriggerEnter(Collider other) // when the player enters the trigger area.
   {
    Rigidbody rigidPrefab; // create variable of Rigidbody of the reward.
    rigidPrefab = Instantiate(Prefab, spawnPoint.position, spawnPoint.rotation) as Rigidbody; // spawn the reward.
   }

   private void onTriggerExit(Collider other) // when the player leaves the trigger area.
   {
    Destroy(Prefab); // destroy the reward.
   }

}
