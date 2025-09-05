using UnityEngine;
using System.Collections.Generic;
using Operations;

/// <summary>
/// A zone that triggers an event when an agent enters it.
/// It's primary use is to trigger a log event when an agent enters a datazone gameobject.
/// </summary>
public class DataZone : Prefab
{
    public string TriggerZoneID { get; set; }
    public bool isAgentInZone { get; set; } = false;
    public List<Operation> Operations { get; set; } = new List<Operation>();

    // Note: There is a race condition on this variable
    // (If multiple DataZones try to write to this simultaneously one will come first in the message arbitrarily)
    // TODO: Currently concurrent entries/exits are gracefully handled, but simultaneously entering one and exiting another is not
    private static string LastDataZoneMessage = null;

    private void Start()
    {
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
            if (LastDataZoneMessage == null) {
                LastDataZoneMessage = "Agent was in DataZone: " + TriggerZoneID;
            } else {
                LastDataZoneMessage += " | " + TriggerZoneID;
            }
            for (int i = 0; i < Operations.Count; i++)
            {
                Operations[i].execute();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            isAgentInZone = false;
            if (LastDataZoneMessage == null) {
                LastDataZoneMessage = "Agent left DataZone: " + TriggerZoneID;
            } else {
                LastDataZoneMessage += " | " + TriggerZoneID;
            }
        }
    }

    public static string ConsumeDataZoneMessage()
    {
        string message = LastDataZoneMessage;
        LastDataZoneMessage = null;
        return message;
    }
}
