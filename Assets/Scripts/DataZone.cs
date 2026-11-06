using System;
using UnityEngine;
using System.Collections.Generic;
using Operations;

/// <summary>
/// A zone placed in the arena that triggers an event when an agent enters it.
/// Its primary use is to trigger a log event when an agent enters a datazone gameobject; it can also run
/// operations on the agents that enter it. The entering, leaving and operations are handled by the
/// <see cref="TriggerVolume"/> on the same object (the prefab carries one), which an agent's own zone
/// (<see cref="AgentZone"/>) shares; this class adds the prefab behaviour (sizing, visibility) and is
/// configured exactly once, when the arena builder spawns it.
/// </summary>
[RequireComponent(typeof(TriggerVolume))]
public class DataZone : Prefab
{
    private TriggerVolume _volume;
    private bool _configured = false;

    /// <summary>The trigger logic of this zone.</summary>
    public TriggerVolume Volume
    {
        get
        {
            if (_volume == null)
            {
                _volume = GetComponent<TriggerVolume>();
            }
            return _volume;
        }
    }

    public string TriggerZoneID
    {
        get { return Volume.TriggerZoneID; }
    }

    public List<Operation> Operations
    {
        get { return Volume.Operations; }
    }

    public bool isAgentInZone
    {
        get { return Volume.IsAgentInZone; }
    }

    /// <summary>
    /// Gives the zone its ID and operations. A DataZone is spawned afresh for every arena, so this is called
    /// once per instance; a second call is a bug.
    /// </summary>
    public void Configure(string triggerZoneID, List<Operation> operations)
    {
        if (_configured)
        {
            throw new InvalidOperationException(
                $"DataZone '{TriggerZoneID}' is already configured; a DataZone is configured once, when it is spawned"
            );
        }
        _configured = true;
        Volume.Configure(triggerZoneID, operations);
    }

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
}
