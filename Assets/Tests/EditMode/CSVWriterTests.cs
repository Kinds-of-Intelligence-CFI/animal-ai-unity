using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CSVWriterTests
{
    private GameObject testGameObject;
    private CSVWriter csvWriter;
    private string csvFilePath;
    private List<string> errorLogs;

    // Parameters for the log entry.
    private Vector3 velocity = new Vector3(1, 2, 3);
    private Vector3 position = new Vector3(4, 5, 6);
    private string actionForward = "forward";
    private string actionRotate = "rotate";
    private string wasAgentFrozen = "false";
    private float reward = 0.5f;
    private string notificationState = "none";
    private string dataZoneMessage = "No";
    private string activeCameraDescription = "mainCamera";
    private string combinedRaycastData = "rayData";
    private int stepCount = 1;
    private float health = 100f;

    [SetUp]
    public void Setup()
    {
        // Create a new GameObject and add the CSVWriter component.
        testGameObject = new GameObject("CSVWriterTestObject");
        csvWriter = testGameObject.AddComponent<CSVWriter>();

        // Initialize the CSV process so that the file is created and header is written.
        csvWriter.InitialiseCSVProcess();

        // Use reflection to grab the private csvFilePath field.
        FieldInfo filePathField = typeof(CSVWriter).GetField("csvFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
        csvFilePath = filePathField.GetValue(csvWriter) as string;

        // Register event to collect error logs
        errorLogs = new List<string>();
        Application.logMessageReceived += HandleLog;
    }

    [TearDown]
    public void Teardown()
    {
        // Ensure that the CSVWriter shuts down its flush thread and closes the writer.
        csvWriter.Shutdown();

        // Clean up the CSV file if it was created.
        if (!string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath))
        {
            File.Delete(csvFilePath);
        }

        // Destroy the test GameObject.
        UnityEngine.Object.DestroyImmediate(testGameObject);

        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Capture only error and exception logs
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorLogs.Add(logString);
        }
    }

    [Test]
    public void Test_LogToCSV_WritesLogEntry()
    {
        csvWriter.EpisodeBegin();

        // Record reward types so that they show up in the log entry.
        csvWriter.RecordRewardType("TestReward");
        csvWriter.RecordDispensedRewardType("DispensedTestReward");
        csvWriter.RecordDispensedReward();

        // Act: Log a CSV entry.
        csvWriter.LogToCSV(
            velocity,
            position,
            actionForward,
            actionRotate,
            wasAgentFrozen,
            reward,
            notificationState,
            dataZoneMessage,
            activeCameraDescription,
            combinedRaycastData,
            stepCount,
            health);

        // Manually flush and shut down to ensure the file is written.
        csvWriter.FlushLogQueue();
        csvWriter.Shutdown();

        // Assert: Check that the CSV file exists.
        Assert.IsTrue(File.Exists(csvFilePath), "CSV file was not created.");

        // Read all lines from the CSV file.
        string[] lines = File.ReadAllLines(csvFilePath);
        Assert.IsTrue(lines.Length >= 2, "CSV file does not contain both header and log entry.");

        // Verify the header line.
        string expectedHeader = "Episode,Step,Health,Reward,XVelocity,YVelocity,ZVelocity,XPosition,YPosition,ZPosition,ActionForwardWithDescription,ActionRotateWithDescription,WasAgentFrozen?,WasNotificationShown?,WasRewardDispensed?,DispensedRewardType,CollectedRewardType,WasSpawnerButtonTriggered?,CombinedSpawnerInfo,DataZoneMessage,ActiveCamera,CombinedRaycastData";
        Assert.AreEqual(expectedHeader, lines[0]);

        // Verify the log entry.
        // Note: The CSV entry is constructed with these fields in order:
        // stepCount, health, reward, velocity.x, velocity.y, velocity.z, position.x, position.y, position.z,
        // actionForward, actionRotate, wasAgentFrozen, notificationState,
        // wasRewardDispensed ("Yes" because RecordDispensedReward() was called),
        // dispensedRewardType, lastCollectedRewardType, wasSpawnerButtonTriggered ("No" by default),
        // combinedSpawnerInfo ("N/A"), dataZoneMessage, activeCameraDescription, combinedRaycastData
        string expectedLogEntry = "1,1,100,0.5,1,2,3,4,5,6,forward,rotate,false,none,Yes,DispensedTestReward,TestReward,No,N/A,No,mainCamera,rayData";
        Assert.AreEqual(expectedLogEntry, lines[1]);
    }

        [Test]
    public void Test_LogToCSV_ResetsCSVWriterState()
    {
        // All of the below bits should be reset after a call to LogToCSV
        // lastCollectedRewardType
        // dispensedRewardType
        // wasRewardDispensed
        // wasSpawnerButtonTriggered
        // combinedSpawnerInfo

        csvWriter.EpisodeBegin();

        // Record reward types so that they show up in the log entry.
        csvWriter.RecordRewardType("TestReward");
        csvWriter.RecordDispensedRewardType("DispensedTestReward");
        csvWriter.RecordDispensedReward();
        csvWriter.RecordSpawnerInfo("TestSpawner");
        csvWriter.OnRewardSpawned(testGameObject);

        // Act: Log a CSV entry.
        csvWriter.LogToCSV(
            velocity,
            position,
            actionForward,
            actionRotate,
            wasAgentFrozen,
            reward,
            notificationState,
            dataZoneMessage,
            activeCameraDescription,
            combinedRaycastData,
            stepCount,
            health);

        csvWriter.LogToCSV(
            velocity,
            position,
            actionForward,
            actionRotate,
            wasAgentFrozen,
            reward,
            notificationState,
            dataZoneMessage,
            activeCameraDescription,
            combinedRaycastData,
            stepCount+1,
            health);

        // Manually flush and shut down to ensure the file is written.
        csvWriter.FlushLogQueue();
        csvWriter.Shutdown();

        // Assert: Check that the CSV file exists.
        Assert.IsTrue(File.Exists(csvFilePath), "CSV file was not created.");

        // Read all lines from the CSV file.
        string[] lines = File.ReadAllLines(csvFilePath);
        Assert.IsTrue(lines.Length == 3, "CSV file does not contain expected number of lines.");

        // Verify the log entry.
        // Note: The CSV entry is constructed with these fields in order:
        // stepCount, health, reward, velocity.x, velocity.y, velocity.z, position.x, position.y, position.z,
        // actionForward, actionRotate, wasAgentFrozen, notificationState,
        // wasRewardDispensed ("Yes" because RecordDispensedReward() was called),
        // dispensedRewardType, lastCollectedRewardType, wasSpawnerButtonTriggered ("No" by default),
        // combinedSpawnerInfo ("N/A"), dataZoneMessage, activeCameraDescription, combinedRaycastData
        string expectedLogEntry_1 = "1,1,100,0.5,1,2,3,4,5,6,forward,rotate,false,none,Yes,DispensedTestReward,TestReward,Yes,TestSpawner,No,mainCamera,rayData";
        Assert.AreEqual(expectedLogEntry_1, lines[1]);
        // Verify that expected loglines have been reset after one step
        string expectedLogEntry_2 = "1,2,100,0.5,1,2,3,4,5,6,forward,rotate,false,none,No,None,None,No,N/A,No,mainCamera,rayData";
        Assert.AreEqual(expectedLogEntry_2, lines[2]);
    }

    [Test]
    public void Test_GoalsCollectedLines_ComeAtTheEndOfEpisodes()
    {
        csvWriter.EpisodeBegin();

        // Act: Log a CSV entry.
        csvWriter.LogToCSV(
            velocity,
            position,
            actionForward,
            actionRotate,
            wasAgentFrozen,
            reward,
            notificationState,
            dataZoneMessage,
            activeCameraDescription,
            combinedRaycastData,
            stepCount,
            health);

        csvWriter.ReportGoalsCollected(5);

        // Manually flush and shut down to ensure the file is written.
        csvWriter.Shutdown();

        // Assert: Check that the CSV file exists.
        Assert.IsTrue(File.Exists(csvFilePath), "CSV file was not created.");

        // Read all lines from the CSV file.
        string[] lines = File.ReadAllLines(csvFilePath);
        Assert.IsTrue(lines.Length == 3, "CSV file does not contain both header and log entry.");

        // Verify the header line.
        string expectedHeader = "Episode,Step,Health,Reward,XVelocity,YVelocity,ZVelocity,XPosition,YPosition,ZPosition,ActionForwardWithDescription,ActionRotateWithDescription,WasAgentFrozen?,WasNotificationShown?,WasRewardDispensed?,DispensedRewardType,CollectedRewardType,WasSpawnerButtonTriggered?,CombinedSpawnerInfo,DataZoneMessage,ActiveCamera,CombinedRaycastData";
        Assert.AreEqual(expectedHeader, lines[0]);

        // Verify the log entry.
        // Note: The CSV entry is constructed with these fields in order:
        // stepCount, health, reward, velocity.x, velocity.y, velocity.z, position.x, position.y, position.z,
        // actionForward, actionRotate, wasAgentFrozen, notificationState,
        // wasRewardDispensed ("Yes" because RecordDispensedReward() was called),
        // dispensedRewardType, lastCollectedRewardType, wasSpawnerButtonTriggered ("No" by default),
        // combinedSpawnerInfo ("N/A"), dataZoneMessage, activeCameraDescription, combinedRaycastData
        string expectedLogEntry = "1,1,100,0.5,1,2,3,4,5,6,forward,rotate,false,none,No,None,None,No,N/A,No,mainCamera,rayData";
        Assert.AreEqual(expectedLogEntry, lines[1]);

        string expectedGoalsCollectedLine = "Positive Goals Collected: 5";
        Assert.AreEqual(expectedGoalsCollectedLine, lines[2]);
    }

    [Test]
    public void Test_LogToCSV_DuplicateStep_Prevention()
    {
        // Arrange: Create a log entry with a specific step count.
        Vector3 velocity = Vector3.zero;
        Vector3 position = Vector3.zero;
        string actionForward = "actFwd";
        string actionRotate = "actRot";
        string wasAgentFrozen = "false";
        float reward = 0.0f;
        string notificationState = "none";
        string dataZoneMessage = "No";
        string activeCameraDescription = "cam";
        string combinedRaycastData = "ray";
        int stepCount = 5;
        float health = 90f;

        // Act: Call LogToCSV twice with the same step count.
        csvWriter.LogToCSV(velocity, position, actionForward, actionRotate, wasAgentFrozen, reward, notificationState, dataZoneMessage, activeCameraDescription, combinedRaycastData, stepCount, health);
        csvWriter.LogToCSV(velocity, position, actionForward, actionRotate, wasAgentFrozen, reward, notificationState, dataZoneMessage, activeCameraDescription, combinedRaycastData, stepCount, health);

        csvWriter.FlushLogQueue();
        csvWriter.Shutdown();

        // Assert: The file should contain only the header and one log entry.
        string[] lines = File.ReadAllLines(csvFilePath);
        Assert.AreEqual(2, lines.Length, "A duplicate log entry was recorded for the same step.");
    }

    [Test]
    public void Test_RecordSpawnerInfo_CombinesInfo()
    {
        // Arrange: Record two pieces of spawner info.
        string spawnerInfo1 = "Info1";
        string spawnerInfo2 = "Info2,withComma";  // The comma should be replaced by a semicolon in the log entry.

        // Act: Record the spawner info twice.
        csvWriter.RecordSpawnerInfo(spawnerInfo1);
        csvWriter.RecordSpawnerInfo(spawnerInfo2);

        // Use reflection to retrieve the internal combined spawner info.
        FieldInfo spawnerField = typeof(CSVWriter).GetField("combinedSpawnerInfo", BindingFlags.NonPublic | BindingFlags.Instance);
        string combinedSpawnerInfo = spawnerField.GetValue(csvWriter) as string;

        // Assert: The expected combined string should join the entries with a '|' and replace commas.
        string expectedCombined = "Info1|Info2,withComma";
        Assert.AreEqual(expectedCombined, combinedSpawnerInfo);
    }

    [Test]
    public void Test_ShuttingDownDoesNotLogError()
    {
        csvWriter.Shutdown();

        // Assert: Ensure no Debug.LogError (or exceptions) were captured.
        Assert.IsEmpty(errorLogs, "Unexpected error logs were emitted: " + string.Join(", ", errorLogs));
    }
}
