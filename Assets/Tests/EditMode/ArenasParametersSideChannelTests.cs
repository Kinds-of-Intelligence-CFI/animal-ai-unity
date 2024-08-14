using NUnit.Framework;
using Unity.MLAgents.SideChannels;
using ArenasParameters;
using System;
using UnityEngine;

/// <summary>
/// Tests for the ArenasParametersSideChannel class.
/// </summary>
public class ArenasParametersSideChannelTests
{
    private TestableArenasParametersSideChannel sideChannel;
    private bool eventTriggered;
    private ArenasParametersEventArgs receivedArgs;

    [SetUp]
    public void Setup()
    {
        sideChannel = new TestableArenasParametersSideChannel();

        sideChannel.NewArenasParametersReceived += (sender, args) =>
        {
            eventTriggered = true;
            receivedArgs = args;
        };
    }

    [Test]
    public void ArenasParametersSideChannel_OnMessageReceived_TriggersEvent()
    {
        byte[] mockYamlData = new byte[] { 1, 2, 3, 4 }; /* Basic byte array */
        var incomingMessage = new IncomingMessage(mockYamlData);

        sideChannel.TestOnMessageReceived(incomingMessage);

        Assert.IsTrue(eventTriggered, "Event was not triggered");

        Assert.AreEqual(
            mockYamlData,
            receivedArgs.arenas_yaml,
            "The received YAML data does not match the expected data"
        );
    }

    /* Subclass to expose the protected OnMessageReceived method */
    private class TestableArenasParametersSideChannel : ArenasParametersSideChannel
    {
        public void TestOnMessageReceived(IncomingMessage msg)
        {
            OnMessageReceived(msg);
        }
    }
}
