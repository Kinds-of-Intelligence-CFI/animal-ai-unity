using System;
using UnityEngine;
using TMPro;
using System.IO;

public class UIManager : MonoBehaviour
{
    public TMP_Text arenaText; // Reference for displaying the current arena
    public TMP_Text configFileNameText; // Reference for displaying the YAML file name
    public AAI3EnvironmentManager environmentManager; // Reference to your environment manager component

    void Awake()
    {
        // Subscribe to the event that notifies when the current arena changes
        AAI3EnvironmentManager.OnArenaChanged += UpdateArenaUI;
    }

    void OnDestroy()
    {
        // Unsubscribe from the event to clean up
        AAI3EnvironmentManager.OnArenaChanged -= UpdateArenaUI;
    }

    private void Start()
    {
        // Initial UI update to display current arena and YAML file name
        UpdateArenaUI(
            environmentManager.GetCurrentArenaIndex(),
            environmentManager.GetTotalArenas()
        );
    }

    private void UpdateArenaUI(int currentArenaIndex, int totalArenas)
    {
        // Set the text to display "Arena X of Y"
        arenaText.text = $"Arena {currentArenaIndex + 1} of {totalArenas}";
        // Display the name of the current YAML configuration file
        configFileNameText.text = $"Config File: {Path.GetFileName(environmentManager.configFile)}";
    }
}
