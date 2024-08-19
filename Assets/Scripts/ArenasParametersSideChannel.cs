using Unity.MLAgents.SideChannels;
using System;
using ArenasParameters;

/// <summary>
/// This class is used to communicate the environment configurations to the Unity.
/// </summary>
public class ArenasParametersSideChannel : SideChannel
{
    /// <summary>
    /// Initializes a new instance of the ArenasParametersSideChannel class.
    /// </summary>
    public ArenasParametersSideChannel()
    {
        ChannelId = new Guid("9c36c837-cad5-498a-b675-bc19c9370072");
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        byte[] yamlData = msg.GetRawBytes();

        /* Create the event args and the YAML data */
        ArenasParametersEventArgs args = new ArenasParametersEventArgs { arenas_yaml = yamlData, };
        OnArenasParametersReceived(args);
    }

    protected virtual void OnArenasParametersReceived(
        ArenasParametersEventArgs arenasParametersEvent
    )
    {
        EventHandler<ArenasParametersEventArgs> handler = NewArenasParametersReceived;
        if (handler != null)
        {
            handler(this, arenasParametersEvent);
        }
    }

    /// <summary>
    /// This event is triggered when new arenas parameters are received.
    /// </summary>
    public EventHandler<ArenasParametersEventArgs> NewArenasParametersReceived;
}
