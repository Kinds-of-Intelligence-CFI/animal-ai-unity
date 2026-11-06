using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Operations;

/// <summary>
/// Unit tests for TriggerVolume's contract that need no physics: configuration, what the operations are told,
/// and the enter/leave bookkeeping (driven by calling the trigger handlers directly with agent-tagged
/// colliders). Line of sight needs a physics scene and is covered by the PlayMode TriggerVolumeTests.
/// </summary>
public class TriggerVolumeTests
{
    /// <summary>An operation that records what it was told and who it ran for.</summary>
    private class RecordingOperation : Operation
    {
        public readonly List<TrainingAgent> Executed = new List<TrainingAgent>();
        public readonly List<TrainingAgent> Completed = new List<TrainingAgent>();
        public int Initialisations = 0;

        public override void initialise(AttachedObjectDetails details)
        {
            base.initialise(details);
            Initialisations++;
        }

        public override void execute(TrainingAgent triggeringAgent)
        {
            Executed.Add(triggeringAgent);
        }

        public override void completeExecution(TrainingAgent triggeringAgent)
        {
            Completed.Add(triggeringAgent);
        }
    }

    private readonly List<GameObject> _created = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    private GameObject Create(string name)
    {
        GameObject created = new GameObject(name);
        _created.Add(created);
        return created;
    }

    private TriggerVolume Volume(Vector3 position = default)
    {
        GameObject host = Create("Volume");
        host.transform.position = position;
        return host.AddComponent<TriggerVolume>();
    }

    private RecordingOperation Operation()
    {
        return Create("Operation").AddComponent<RecordingOperation>();
    }

    /// <summary>
    /// An agent as the volume sees one: an "agent"-tagged collider with a TrainingAgent. In edit mode the
    /// agent's Unity callbacks don't run, so it needs no arena.
    /// </summary>
    private (TrainingAgent agent, Collider collider) Agent(string name = "Agent")
    {
        GameObject created = Create(name);
        created.tag = "agent";
        Collider collider = created.AddComponent<SphereCollider>();
        TrainingAgent agent = created.AddComponent<TrainingAgent>();
        return (agent, collider);
    }

    private static void Enter(TriggerVolume volume, Collider collider)
    {
        Invoke(volume, "OnTriggerEnter", collider);
    }

    private static void Exit(TriggerVolume volume, Collider collider)
    {
        Invoke(volume, "OnTriggerExit", collider);
    }

    private static void Invoke(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(info, $"{target.GetType().Name} should have a private {method}");
        info.Invoke(target, args);
    }

    private static string PendingMessage(TrainingAgent agent)
    {
        FieldInfo field = typeof(TrainingAgent).GetField(
            "_pendingDataZoneMessage",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        Assert.IsNotNull(field, "TrainingAgent should queue the zone message in _pendingDataZoneMessage");
        return (string)field.GetValue(agent);
    }

    [Test]
    public void Configure_SetsTheIDAndOperations_AndTellsEachOperationAboutTheVolume()
    {
        TriggerVolume volume = Volume(new Vector3(3, 0.5f, 7));
        RecordingOperation first = Operation();
        RecordingOperation second = Operation();

        volume.Configure("Z1", new List<Operation> { first, second });

        Assert.AreEqual("Z1", volume.TriggerZoneID);
        CollectionAssert.AreEqual(new Operation[] { first, second }, volume.Operations);
        foreach (RecordingOperation operation in new[] { first, second })
        {
            Assert.AreEqual(1, operation.Initialisations);
            Assert.AreSame(volume, operation.attachedObjectDetails.obj, "The volume is the operation's host");
            Assert.AreEqual("Z1", operation.attachedObjectDetails.ID);
            Assert.AreEqual(
                new Vector3(3, 0.5f, 7),
                operation.attachedObjectDetails.location,
                "Taken at configuration, so a zone is configured once it is in position"
            );
        }
    }

    [Test]
    public void Configure_WithNulls_UsesAnEmptyIDAndNoOperations()
    {
        TriggerVolume volume = Volume();

        volume.Configure(null, null);

        Assert.AreEqual("", volume.TriggerZoneID);
        Assert.IsNotNull(volume.Operations);
        Assert.IsEmpty(volume.Operations);
    }

    [Test]
    public void Configure_Again_ReplacesTheOperations_AndForgetsAgentsInTheZone()
    {
        TriggerVolume volume = Volume();
        RecordingOperation first = Operation();
        RecordingOperation second = Operation();
        var (agent, collider) = Agent();
        volume.Configure("Z1", new List<Operation> { first });
        Enter(volume, collider);
        Assert.IsTrue(volume.IsAgentInZone);

        volume.Configure("Z2", new List<Operation> { second });

        Assert.AreEqual("Z2", volume.TriggerZoneID);
        CollectionAssert.AreEqual(new Operation[] { second }, volume.Operations);
        Assert.IsFalse(volume.IsAgentInZone);
        Assert.IsEmpty(first.Completed, "Reconfiguring forgets agents without completing the old operations");

        /* The agent is forgotten, so coming in again counts as a fresh entry */
        Enter(volume, collider);
        CollectionAssert.AreEqual(new[] { agent }, second.Executed);
    }

    [Test]
    public void Enter_RunsEveryOperationOnce_ForTheEnteringAgent()
    {
        TriggerVolume volume = Volume();
        RecordingOperation first = Operation();
        RecordingOperation second = Operation();
        volume.Configure("Z", new List<Operation> { first, second });
        var (agent, collider) = Agent();

        Enter(volume, collider);

        CollectionAssert.AreEqual(new[] { agent }, first.Executed);
        CollectionAssert.AreEqual(new[] { agent }, second.Executed);
        Assert.IsEmpty(first.Completed);
        CollectionAssert.AreEquivalent(new[] { agent }, volume.AgentsInZone);
    }

    [Test]
    public void ASecondEnter_ForAnAgentAlreadyInside_DoesNothing()
    {
        TriggerVolume volume = Volume();
        RecordingOperation operation = Operation();
        volume.Configure("Z", new List<Operation> { operation });
        var (agent, collider) = Agent();

        Enter(volume, collider);
        Enter(volume, collider);

        Assert.AreEqual(1, operation.Executed.Count);
        Assert.AreEqual(1, volume.AgentsInZone.Count);
    }

    [Test]
    public void Exit_CompletesEveryOperation_AndForgetsTheAgent()
    {
        TriggerVolume volume = Volume();
        RecordingOperation operation = Operation();
        volume.Configure("Z", new List<Operation> { operation });
        var (agent, collider) = Agent();
        Enter(volume, collider);

        Exit(volume, collider);

        CollectionAssert.AreEqual(new[] { agent }, operation.Completed);
        Assert.IsFalse(volume.IsAgentInZone);

        /* Leaving twice, or leaving without having entered, is ignored */
        Exit(volume, collider);
        Assert.AreEqual(1, operation.Completed.Count);
    }

    [Test]
    public void SeveralAgents_AreTrackedIndependently()
    {
        TriggerVolume volume = Volume();
        RecordingOperation operation = Operation();
        volume.Configure("Z", new List<Operation> { operation });
        var (first, firstCollider) = Agent("First");
        var (second, secondCollider) = Agent("Second");

        Enter(volume, firstCollider);
        Enter(volume, secondCollider);
        CollectionAssert.AreEquivalent(new[] { first, second }, volume.AgentsInZone);

        Exit(volume, firstCollider);
        CollectionAssert.AreEquivalent(new[] { second }, volume.AgentsInZone);
        CollectionAssert.AreEqual(new[] { first }, operation.Completed);
        CollectionAssert.AreEqual(new[] { first, second }, operation.Executed);
    }

    [Test]
    public void ColliderWithoutTheAgentTag_IsIgnored()
    {
        TriggerVolume volume = Volume();
        RecordingOperation operation = Operation();
        volume.Configure("Z", new List<Operation> { operation });
        GameObject wall = Create("Wall");
        Collider wallCollider = wall.AddComponent<BoxCollider>();

        Enter(volume, wallCollider);

        Assert.IsEmpty(operation.Executed);
        Assert.IsFalse(volume.IsAgentInZone);
    }

    [Test]
    public void TheOwner_IsIgnored_OtherAgentsAreNot()
    {
        TriggerVolume volume = Volume();
        RecordingOperation operation = Operation();
        volume.Configure("Z", new List<Operation> { operation });
        var (owner, ownerCollider) = Agent("Owner");
        var (other, otherCollider) = Agent("Other");
        volume.Owner = owner;

        Enter(volume, ownerCollider);
        Enter(volume, otherCollider);

        CollectionAssert.AreEqual(new[] { other }, operation.Executed);
        CollectionAssert.AreEquivalent(new[] { other }, volume.AgentsInZone);
    }

    [Test]
    public void Clear_ForgetsEveryone_WithoutCompletingTheOperations()
    {
        TriggerVolume volume = Volume();
        RecordingOperation operation = Operation();
        volume.Configure("Z", new List<Operation> { operation });
        var (agent, collider) = Agent();
        Enter(volume, collider);

        volume.Clear();

        Assert.IsFalse(volume.IsAgentInZone);
        Assert.IsEmpty(operation.Completed);

        /* A later exit for the forgotten agent is ignored, and a later entry is a fresh one */
        Exit(volume, collider);
        Assert.IsEmpty(operation.Completed);
        Enter(volume, collider);
        Assert.AreEqual(2, operation.Executed.Count);
    }

    [Test]
    public void EnteringAndLeaving_RecordTheDataZoneMessages_OnTheAgent()
    {
        TriggerVolume volume = Volume();
        volume.Configure("D1", null);
        var (agent, collider) = Agent();

        Enter(volume, collider);
        Assert.AreEqual("Agent was in DataZone: D1", PendingMessage(agent), "The exact text the CSV logs");
        Assert.AreEqual(TriggerVolume.EnterMessagePrefix + "D1", PendingMessage(agent));

        /* A second message in the same step appends just the zone's ID (see TrainingAgent.RecordDataZoneMessage) */
        Exit(volume, collider);
        Assert.AreEqual("Agent was in DataZone: D1 | D1", PendingMessage(agent));
    }

    [Test]
    public void MessagePrefixes_AreTheOnesTheCSVTemplatesExpect()
    {
        Assert.AreEqual("Agent was in DataZone: ", TriggerVolume.EnterMessagePrefix);
        Assert.AreEqual("Agent left DataZone: ", TriggerVolume.LeaveMessagePrefix);
    }
}
