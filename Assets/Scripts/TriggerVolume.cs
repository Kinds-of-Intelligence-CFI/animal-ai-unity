using System.Collections.Generic;
using UnityEngine;
using Operations;

/// <summary>
/// A trigger collider that runs operations on the agents inside it: every operation's execute when an
/// agent comes in, and its completeExecution when that agent leaves. It is the part shared by a
/// <see cref="DataZone"/> (a zone placed in the arena) and an <see cref="AgentZone"/> (a zone carried by an
/// agent), and it is the host the operations see in their AttachedObjectDetails.
///
/// A zone can be given an <see cref="Owner"/>, an agent that never triggers it, and can require a line of
/// sight: an agent then only counts as inside while nothing solid lies between the zone's centre and the
/// agent, re-checked every physics step for every agent overlapping the collider.
/// </summary>
public class TriggerVolume : MonoBehaviour
{
    /* The CSV messages an agent records on entering and leaving; the same for both kinds of zone */
    public const string EnterMessagePrefix = "Agent was in DataZone: ";
    public const string LeaveMessagePrefix = "Agent left DataZone: ";

    public string TriggerZoneID { get; private set; } = "";
    public List<Operation> Operations { get; private set; } = new List<Operation>();

    /// <summary>The agent carrying this zone, which never triggers it. Null for a zone placed in the arena.</summary>
    public TrainingAgent Owner { get; set; }

    /// <summary>
    /// When set, an agent inside the collider only counts as in the zone while the straight line from the
    /// zone's centre to the agent's centre is clear of solid (non-trigger) colliders.
    /// </summary>
    public bool RequiresLineOfSight { get; set; }

    /* Agents overlapping the collider, whether or not they are in sight */
    private readonly HashSet<TrainingAgent> _overlapping = new HashSet<TrainingAgent>();
    /* Agents the operations have been executed for and not yet completed */
    private readonly HashSet<TrainingAgent> _agentsInZone = new HashSet<TrainingAgent>();
    private readonly List<TrainingAgent> _toEvaluate = new List<TrainingAgent>();

    public bool IsAgentInZone
    {
        get { return _agentsInZone.Count > 0; }
    }

    public IReadOnlyCollection<TrainingAgent> AgentsInZone
    {
        get { return _agentsInZone; }
    }

    /// <summary>
    /// Gives the zone its ID and operations, forgets any agents in it, and hands every operation this zone as
    /// its attached object (so call it once the zone is in position: the operations record its location). A
    /// DataZone is configured once, when it is spawned; an agent's zone is configured again for every arena.
    /// </summary>
    public void Configure(string triggerZoneID, List<Operation> operations)
    {
        TriggerZoneID = triggerZoneID ?? "";
        Operations = operations ?? new List<Operation>();
        Clear();
        foreach (Operation operation in Operations)
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

    /// <summary>
    /// Forgets every agent in the zone without running completeExecution. For an arena reset: the agents
    /// have been moved (OnTriggerExit doesn't fire for a teleport) and anything still running for them, such
    /// as a continuous reward, has already stopped with their episode.
    /// </summary>
    public void Clear()
    {
        _overlapping.Clear();
        _agentsInZone.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        TrainingAgent agent = AgentFrom(other);
        if (agent == null || !_overlapping.Add(agent))
        {
            return;
        }
        Evaluate(agent);
    }

    private void OnTriggerExit(Collider other)
    {
        TrainingAgent agent = AgentFrom(other);
        if (agent == null || !_overlapping.Remove(agent))
        {
            return;
        }
        SetInZone(agent, false);
    }

    private void FixedUpdate()
    {
        if (!RequiresLineOfSight || _overlapping.Count == 0)
        {
            return;
        }
        /* Agents can step behind, or out from behind, something while staying inside the collider. The sweep
           walks a copy so it can't throw if an operation clears the set (an endEpisode resets the arena, which
           reconfigures this zone) */
        _toEvaluate.Clear();
        _toEvaluate.AddRange(_overlapping);
        foreach (TrainingAgent agent in _toEvaluate)
        {
            Evaluate(agent);
        }
    }

    private void Evaluate(TrainingAgent agent)
    {
        SetInZone(agent, !RequiresLineOfSight || HasLineOfSightTo(agent));
    }

    private void SetInZone(TrainingAgent agent, bool inZone)
    {
        if (inZone)
        {
            if (!_agentsInZone.Add(agent))
            {
                return;
            }
            agent.RecordDataZoneMessage(EnterMessagePrefix + TriggerZoneID, TriggerZoneID);
            for (int i = 0; i < Operations.Count; i++)
            {
                Operations[i].execute(agent);
            }
        }
        else
        {
            if (!_agentsInZone.Remove(agent))
            {
                return;
            }
            agent.RecordDataZoneMessage(LeaveMessagePrefix + TriggerZoneID, TriggerZoneID);
            for (int i = 0; i < Operations.Count; i++)
            {
                Operations[i].completeExecution(agent);
            }
        }
    }

    /// <summary>
    /// Whether the straight line from this zone's centre to the agent's centre reaches the agent's body
    /// before meeting anything solid. Triggers (DataZones, HotZones, DeathZones) are looked through, and so is anything
    /// on the Ignore Raycast layer; walls, including transparent ones, blocks, ramps, goals and other agents are
    /// not. A ray never hits the collider it starts inside, so the owner's own body doesn't get in the way.
    /// </summary>
    public bool HasLineOfSightTo(TrainingAgent agent)
    {
        if (
            !Physics.Linecast(
                transform.position,
                agent.transform.position,
                out RaycastHit hit,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            return true;
        }
        return hit.collider.gameObject == agent.gameObject;
    }

    private TrainingAgent AgentFrom(Collider other)
    {
        if (!other.CompareTag("agent"))
        {
            return null;
        }
        TrainingAgent agent = other.GetComponent<TrainingAgent>();
        /* Colliders on one rigidbody don't trigger each other, so the owner never arrives here; this keeps
           it that way whatever the agent's colliders become */
        return agent == Owner ? null : agent;
    }
}
