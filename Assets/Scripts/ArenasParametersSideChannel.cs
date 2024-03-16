using Unity.MLAgents.SideChannels;
using System;
using ArenasParameters;

/// <summary>
/// This class is used to communicate the environment configurations to the Unity.
/// </summary>
public class ArenasParametersSideChannel : SideChannel
{
	public ArenasParametersSideChannel()
	{
		ChannelId = new Guid("9c36c837-cad5-498a-b675-bc19c9370072");
	}

	protected override void OnMessageReceived(IncomingMessage msg)
	{
		ArenasParametersEventArgs args = new ArenasParametersEventArgs();
		args.arenas_yaml = msg.GetRawBytes();
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

	public EventHandler<ArenasParametersEventArgs> NewArenasParametersReceived;

}
