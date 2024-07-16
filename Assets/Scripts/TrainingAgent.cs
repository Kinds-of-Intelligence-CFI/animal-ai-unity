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
/// It is used to define the behaviour of the agent in the training environment.
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

    [Header("CSV Logging")]
    public string csvFilePath = "";
    private StreamWriter writer;
    private bool headerWritten = false;
    private string yamlFileName;
    private int customEpisodeCount = 0;
    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private const int bufferSize = 150; /* Corresponds to rows in the CSV file to keep in memory before flushing to disk */
    private bool isFlushing = false;
    private string lastCollectedRewardType = "None";
    private string dispensedRewardType = "None";
    private string wasInDataZone = "No";
    private bool wasRewardDispensed = false;
    private bool wasButtonPressed = false;

    public void OnInDataZone(string zoneLogString)
    {
        wasInDataZone = "Agent was in DataZone: " + zoneLogString;
    }

    private void OnRewardSpawned(GameObject reward)
    {
        wasButtonPressed = true;
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

    private void InitialiseCSVProcess()
    {
        // Base path for the logs to be stored
        string basePath;

        if (Application.isEditor)
        {
            // The root directory is the parent of the Assets folder
            basePath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
        else
        {
            // Important! For builds, use the parent of the directory where the executable resides
            basePath = Path.GetDirectoryName(Application.dataPath);
        }

        // Folder for the CSV logs
        string directoryPath = Path.Combine(basePath, "ObservationLogs");

        // Simple check to see if the directory exists, if not create it
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Generate a filename with the YAML file name and a date stamp to prevent overwriting. TODO: Extract YAML name from side channel message
        string dateTimeString = DateTime.Now.ToString("dd-MM-yy_HHmm");
        string filename = $"Observations_{yamlFileName}_{dateTimeString}.csv";
        csvFilePath = Path.Combine(directoryPath, filename);

        writer = new StreamWriter(csvFilePath, true);

        if (!File.Exists(csvFilePath) || new FileInfo(csvFilePath).Length == 0)
        {
            if (!headerWritten)
            {
                writer.WriteLine(
                    "Episode,Step,Reward,CollectedRewardType,Health,XVelocity,YVelocity,ZVelocity,XPosition,YPosition,ZPosition,ActionForward,ActionRotate,ActionForwardDescription,ActionRotateDescription,IsFrozen?,NotificationState,DispensedRewardType,WasRewardDispensed?,WasButtonPressed?,RaycastObservations,RaycastTags,WasInDataZone?"
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
    /// Logs the agent's state to a CSV file. This is called every step. The data is stored in a queue and flushed to the file in a separate thread.
    /// </summary>
    private void LogToCSV(
        Vector3 velocity,
        Vector3 position,
        int lastActionForward,
        int lastActionRotate,
        string actionForwardDescription,
        string actionRotateDescription,
        bool isFrozen,
        float reward,
        string notificationState,
        int customEpisodeCount,
        string DispensedRewardType,
        bool wasRewardDispensed,
        bool wasButtonPressed,
        float[] raycastObservations,
        string[] raycastTags,
        string wasInDataZone
    )
    {
        string raycastData = string.Join(",", raycastObservations);
        string raycastTagsData = string.Join(",", raycastTags);
        string logEntry =
            $"{customEpisodeCount},{StepCount},{reward},{lastCollectedRewardType},{health},{velocity.x},{velocity.y},{velocity.z},{position.x},{position.y},{position.z},{lastActionForward},{lastActionRotate},{actionForwardDescription},{actionRotateDescription},{isFrozen},{notificationState},{DispensedRewardType},{wasRewardDispensed},{wasButtonPressed},{raycastData},{raycastTagsData},{wasInDataZone}";
        logQueue.Enqueue(logEntry);
        lastCollectedRewardType = "None";
    }

    /// <summary>
    /// Flushes the log queue to the CSV file. This is called in a separate thread to prevent the main thread from being blocked.
    /// </summary>
    private void FlushLogQueue()
    {
        while (logQueue.TryDequeue(out var logEntry))
        {
            writer.WriteLine(logEntry);
        }
        writer.Flush();
        Debug.Log("Flushed log queue to CSV file.");
    }

    /// <summary>
    /// Starts a thread that checks if the log queue is full and flushes it to the CSV file if it is.
    // WARNING: im not sure if this is the best way to handle this, but it works for now
    /// </summary>
    private void StartFlushThread()
    {
        new Thread(() =>
        {
            while (true)
            {
                if (logQueue.Count >= bufferSize && !isFlushing)
                {
                    isFlushing = true;
                    FlushLogQueue();
                    isFlushing = false;
                }
                Thread.Sleep(100); // polls every 100ms to check if the queue is full. This is to prevent the thread from running continuously and hogging resources.
            }
        }).Start();
    }

    /// <summary>
    /// OnDisable is called when the training session is stopped form mlagents. It closes the CSV file and flushes the log queue at whatever the current size is.
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        if (writer != null)
        {
            FlushLogQueue(); // Ensures all buffered data is written to the file before closing (important for builds)
            writer.Close();
        }

        Spawner_InteractiveButton.RewardSpawned -= OnRewardSpawned;
        DataZone.OnInDataZone -= OnInDataZone;
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

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(health);
        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.velocity);
        Vector3 localPos = transform.position;
        bool isFrozen = IsFrozen();
        string actionForwardDescription = DescribeActionForward(lastActionForward);
        string actionRotateDescription = DescribeActionRotate(lastActionRotate);
        float reward = GetCumulativeReward();
        string notificationState = GetNotificationState();

        (float[] raycastObservations, string[] raycastTags) = CollectRaycastObservations();
        foreach (float observation in raycastObservations)
        {
            sensor.AddObservation(observation);
        }

        LogToCSV(
            localVel,
            localPos,
            lastActionForward,
            lastActionRotate,
            actionForwardDescription,
            actionRotateDescription,
            isFrozen,
            reward,
            notificationState,
            customEpisodeCount,
            dispensedRewardType,
            wasRewardDispensed,
            wasButtonPressed,
            raycastObservations,
            raycastTags,
            wasInDataZone
        );
        dispensedRewardType = "None";
        wasRewardDispensed = false;
        wasButtonPressed = false;
        wasInDataZone = "No";
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
        bool isFrozen = IsFrozen();
        string actionForwardDescription = DescribeActionForward(lastActionForward);
        string actionRotateDescription = DescribeActionRotate(lastActionRotate);
        float reward = GetCumulativeReward();
        string notificationState = GetNotificationState();

        // Collect raycast observations and tags directly from the RayPerceptionSensorComponent3D
        // TODO: Need to check if this is the correct way to collect raycast observations
        (float[] raycastObservations, string[] raycastTags) = CollectRaycastObservations();

        LogToCSV(
            localVel,
            localPos,
            lastActionForward,
            lastActionRotate,
            actionForwardDescription,
            actionRotateDescription,
            isFrozen,
            reward,
            notificationState,
            customEpisodeCount,
            dispensedRewardType,
            wasRewardDispensed,
            wasButtonPressed,
            raycastObservations,
            raycastTags,
            wasInDataZone
        );
        dispensedRewardType = "None";
        wasRewardDispensed = false;
        wasButtonPressed = false;
        wasInDataZone = "No";

        UpdateHealth(_rewardPerStep);
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
            // If the agent is frozen, stop all movement and rotation
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
        // Update the health of the agent and reset any queued updates
        if (!IsFrozen())
        {
            health += 100 * updateAmount;
            health += 100 * _nextUpdateHealth;
            _nextUpdateHealth = 0;
            AddReward(updateAmount);
        }

        _currentScore = GetCumulativeReward();

        // Ensure health does not exceed maximum limits
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

        // Handle arena completion or episode ending
        // TODO: Debug current pass mark for arena == 0
        if (andCompleteArena || _nextUpdateCompleteArena)
        {
            _nextUpdateCompleteArena = false;
            float cumulativeReward = GetCumulativeReward();
            Debug.Log($"Current pass mark: {Arena.CurrentPassMark}");

            bool proceedToNext =
                Arena.CurrentPassMark == 0 || cumulativeReward >= Arena.CurrentPassMark;
            Debug.Log(
                $"Proceed to next arena: {proceedToNext}, Merge next arena: {_arena.mergeNextArena}"
            );

            if (proceedToNext)
            {
                // If the next arena is merged, load that without ending the episode
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
    }

    public override void OnEpisodeBegin()
    {
        if (!_arena.IsFirstArenaReset) // Don't log for the first initialization of the arena
        {
            writer.WriteLine($"Number of Goals Collected: {numberOfGoalsCollected}");
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
        Debug.Log("Agent state reset in OnEpisodeBegin.");
    }

    public void AddExtraReward(float rewardFactor)
    {
        UpdateHealth(Math.Min(rewardFactor * _rewardPerStep, -0.001f));
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

    /// <summary>
    /// Sets the colour and size of the agent. Not used in this implementation.
    /// </summary>
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
    /// If rotationY set to < 0 change to random rotation.
    ///</summary>
    public virtual Vector3 GetRotation(float rotationY)
    {
        return new Vector3(0, rotationY < 0 ? Random.Range(0f, 360f) : rotationY, 0);
    }
}
