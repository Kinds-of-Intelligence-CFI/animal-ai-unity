using System;
using UnityEngine;
using TMPro;
using System.IO;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text arenaText;
    public TMP_Text totalObjectsText;
    public AAI3EnvironmentManager environmentManager;

    public TrainingArena trainingArena;

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
        trainingArena = GameObject.FindObjectOfType<TrainingArena>();
        Debug.Assert(trainingArena != null, "TrainingArena not found in the scene");
        UpdateArenaUI(
            environmentManager.GetCurrentArenaIndex(),
            environmentManager.GetTotalArenas()
        );
    }

    private void UpdateArenaUI(int currentArenaIndex, int totalArenas)
    {
        arenaText.text = $"Arena {currentArenaIndex + 1} of {totalArenas}";
        totalObjectsText.text = $"Total Objects: {trainingArena.Builder.GetTotalObjectsSpawned()}";
    }
}
