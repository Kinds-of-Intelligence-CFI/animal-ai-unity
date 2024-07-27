using System.Linq;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using PrefabInterface;
using Unity.MLAgents.Sensors;
using YAMLDefs;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

/// <summary>
/// The TrainingAgent class is a subclass of the Agent class in the ML-Agents library.
/// Actions are currently discrete. 2 branches of 0,1,2, 0,1,2 for forward and rotate respectively.
/// </summary>

public class TrainingAgent : Agent, IPrefab
{
    [Header("Agent Settings")]
    public float speed = 25f;
    public float quickStopRatio = 0.9f;
    public float rotationSpeed = 100f;
    public float rotationAngle = 0.25f;
    private int lastActionForward = 0;
    private int lastActionRotate = 0;

    [Header("Agent State / Other Variables")]
    [HideInInspector]
    public int numberOfGoalsCollected = 0;

    [HideInInspector]
    public ProgressBar progBar;
    private Rigidbody _rigidBody;
    private bool _isGrounded;
    private ContactPoint _lastContactPoint;

    [Header("Agent Rewards & Score")]
    private float _rewardPerStep;
    private float _previousScore = 0;
    private float _currentScore = 0;

    [Header("Agent Health")]
    public float health = 100f;
    private float _maxHealth = 100f;

    [Header("Agent Freeze & Countdown")]
    public float timeLimit = 0f;
    private float _nextUpdateHealth = 0f;
    private float _freezeDelay = 0f;
    private bool _isFrozen = false;

    private bool _nextUpdateCompleteArena = false;

    [Header("Agent Notification")]
    public bool showNotification = false;
    private TrainingArena _arena;
    private bool _isCountdownActive = false;

    [Header("External References")]
    public PlayerControls playerControls;

    [Header("CSV Logging")]
    public string csvFilePath = "";
    private StreamWriter writer;
    private bool headerWritten = false;
    private int customEpisodeCount = 0;
    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private const int bufferSize = 101; /* Corresponds to rows in the CSV file to keep in memory before flushing to disk */
    private bool isFlushing = false;
    private string lastCollectedRewardType = "None";
    private string dispensedRewardType = "None";
    private string wasAgentInDataZone = "No";
    private bool wasRewardDispensed = false;
    private bool wasSpawnerButtonTriggered = false;
    private string combinedSpawnerInfo = "";

    private string yamlFileName;
    private AutoResetEvent flushEvent = new AutoResetEvent(false);

    public void RecordSpawnerInfo(string spawnerInfo)
    {
        if (string.IsNullOrEmpty(combinedSpawnerInfo))
        {
            combinedSpawnerInfo = spawnerInfo;
        }
        else
        {
            combinedSpawnerInfo += $"|{spawnerInfo}";
        }
    }

    public void OnInDataZone(string zoneLogString)
    {
        wasAgentInDataZone = "Agent was in DataZone: " + zoneLogString;
    }

    public void OnOutDataZone(string zoneLogString)
    {
        wasAgentInDataZone = "Agent left DataZone: " + zoneLogString;
    }

    private void OnRewardSpawned(GameObject reward)
    {
        wasSpawnerButtonTriggered = true;
    }

    public void RecordDispensedReward()
    {
        wasRewardDispensed = true;
    }

    public void RecordRewardType(string type)
    {
        lastCollectedRewardType = type;
    }

    public void RecordDispensedRewardType(string type)
    {
        dispensedRewardType = type;
    }

    public void SetYamlFileName(string fileName)
    {
        yamlFileName = fileName;
    }

    private string CombineRaycastData(float[] observations, string[] tags)
    {
        return string.Join(";", observations.Zip(tags, (obs, tag) => $"{obs}:{tag}"));
    }

    /// <summary>
    /// Initialize is called when the training session is started from mlagents. It sets up the agent's initial state.
    /// </summary>
    public override void Initialize()
    {
        _arena = GetComponentInParent<TrainingArena>();
        _rigidBody = GetComponent<Rigidbody>();
        _rewardPerStep = timeLimit > 0 ? -1f / timeLimit : 0;
        progBar = GameObject.Find("UI ProgressBar").GetComponent<ProgressBar>();
        progBar.AssignAgent(this);
        health = _maxHealth;

        Spawner_InteractiveButton.RewardSpawned += OnRewardSpawned;
        DataZone.OnInDataZone += OnInDataZone;
        DataZone.OnOutDataZone += OnOutDataZone;

        playerControls = GameObject.FindObjectOfType<PlayerControls>();

        InitialiseCSVProcess();

        if (!Application.isEditor)
        {
            AAI3EnvironmentManager envManager = FindObjectOfType<AAI3EnvironmentManager>();
            if (envManager != null)
            {
                SetYamlFileName(envManager.GetCurrentYamlFileName());
            }
        }

        StartFlushThread();
    }

    /// <summary>
    /// OnDisable is called when the training session is stopped form mlagents.
    /// It closes the CSV file and flushes the log queue at whatever the current size is.
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        if (writer != null)
        {
            FlushLogQueue(); /* Flush any remaining logs in the queue */
            writer.Close();
        }

        Spawner_InteractiveButton.RewardSpawned -= OnRewardSpawned;
        DataZone.OnInDataZone -= OnInDataZone;
        DataZone.OnOutDataZone -= OnOutDataZone;
    }

    public void AddExtraReward(float rewardFactor)
    {
        UpdateHealth(Math.Min(rewardFactor * _rewardPerStep, -0.001f));
    }

    public float GetPreviousScore()
    {
        return _previousScore;
    }

    public float GetFreezeDelay()
    {
        return _freezeDelay;
    }

    public void SetFreezeDelay(float v)
    {
        _freezeDelay = Mathf.Clamp(v, 0f, v);
        if (v != 0f && !_isCountdownActive)
        {
            Debug.Log(
                "Starting coroutine UnfreezeCountdown() with wait seconds == " + GetFreezeDelay()
            );
            StartCoroutine(UnfreezeCountdown());
        }
    }

    public bool IsFrozen()
    {
        return _freezeDelay > 0f || _isFrozen;
    }

    public void FreezeAgent(bool freeze)
    {
        _isFrozen = freeze;
        if (_isFrozen)
        {
            _rigidBody.velocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
        }
    }

    private IEnumerator UnfreezeCountdown()
    {
        _isCountdownActive = true;
        yield return new WaitForSeconds(GetFreezeDelay());

        Debug.Log("Unfreezing Agent!");
        SetFreezeDelay(0f);
        _isCountdownActive = false;
    }

    private string GetNotificationState()
    {
        if (NotificationManager.Instance == null)
        {
            return "None";
        }

        return NotificationManager.Instance.GetCurrentNotificationState();
    }

    private (float[] hitFractions, string[] hitTags) CollectRaycastObservations()
    {
        RayPerceptionSensorComponent3D rayPerception =
            GetComponent<RayPerceptionSensorComponent3D>();
        if (rayPerception == null)
        {
            return (new float[0], new string[0]);
        }

        var rayPerceptionInput = rayPerception.GetRayPerceptionInput();
        var rayPerceptionOutput = RayPerceptionSensor.Perceive(rayPerceptionInput);
        float[] hitFractions = rayPerceptionOutput.RayOutputs.Select(r => r.HitFraction).ToArray();
        string[] hitTags = rayPerceptionOutput.RayOutputs
            .Select(r => r.HitGameObject?.tag ?? "None")
            .ToArray();

        return (hitFractions, hitTags);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(health);
        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.velocity);
        Vector3 localPos = transform.position;
        bool wasAgentFrozen = IsFrozen();
        string actionForwardDescription = DescribeActionForward(lastActionForward);
        string actionRotateDescription = DescribeActionRotate(lastActionRotate);
        string actionForwardWithDescription = $"{lastActionForward} ({actionForwardDescription})";
        string actionRotateWithDescription = $"{lastActionRotate} ({actionRotateDescription})";
        float reward = GetCumulativeReward();
        string notificationState = GetNotificationState();
        (float[] raycastObservations, string[] raycastTags) = CollectRaycastObservations();
        string combinedRaycastData = CombineRaycastData(raycastObservations, raycastTags);
    }

    public override void OnActionReceived(ActionBuffers action)
    {
        lastActionForward = Mathf.FloorToInt(action.DiscreteActions[0]);
        lastActionRotate = Mathf.FloorToInt(action.DiscreteActions[1]);

        if (!IsFrozen())
        {
            MoveAgent(lastActionForward, lastActionRotate);
        }

        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.velocity);
        Vector3 localPos = transform.position;
        bool wasAgentFrozen = IsFrozen();
        string actionForwardDescription = DescribeActionForward(lastActionForward);
        string actionRotateDescription = DescribeActionRotate(lastActionRotate);
        string actionForwardWithDescription = $"{lastActionForward} ({actionForwardDescription})";
        string actionRotateWithDescription = $"{lastActionRotate} ({actionRotateDescription})";
        float reward = GetCumulativeReward();
        string notificationState = GetNotificationState();

        (float[] raycastObservations, string[] raycastTags) = CollectRaycastObservations();
        string combinedRaycastData = CombineRaycastData(raycastObservations, raycastTags);

        LogToCSV(
            localVel,
            localPos,
            actionForwardWithDescription,
            actionRotateWithDescription,
            wasAgentFrozen ? "Yes" : "No",
            reward,
            notificationState,
            customEpisodeCount,
            wasRewardDispensed,
            dispensedRewardType,
            lastCollectedRewardType,
            wasSpawnerButtonTriggered,
            combinedSpawnerInfo,
            wasAgentInDataZone,
            playerControls.GetActiveCameraDescription(),
            combinedRaycastData
        );

        dispensedRewardType = "None";
        wasRewardDispensed = false;
        wasSpawnerButtonTriggered = false;
        wasAgentInDataZone = "No";
        combinedSpawnerInfo = "";

        UpdateHealth(_rewardPerStep);
    }

    private void InitialiseCSVProcess()
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

        /* Generate a filename with the YAML file name and a date stamp to prevent overwriting. */
        // TODO: Extract YAML name from side channel message.
        string dateTimeString = DateTime.Now.ToString("dd-MM-yy_HHmm");
        string filename = $"Observations_{yamlFileName}_{dateTimeString}.csv";
        csvFilePath = Path.Combine(directoryPath, filename);

        writer = new StreamWriter(csvFilePath, true);

        if (!File.Exists(csvFilePath) || new FileInfo(csvFilePath).Length == 0)
        {
            if (!headerWritten)
            {
                writer.WriteLine(
                    "Episode,Step,Health,Reward,XVelocity,YVelocity,ZVelocity,XPosition,YPosition,ZPosition,ActionForwardWithDescription,ActionRotateWithDescription,WasAgentFrozen?,NotificationShown?,WasRewardDispensed?,DispensedRewardType,CollectedRewardType,WasSpawnerButtonTriggered?,CombinedSpawnerInfo,WasAgentInDataZone?,ActiveCamera,CombinedRaycastData"
                );
                headerWritten = true;
            }
            else
            {
                Debug.LogError("Header/Columns already written to CSV file.");
            }
        }
    }

    /// <summary>
    /// Logs the agent's state to a CSV file. This is called every step.
    /// The data is stored in a queue and flushed to the file in a separate thread.
    /// </summary>
    private void LogToCSV(
        Vector3 velocity,
        Vector3 position,
        string actionForwardWithDescription,
        string actionRotateWithDescription,
        string wasAgentFrozen,
        float reward,
        string notificationState,
        int customEpisodeCount,
        bool wasRewardDispensed,
        string dispensedRewardType,
        string lastCollectedRewardType,
        bool wasSpawnerButtonTriggered,
        string combinedSpawnerInfo,
        string wasAgentInDataZone,
        string activeCameraDescription,
        string combinedRaycastData
    )
    {
        string logEntry = string.Format(
            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21}",
            customEpisodeCount,
            StepCount,
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

        if (logQueue.Count >= bufferSize)
        {
            flushEvent.Set();
        }
    }

    /// <summary>
    /// Flushes the log queue to the CSV file. This is called in a separate thread to prevent the main thread from being blocked.
    /// </summary>
    private void FlushLogQueue()
    {
        lock (logQueue)
        {
            while (logQueue.TryDequeue(out var logEntry))
            {
                writer.WriteLine(logEntry);
            }
            writer.Flush();
            Debug.Log("Flushed log queue to CSV file.");
        }
    }

    /// <summary>
    /// Starts a thread that checks if the log queue is full and flushes it to the CSV file if it is.
    /// </summary>
    private void StartFlushThread()
    {
        new Thread(() =>
        {
            while (true)
            {
                flushEvent.WaitOne();
                if (logQueue.Count >= bufferSize && !isFlushing)
                {
                    isFlushing = true;
                    FlushLogQueue();
                    isFlushing = false;
                }
            }
        }).Start();
    }

    private string DescribeActionForward(int actionForward)
    {
        return actionForward switch
        {
            0 => "No Movement",
            1 => "Move Forward",
            2 => "Move Backward",
            _ => "Unknown Forward Action"
        };
    }

    private string DescribeActionRotate(int actionRotate)
    {
        return actionRotate switch
        {
            0 => "No Rotation",
            1 => "Rotate Right",
            2 => "Rotate Left",
            _ => "Unknown Rotation Action"
        };
    }

    private void MoveAgent(int actionForward, int actionRotate)
    {
        if (IsFrozen())
        {
            /* If the agent is frozen, stop all movement and rotation */
            _rigidBody.velocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
            return;
        }

        Vector3 directionToGo = Vector3.zero;
        Vector3 rotateDirection = Vector3.zero;
        Vector3 quickStop = Vector3.zero;

        if (_isGrounded)
        {
            switch (actionForward)
            {
                case 1:
                    directionToGo = transform.forward * 1f;
                    break;
                case 2:
                    directionToGo = transform.forward * -1f;
                    break;
                case 0: /* Slow down faster than drag with no input */
                    quickStop = _rigidBody.velocity * quickStopRatio;
                    _rigidBody.velocity = quickStop;
                    break;
            }
        }

        switch (actionRotate)
        {
            case 1:
                rotateDirection = transform.up * 1f;
                break;
            case 2:
                rotateDirection = transform.up * -1f;
                break;
        }

        transform.Rotate(rotateDirection, Time.fixedDeltaTime * rotationSpeed);
        _rigidBody.AddForce(
            directionToGo.normalized * speed * 100f * Time.fixedDeltaTime,
            ForceMode.Acceleration
        );
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        discreteActionsOut[1] = 0;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[1] = 2;
        }
    }

    public void UpdateHealthNextStep(float updateAmount, bool andCompleteArena = false)
    {
        /*
            IMPORTANT!
            ML-Agents doesn't guarantee behaviour if an episode ends outside of OnActionReceived
            Therefore we queue any health updates to happen on the next action step.
        */
        _nextUpdateHealth += updateAmount;
        if (andCompleteArena)
        {
            _nextUpdateCompleteArena = true;
        }
    }

    public void UpdateHealth(float updateAmount, bool andCompleteArena = false)
    {
        if (!IsFrozen())
        {
            health += 100 * updateAmount;
            health += 100 * _nextUpdateHealth;
            _nextUpdateHealth = 0;
            AddReward(updateAmount);
        }

        _currentScore = GetCumulativeReward();

        /* Ensure health does not exceed maximum limits */
        if (health > _maxHealth)
        {
            health = _maxHealth;
        }
        else if (health <= 0)
        {
            health = 0;

            if (showNotification)
            {
                NotificationManager.Instance.ShowFailureNotification();
            }
            StartCoroutine(EndEpisodeAfterDelay());
            return;
        }

        // TODO: Debug current pass mark for arena == 0
        if (andCompleteArena || _nextUpdateCompleteArena)
        {
            _nextUpdateCompleteArena = false;
            float cumulativeReward = GetCumulativeReward();
            Debug.Log($"Current pass mark: {Arena.CurrentPassMark}");

            bool proceedToNext =
                Arena.CurrentPassMark == 0 || cumulativeReward >= Arena.CurrentPassMark;

            if (proceedToNext)
            {
                if (_arena.mergeNextArena)
                {
                    _arena.LoadNextArena();
                    return;
                }

                if (showNotification)
                {
                    NotificationManager.Instance.ShowSuccessNotification();
                }
            }
            else
            {
                if (showNotification)
                {
                    NotificationManager.Instance.ShowFailureNotification();
                }
            }

            StartCoroutine(EndEpisodeAfterDelay());
        }
    }

    IEnumerator EndEpisodeAfterDelay()
    {
        if (!showNotification)
        {
            EndEpisode();
            yield break;
        }
        yield return new WaitForSeconds(2.5f);

        NotificationManager.Instance.HideNotification();

        EndEpisode();
        FlushLogQueue();
    }

    public override void OnEpisodeBegin()
    {
        if (!_arena.IsFirstArenaReset)
        {
            writer.WriteLine($"Goals Collected: {numberOfGoalsCollected}");
            writer.Flush();
        }

        numberOfGoalsCollected = 0;
        customEpisodeCount++;

        writer.Flush();
        EpisodeDebugLogs();

        StopAllCoroutines();
        _previousScore = _currentScore;
        numberOfGoalsCollected = 0;
        _arena.ResetArena();
        _rewardPerStep = timeLimit > 0 ? -1f / timeLimit : 0;
        _isGrounded = false;
        health = _maxHealth;

        SetFreezeDelay(GetFreezeDelay());
    }

    private void EpisodeDebugLogs()
    {
        Debug.Log("Episode Begin");
        Debug.Log($"Value of showNotification: {showNotification}");
        Debug.Log("Current Pass Mark: " + Arena.CurrentPassMark);
        Debug.Log("Number of Goals Collected: " + numberOfGoalsCollected);
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0)
            {
                _isGrounded = true;
            }
        }
        _lastContactPoint = collision.contacts.Last();
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0)
            {
                _isGrounded = true;
            }
        }
        _lastContactPoint = collision.contacts.Last();
    }

    void OnCollisionExit(Collision collision)
    {
        if (_lastContactPoint.normal.y > 0)
        {
            _isGrounded = false;
        }
    }

    //******************************
    //PREFAB INTERFACE FOR THE AGENT
    //******************************

    public void SetColor(Vector3 color) { }

    public void SetSize(Vector3 scale) { }

    /// <summary>
    /// Returns a random position within the range for the object.
    /// </summary>
    public virtual Vector3 GetPosition(
        Vector3 position,
        Vector3 boundingBox,
        float rangeX,
        float rangeZ
    )
    {
        float xBound = boundingBox.x;
        float zBound = boundingBox.z;
        float xOut =
            position.x < 0
                ? Random.Range(xBound, rangeX - xBound)
                : Math.Max(0, Math.Min(position.x, rangeX));
        float yOut = Math.Max(position.y, 0) + transform.localScale.y / 2 + 0.01f;
        float zOut =
            position.z < 0
                ? Random.Range(zBound, rangeZ - zBound)
                : Math.Max(0, Math.Min(position.z, rangeZ));

        return new Vector3(xOut, yOut, zOut);
    }

    ///<summary>
    /// If rotationY set to < 0, then change to random rotation.
    ///</summary>
    public virtual Vector3 GetRotation(float rotationY)
    {
        return new Vector3(0, rotationY < 0 ? Random.Range(0f, 360f) : rotationY, 0);
    }
}
