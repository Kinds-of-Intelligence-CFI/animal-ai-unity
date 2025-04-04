using UnityEngine;

/// <summary>
/// A zone that triggers an event when an agent enters it.
/// It's primary use is to trigger a log event when an agent enters a datazone gameobject.
/// </summary>
public class DataZone : Prefab
{
    public string TriggerZoneID { get; set; }
    public bool ZoneVisibility { get; set; } = true;
    public bool isAgentInZone { get; set; } = false;

    // TODO: Fix the race condition on LastDataZoneMessage
    // (If multiple DataZones try to write to this simultaneously one will get overwritten)
    private static string? LastDataZoneMessage = null;

    private void Start()
    {
        SetVisibility(ZoneVisibility);
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    public void SetVisibility(bool visibility)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = visibility;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agent") && !isAgentInZone)
        {
            isAgentInZone = true;
            LastDataZoneMessage = "Agent was in DataZone: " + TriggerZoneID;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            isAgentInZone = false;
            LastDataZoneMessage = "Agent left DataZone: " + TriggerZoneID;
        }
    }

    public static string ConsumeDataZoneMessage()
    {
        string? message = LastDataZoneMessage;
        LastDataZoneMessage = null;
        return message;
    }
}
