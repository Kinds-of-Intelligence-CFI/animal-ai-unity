using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner_InteractiveButton : MonoBehaviour
{
   public SpawnerStockpiler spawnerStockpiler; // reference to the spawner script (SpawnerStockpiler.cs)

    private void OnTriggerEnter(Collider other) // when the agent enters the trigger zone (button)
    {
        if (other.CompareTag("agent")) // if the agent enters the trigger zone (button)
        {
            spawnerStockpiler.triggerActivated = true; // then enable the spawner script (SpawnerStockpiler.cs)
        
        }
    }
}
