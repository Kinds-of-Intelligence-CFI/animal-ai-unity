using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner_InteractiveButton : MonoBehaviour
{
   public SpawnerStockpiler spawnerStockpiler; // reference to the spawner script (SpawnerStockpiler.cs)
   public SpawnerStockpiler spawnerDisperser; // reference to the spawner script (SpawnerDisperser.cs)

    private void OnTriggerEnter(Collider other) // when the agent enters the trigger zone (button)
    {
        if (other.CompareTag("agent")) // if the agent enters the trigger zone (button)
        {
            Debug.Log("trigger activated"); // print "Button pressed" in the console
            spawnerStockpiler.triggerActivated = true; // then enable the spawner script (SpawnerStockpiler.cs)
            spawnerDisperser.triggerActivated = true; // then enable the spawner script (SpawnerDisperser.cs)
        }
        else
        {
            Debug.Log("trigger not activated"); // print "Button not pressed" in the console
        }
    }
}
