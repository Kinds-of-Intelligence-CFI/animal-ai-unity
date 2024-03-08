using System;
using UnityEngine;
using TMPro;
using System.IO;

public class UIManager : MonoBehaviour
{
    public TMP_Text arenaText;
    public TMP_Text configFileNameText;
    public AAI3EnvironmentManager environmentManager;

    void Awake()
    {
        AAI3EnvironmentManager.OnArenaChanged += UpdateArenaUI;
    }

    void OnDestroy()
    {
        AAI3EnvironmentManager.OnArenaChanged -= UpdateArenaUI;
    }

    private void Start()
    {
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
