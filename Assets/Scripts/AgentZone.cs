using System;
using UnityEngine;

/// <summary>
/// The spherical zone an agent can carry (the Agent item's <c>zone</c> block): a trigger sphere centred on
/// the agent that moves with it and runs its operations on any other agent inside it, through the
/// <see cref="TriggerVolume"/> it shares with DataZone. It requires a line of sight, so walls and other
/// agents block it, and it never acts on the agent carrying it.
///
/// It is an instance of the AgentZone prefab (Resources/AgentZone: a unit sphere with a trigger collider,
/// the translucent DataZone material, a disabled renderer, a TriggerVolume and this script) parented to the
/// agent, so it follows the agent wherever it goes. The prefab is on the Ignore Raycast layer, so ray
/// sensors and the builder's spawn checks look straight through it; cameras see it only when the zone is
/// made visible. The arena builder configures it every time the agent is placed, since each arena can give
/// the agent a different zone or none.
/// </summary>
public class AgentZone : MonoBehaviour
{
    /// <summary>The smallest radius: the agents' body radius, at which the zone fires when the agents touch.</summary>
    public const float MinimumRadius = 0.5f;

    /// <summary>Resources path of the prefab every agent's zone is instantiated from.</summary>
    public const string PrefabResourcePath = "AgentZone";

    private TriggerVolume _volume;
    private SphereCollider _collider;
    private MeshRenderer _renderer;

    public TriggerVolume Volume
    {
        get { return _volume; }
    }

    /// <summary>The radius in arena units, as configured (at least <see cref="MinimumRadius"/>).</summary>
    public float Radius { get; private set; }

    public bool IsVisible
    {
        get { return _renderer != null && _renderer.enabled; }
    }

    private void Awake()
    {
        _volume = GetComponent<TriggerVolume>();
        _collider = GetComponent<SphereCollider>();
        _renderer = GetComponent<MeshRenderer>();
    }

    /// <summary>The agent's zone, active or not, or null if it has never had one.</summary>
    public static AgentZone Of(TrainingAgent agent)
    {
        return agent.GetComponentInChildren<AgentZone>(true);
    }

    /// <summary>
    /// Gives <paramref name="agent"/> the zone described by <paramref name="settings"/>, creating the zone
    /// the first time and reconfiguring it after that. With null settings the agent's zone, if any, is
    /// switched off. Agents already inside are forgotten (see <see cref="TriggerVolume.Clear"/>), and the
    /// operations are (re)initialised with this zone as their attached object.
    /// </summary>
    public static AgentZone Configure(TrainingAgent agent, YAMLDefs.Zone settings)
    {
        AgentZone zone = Of(agent);
        if (settings == null)
        {
            if (zone != null)
            {
                zone.Volume.Clear();
                zone.gameObject.SetActive(false);
            }
            return null;
        }
        if (zone == null)
        {
            zone = Create(agent);
        }
        zone.Apply(agent, settings);
        return zone;
    }

    /// <summary>
    /// Takes the zone out of the physics scene while the agent is being placed, so the agent's spawn
    /// footprint is just its body. <see cref="Configure"/> brings it back.
    /// </summary>
    public static void Deactivate(TrainingAgent agent)
    {
        AgentZone zone = Of(agent);
        if (zone != null)
        {
            zone.gameObject.SetActive(false);
        }
    }

    private static AgentZone Create(TrainingAgent agent)
    {
        GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Agent zone prefab not found at Resources/{PrefabResourcePath}");
        }
        GameObject child = Instantiate(prefab, agent.transform);
        child.name = prefab.name;

        AgentZone zone = child.GetComponent<AgentZone>();
        if (zone == null || zone._volume == null || zone._collider == null || zone._renderer == null)
        {
            throw new InvalidOperationException(
                $"Agent zone prefab '{PrefabResourcePath}' needs an AgentZone, a TriggerVolume, a SphereCollider and a MeshRenderer"
            );
        }
        zone._volume.Owner = agent;
        zone._volume.RequiresLineOfSight = true;
        return zone;
    }

    private void Apply(TrainingAgent agent, YAMLDefs.Zone settings)
    {
        Radius = Mathf.Max(settings.radius, MinimumRadius);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        /* The prefab is a unit sphere, so its scale is the zone's diameter */
        transform.localScale = Vector3.one * (2f * Radius);

        _volume.Owner = agent;
        _volume.RequiresLineOfSight = true;
        _renderer.enabled = settings.zoneVisibility;
        _collider.enabled = true;

        gameObject.SetActive(true);
        /* Last, once the zone is in place and active: the operations record its location */
        _volume.Configure(
            string.IsNullOrEmpty(settings.triggerZoneID) ? $"agent{agent.agentIndex}" : settings.triggerZoneID,
            settings.operations
        );
    }
}
