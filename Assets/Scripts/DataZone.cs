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
    public List<Operation> Operations { get; set; } = new List<Operation>();

    private readonly HashSet<TrainingAgent> _agentsInZone = new HashSet<TrainingAgent>();

    public bool isAgentInZone
    {
        get { return _agentsInZone.Count > 0; }
    }

    private void Start()
    {
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        foreach (var operation in Operations)
        {
            operation.initialise(
                new AttachedObjectDetails
                {
                    obj = this,
                    ID = TriggerZoneID,
                    location = transform.position
                }
            );
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
        TrainingAgent agent = AgentFrom(other);
        if (agent == null || !_agentsInZone.Add(agent))
        {
            return;
        }
        agent.RecordDataZoneMessage("Agent was in DataZone: " + TriggerZoneID, TriggerZoneID);
        for (int i = 0; i < Operations.Count; i++)
        {
            Operations[i].execute(agent);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TrainingAgent agent = AgentFrom(other);
        if (agent == null || !_agentsInZone.Remove(agent))
        {
            return;
        }
        agent.RecordDataZoneMessage("Agent left DataZone: " + TriggerZoneID, TriggerZoneID);
        for (int i = 0; i < Operations.Count; i++)
        {
            Operations[i].completeExecution(agent);
        }
    }

    private static TrainingAgent AgentFrom(Collider other)
    {
        return other.CompareTag("agent") ? other.GetComponent<TrainingAgent>() : null;
    }
}
