using UnityEngine;

/// <summary>
/// A zone that triggers an event when an agent enters it. It's primary use is to trigger data collection.
/// </summary>
public class DataZone : Prefab
{
    // TODO: Add logic to differentiate between different datazones
    // TODO: Implement parameter to specify zone names/sections in yaml file (under the datazones Item)
    public delegate void InDataZoneHandler(GameObject zone);
    public static event InDataZoneHandler OnInDataZone;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("agent"))
        {
            OnInDataZone?.Invoke(gameObject);
        }
    }
}
