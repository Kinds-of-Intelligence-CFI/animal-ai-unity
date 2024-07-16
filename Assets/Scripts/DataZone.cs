using UnityEngine;

/// <summary>
/// A zone that triggers an event when an agent enters it. It's primary use is to trigger a log event when an agent enters a trigger/data zone.
/// </summary>
public class DataZone : Prefab
{
    public delegate void InDataZoneHandler(string TriggerZoneID);
    public static event InDataZoneHandler OnInDataZone;
    public string TriggerZoneID { get; set; }
    public bool ZoneVisibility { get; set; } = true;

    public void SetVisibility(bool visibility)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = visibility;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("agent"))
        {
            OnInDataZone?.Invoke(TriggerZoneID);
        }
    }
}
