using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using ArenaBuilders;
using UnityEngineExtensions;
using ArenasParameters;
using Holders;
using Random = UnityEngine.Random;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// This class is responsible for managing writing details of the episode to the .csv file
/// </summary>
public class CSVWriter : MonoBehaviour
{
    [Header("CSV Logging")]
    private string csvFilePath = "";
    private StreamWriter writer;
    private bool headerWritten = false;
    private int episodeCount = 0;
    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private const int bufferSize = 101; /* Corresponds to rows in the CSV file to keep in memory before flushing to disk */
    private bool isFlushing = false;
    private AutoResetEvent flushEvent = new AutoResetEvent(false);
    private bool threadRunning = true;
    private string lastCollectedRewardType = "None";
    private string dispensedRewardType = "None";
    private bool wasRewardDispensed = false;
    private bool wasSpawnerButtonTriggered = false;
    private string combinedSpawnerInfo = "N/A";
    private int lastLoggedStep = -1;
    private Thread flushThread;

    /// <summary>
    /// Logs the agent's state to a CSV file. This is called every step.
    /// The data is stored in a queue and flushed to the file in a separate thread.
    /// </summary>
    public void LogToCSV(
        Vector3 velocity,
        Vector3 position,
        string actionForwardWithDescription,
        string actionRotateWithDescription,
        string wasAgentFrozen,
        float reward,
        string notificationState,
        string wasAgentInDataZone,
        string activeCameraDescription,
        string combinedRaycastData,
        int stepCount,
        float health
    )
    {
        /* Check if the current step has already been logged */
        if (stepCount == lastLoggedStep)
        {
            Debug.Log("Skipping duplicated log step");
            return;
        }
        string logEntry = string.Format(
            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21}",
            episodeCount,
            stepCount,
            health,
            reward,
            velocity.x,
            velocity.y,
            velocity.z,
            position.x,
            position.y,
            position.z,
            actionForwardWithDescription,
            actionRotateWithDescription,
            wasAgentFrozen,
            notificationState,
            wasRewardDispensed ? "Yes" : "No",
            dispensedRewardType,
            lastCollectedRewardType,
            wasSpawnerButtonTriggered ? "Yes" : "No",
            combinedSpawnerInfo.Replace(",", ";"),
            wasAgentInDataZone,
            activeCameraDescription,
            combinedRaycastData
        );

        logQueue.Enqueue(logEntry);
        lastCollectedRewardType = "None";
        dispensedRewardType = "None";
        wasRewardDispensed = false;
        wasSpawnerButtonTriggered = false;
        combinedSpawnerInfo = "N/A";

        lastLoggedStep = stepCount;

        if (logQueue.Count >= bufferSize)
        {
            flushEvent.Set();
        }
    }

    public void EpisodeBegin()
    {
        lastLoggedStep = -1;
        episodeCount++;
    }

    // TODO: Potential bug that if this is called twice before the CSV is written to we lose a reward type
    public void RecordRewardType(string type)
    {
        lastCollectedRewardType = type;
    }

    public void RecordDispensedRewardType(string type)
    {
        dispensedRewardType = type;
    }

    public void RecordDispensedReward()
    {
        wasRewardDispensed = true;
    }

    public void OnRewardSpawned(GameObject reward)
    {
        wasSpawnerButtonTriggered = true;
    }

    public void ReportGoalsCollected(int numberOfGoalsCollected){
        logQueue.Enqueue($"Goals Collected: {numberOfGoalsCollected}");
        FlushLogQueue();
    }

    public void Shutdown(bool onlyCloseWriter = false){
        // TODO: onlyCloseWriter is included to ensure the fidelity of the refactor, should be removed
        if (!onlyCloseWriter) {
            threadRunning = false; /* Signal the flush thread to stop */
            flushEvent.Set(); /* Ensure the thread is not stuck in WaitOne() */
            if (flushThread != null && flushThread.IsAlive)
            {
                flushThread.Join();        // Wait for the flush thread to exit.
            } else {
                Debug.LogWarning("Shutdown not able to find flushthread to wait for");
            }
        }
        writer.Close(); /* Close the writer */
    }

    public void InitialiseCSVProcess()
    {
        /* Base path for the logs to be stored */
        string basePath;

        if (Application.isEditor)
        {
            /* The root directory is the parent of the Assets folder */
            basePath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
        else
        {
            /* Important! For builds, use the parent of the directory where the executable resides */
            basePath = Path.GetDirectoryName(Application.dataPath);
        }

        /* Directory to store the logs under folder "ObservationLogs" */
        string directoryPath = Path.Combine(basePath, "ObservationLogs");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        /* Generate a filename with a date stamp to prevent overwriting */
        string dateTimeString = DateTime.Now.ToString("dd-MM-yy_HHmm");
        string filename = $"Observations_{dateTimeString}.csv";
        csvFilePath = Path.Combine(directoryPath, filename);

        writer = new StreamWriter(
            new FileStream(csvFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)
        );

        if (!File.Exists(csvFilePath) || new FileInfo(csvFilePath).Length == 0)
        {
            if (!headerWritten)
            {
                writer.WriteLine(
                    "Episode,Step,Health,Reward,XVelocity,YVelocity,ZVelocity,XPosition,YPosition,ZPosition,ActionForwardWithDescription,ActionRotateWithDescription,WasAgentFrozen?,WasNotificationShown?,WasRewardDispensed?,DispensedRewardType,CollectedRewardType,WasSpawnerButtonTriggered?,CombinedSpawnerInfo,WasAgentInDataZone?,ActiveCamera,CombinedRaycastData"
                );
                headerWritten = true;
            }
            else
            {
                Debug.LogError("Header/Columns already written to CSV file.");
            }
        }
        StartFlushThread();
    }

    /// <summary>
    /// Flushes the log queue to the CSV file. This is called in a separate thread to prevent the main thread from being blocked.
    /// </summary>
    private void StartFlushThread()
    {
        flushThread = new Thread(() =>
        {
            while (threadRunning)
            {
                flushEvent.WaitOne();
                if (logQueue.Count >= bufferSize && !isFlushing)
                {
                    isFlushing = true;
                    FlushLogQueue();
                    isFlushing = false;
                }
            }
            FlushLogQueue();
        });
        // Mark the thread as background so if we have a runtime error it won't stop the application from closing
        flushThread.IsBackground = true;
        flushThread.Start();
    }

    public void FlushLogQueue() /* TODO: Ensure all calls to this are guarded by the flushEvent?*/
    {
        try
        {
            lock (logQueue)
            {
                while (logQueue.TryDequeue(out var logEntry))
                {
                    writer.WriteLine(logEntry);
                }
                Debug.Log("Flushed log queue to CSV file.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to flush log queue: " + ex.Message);
        }
    }

    public void RecordSpawnerInfo(string spawnerInfo)
    {
        if (string.IsNullOrEmpty(spawnerInfo))
        {
            combinedSpawnerInfo = "N/A";
        }
        else
        {
            if (combinedSpawnerInfo == "N/A")
            {
                combinedSpawnerInfo = spawnerInfo;
            }
            else
            {
                combinedSpawnerInfo += $"|{spawnerInfo}";
            }
        }
        Debug.Log($"Recorded Spawner Info: {combinedSpawnerInfo}");
    }
}
