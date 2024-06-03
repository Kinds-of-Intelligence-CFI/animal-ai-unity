using Unity.MLAgents.SideChannels;
using System;
using ArenasParameters;
using UnityEngine;

/// <summary>
/// This class is used to communicate the environment configurations to the Unity.
/// </summary>
public class ArenasParametersSideChannel : SideChannel
{
	public string CurrentYamlFileName { get; private set; } // Store the current YAML file name

	/// <summary>
	/// Initializes a new instance of the ArenasParametersSideChannel class.
	/// </summary>
	public ArenasParametersSideChannel()
	{
		ChannelId = new Guid("9c36c837-cad5-498a-b675-bc19c9370072");
	}

	/// <summary>
	/// This method is called when a message is received from the Unity.
	/// </summary>
	protected override void OnMessageReceived(IncomingMessage msg)
	{
		string fileName = msg.ReadString();
		byte[] yamlData = msg.GetRawBytes();

		Debug.Log($"Received YAML file name: {fileName}");
		CurrentYamlFileName = fileName;

		// Create the event args including the file name and the YAML data
		ArenasParametersEventArgs args = new ArenasParametersEventArgs
		{
			arenas_yaml = yamlData,
			yamlFileName = fileName
		};
		OnArenasParametersReceived(args);
	}

	/// <summary>
	/// This method is called when the arenas parameters are received.
	/// </summary>
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
