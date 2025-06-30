using Unity.MLAgents.SideChannels;
using System;
using System.IO;
using System.Text;
using UnityEngine;
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
        byte[] rawData = msg.GetRawBytes();
        string potentialFilePath = Encoding.UTF8.GetString(rawData);

        // Check if the received string is a valid file path
        if (!string.IsNullOrEmpty(potentialFilePath) && File.Exists(potentialFilePath))
        {
            // read arena config from file
            byte[] yamlData = ReadArenaConfigFile(potentialFilePath);
            if (yamlData != null)
            {
                ArenasParametersEventArgs args = new ArenasParametersEventArgs { arenas_yaml = yamlData, };
                OnArenasParametersReceived(args);
            }
        }
        else
        {
            // treat raw data as arena content directly
            ArenasParametersEventArgs args = new ArenasParametersEventArgs { arenas_yaml = rawData, };
            OnArenasParametersReceived(args);
        }
    }

    private byte[] ReadArenaConfigFile(string filePath)
    {
        try
        {

            if (!File.Exists(filePath))
            {
                Debug.LogError($"Arena config file not found: {filePath}");
                return null;
            }

            byte[] fileContent = File.ReadAllBytes(filePath);
            if (fileContent == null || fileContent.Length == 0)
            {
                Debug.LogError($"Arena config file is empty: {filePath}");
                return null;
            }

            Debug.Log($"Successfully loaded arena config from: {filePath}");
            return fileContent;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading arena config file '{filePath}': {ex.Message}");
            return null;
        }
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
