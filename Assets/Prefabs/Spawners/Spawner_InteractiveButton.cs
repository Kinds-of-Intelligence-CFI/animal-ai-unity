using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner_InteractiveButton : MonoBehaviour
{
    /// variables
    public Rigidbody Prefab; // the reward to spawn when the player interacts with the button.
    public Rigidbody spawnPoint; // the spawn point for the reward.

   void OnTriggerEnter(Collider other) // when the player enters the trigger area.
   {
    Rigidbody rigidPrefab;
    rigidPrebab = Instantiate(Prefab, transform.position, transform.rotation) as Rigidbody; // spawn the reward.
   }

}
