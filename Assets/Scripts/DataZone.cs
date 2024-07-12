using UnityEngine;

/// <summary>
/// A zone that triggers an event when an agent enters it. It's primary use is to trigger a log event when an agent enters a trigger/data zone.
/// </summary>
public class DataZone : Prefab
{
    public delegate void InDataZoneHandler(string TriggerZoneID);
    public static event InDataZoneHandler OnInDataZone;

    public string TriggerZoneID { get; set; }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("agent"))
        {
            Debug.Log("Agent entered data zone: " + TriggerZoneID);
            OnInDataZone?.Invoke(TriggerZoneID);
        }
    }
}
