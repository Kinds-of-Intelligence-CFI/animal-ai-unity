using System.Linq;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using PrefabInterface;
using Unity.MLAgents.Sensors;
using Operations;

/// <summary>
/// Why an agent has finished its part of the current arena episode.
/// </summary>
public enum AgentFinishReason
{
    /// <summary>The agent's health reached zero (including running out of time).</summary>
    HealthDepleted,

    /// <summary>The agent completed the arena, e.g. collected the goal or triggered an EndEpisode operation.</summary>
    ArenaCompleted,
}

/// <summary>
/// The TrainingAgent class is a subclass of the Agent class in the ML-Agents library.
/// Actions are currently discrete. 2 branches of 0,1,2, 0,1,2 for forward and rotate respectively.
///
/// An agent never ends the arena episode itself: it reports to its <see cref="TrainingArena"/>
/// when it finishes and the arena decides when the episode ends for every agent.
/// </summary>
public class TrainingAgent : Agent, IPrefab
{
    /// <summary>
    /// Raised when this agent's episode ends (just before the next episode begins, or when the agent is disabled).
    /// </summary>
    public event Action<TrainingAgent> EpisodeEnded;

    [Header("Multi-Agent")]
    /* Set by the arena when it creates the agent; -1 means unassigned */
    [HideInInspector]
    public int agentIndex = -1;

    /// <summary>The arena this agent belongs to. Set by the arena when it creates the agent.</summary>
    public TrainingArena Arena { get; set; }

    /// <summary>The primary agent (index 0) is the one player controls, the UI and the cameras follow.</summary>
    public bool IsPrimaryAgent
    {
        get { return agentIndex == 0; }
    }

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
    private bool _isMovementFrozen = false;
    private bool _isHealthFrozen = false;
    private Coroutine _unfreezeCountdown;

    private bool _nextUpdateCompleteArena = false;
    private bool _episodeInProgress = false;
    private string _pendingDataZoneMessage = null;

    private CSVWriter _csvWriter;

    private CSVUploader _csvUploader;

    [Header("External References")]
    public PlayerControls playerControls;

    public void RecordSpawnerInfo(string spawnerInfo)
    {
        _csvWriter.RecordSpawnerInfo(spawnerInfo);
    }

    public void RecordDispensedReward()
    {
        _csvWriter.RecordDispensedReward();
    }

    public void RecordRewardType(string type)
    {
        _csvWriter.RecordRewardType(type);
    }

    public void RecordDispensedRewardType(string type)
    {
        _csvWriter.RecordDispensedRewardType(type);
    }

    /// <summary>Writes this agent's queued observation log entries to its CSV file.</summary>
    public void FlushLog()
    {
        _csvWriter.FlushLogQueue();
    }

    /// <summary>Browser builds: uploads this agent's CSV in the background.</summary>
    public void StartLogUpload(string experimentId, string userId)
    {
        _csvUploader.StartUpload(experimentId, userId);
    }

    /// <summary>Browser builds: uploads this agent's CSV; run it as a coroutine to wait for the upload.</summary>
    public IEnumerator UploadLog(string experimentId, string userId)
    {
        return _csvUploader.UploadCSV(experimentId, userId);
    }

    /// <summary>
    /// Records a DataZone event for this agent's next log entry. The first event in a step is logged
    /// in full; later ones append their zone ID.
    /// </summary>
    public void RecordDataZoneMessage(string message, string zoneID)
    {
        _pendingDataZoneMessage =
            _pendingDataZoneMessage == null ? message : _pendingDataZoneMessage + " | " + zoneID;
    }

    private string ConsumeDataZoneMessage()
    {
        string message = _pendingDataZoneMessage;
        _pendingDataZoneMessage = null;
        return message;
    }

    private string CombineRaycastData(float[] observations, string[] tags)
    {
        return string.Join(";", observations.Zip(tags, (obs, tag) => $"{obs}:{tag}"));
    }

    /// <summary>
    /// Initialize is called when the training session is started from ml-agents. It sets up the agent's initial state.
    /// </summary>
    public override void Initialize()
    {
        /* Agents are only ever created by an arena (TrainingArena.CreateAgent), which sets both of these */
        if (Arena == null)
        {
            throw new InvalidOperationException($"Agent {name} has no arena; agents must be created by a TrainingArena");
        }
        if (agentIndex < 0)
        {
            throw new InvalidOperationException($"Agent {name} has no agent index; agents must be created by a TrainingArena");
        }
        _rigidBody = GetComponent<Rigidbody>();
        _rewardPerStep = timeLimit > 0 ? -1f / timeLimit : 0;
        health = _maxHealth;

        /* An agent that is deactivated and reactivated initialises again; reuse its components */
        if (_csvWriter == null)
        {
            _csvWriter = gameObject.AddComponent<CSVWriter>();
        }
        if (_csvUploader == null)
        {
            _csvUploader = gameObject.AddComponent<CSVUploader>();
        }

        if (IsPrimaryAgent)
        {
            GameObject progBarObject = GameObject.Find("UI ProgressBar");
            if (progBarObject != null)
            {
                progBar = progBarObject.GetComponent<ProgressBar>();
                progBar.AssignAgent(this);
            }
        }

        ToggleObject.RewardSpawned += _csvWriter.OnRewardSpawned;

        playerControls = GameObject.FindAnyObjectByType<PlayerControls>();

        _csvWriter.InitialiseCSVProcess(agentIndex);
    }

    /// <summary>
    /// OnDisable is called when the training session is stopped from ml-agents.
    /// It closes the CSV file and flushes the log queue (what's left to flush).
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable(); /* Call the base class method */

        if (_episodeInProgress)
        {
            _episodeInProgress = false;
            EpisodeEnded?.Invoke(this);
        }

        _csvWriter.ReportGoalsCollected(numberOfGoalsCollected);
        _csvWriter.Shutdown();

        ToggleObject.RewardSpawned -= _csvWriter.OnRewardSpawned;
    }

    public void AddExtraReward(float rewardFactor)
    {
        UpdateHealth(Math.Min(rewardFactor * _rewardPerStep, -0.001f));
    }

    public float GetPreviousScore()
    {
        return _previousScore;
    }

    /// <summary>
    /// Sets the episode time limit (in steps) and the per-step health cost that makes the agent run out of health when it expires.
    /// </summary>
    public void SetTimeLimit(float steps)
    {
        timeLimit = steps;
        _rewardPerStep = timeLimit > 0 ? -1f / timeLimit : 0;
    }

    public float GetFreezeDelay()
    {
        return _freezeDelay;
    }

    /// <summary>
    /// Freezes the agent for <paramref name="v"/> seconds from now. Calling it again restarts the countdown.
    /// </summary>
    public void SetFreezeDelay(float v)
    {
        _freezeDelay = Mathf.Max(v, 0f);
        if (_unfreezeCountdown != null)
        {
            StopCoroutine(_unfreezeCountdown);
            _unfreezeCountdown = null;
        }
        if (_freezeDelay > 0f)
        {
            if (!gameObject.activeInHierarchy)
            {
                /* The countdown is the only thing that ends the freeze, and it can't run on an inactive agent */
                Debug.LogError(
                    $"Agent {agentIndex} was given a {_freezeDelay}s freeze while inactive; it stays frozen until its next episode"
                );
                return;
            }
            Debug.Log(
                "Starting coroutine UnfreezeCountdown() with wait seconds == " + GetFreezeDelay()
            );
            _unfreezeCountdown = StartCoroutine(UnfreezeCountdown());
        }
    }

    private bool IsHealthFrozen()
    {
        return _freezeDelay > 0f || _isHealthFrozen;
    }

    private bool IsMovementFrozen()
    {
        return _freezeDelay > 0f || _isMovementFrozen;
    }

    public void FreezeAgent(bool freeze, bool updateMovementFreeze = true, bool updateHealthFreeze = true)
    {
        if (updateMovementFreeze) {
            _isMovementFrozen = freeze;
        }
        if (updateHealthFreeze) {
            _isHealthFrozen = freeze;
        }
        if (freeze)
        {
            _rigidBody.linearVelocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
        }
    }

    private IEnumerator UnfreezeCountdown()
    {
        yield return new WaitForSeconds(_freezeDelay);

        Debug.Log("Unfreezing Agent!");
        _freezeDelay = 0f;
        _unfreezeCountdown = null;
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
        var rayPerceptionOutput = RayPerceptionSensor.Perceive(rayPerceptionInput, false);
        float[] hitFractions = rayPerceptionOutput.RayOutputs.Select(r => r.HitFraction).ToArray();
        string[] hitTags = rayPerceptionOutput.RayOutputs
            .Select(r => r.HitGameObject?.tag ?? "None")
            .ToArray();

        return (hitFractions, hitTags);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(health);
        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.linearVelocity);
        sensor.AddObservation(localVel);
        Vector3 localPos = transform.position;
        sensor.AddObservation(localPos);
        bool wasAgentFrozen = IsMovementFrozen();

        string actionForwardDescription = DescribeActionForward(lastActionForward);
        string actionRotateDescription = DescribeActionRotate(lastActionRotate);
        string actionForwardWithDescription = $"{lastActionForward} ({actionForwardDescription})";
        string actionRotateWithDescription = $"{lastActionRotate} ({actionRotateDescription})";
        float reward = GetCumulativeReward();
        string notificationState = GetNotificationState();
        (float[] raycastObservations, string[] raycastTags) = CollectRaycastObservations();
        string combinedRaycastData = CombineRaycastData(raycastObservations, raycastTags);
        string activeCameraDescription = GetActiveCameraDescription();
        string dataZoneMessage = ConsumeDataZoneMessage();

        _csvWriter.LogToCSV(
            localVel,
            localPos,
            actionForwardWithDescription,
            actionRotateWithDescription,
            wasAgentFrozen ? "Yes" : "No",
            reward,
            notificationState,
            dataZoneMessage,
            activeCameraDescription,
            combinedRaycastData,
            StepCount,
            health,
            Arena.arenaID
        );
    }

    public override void OnActionReceived(ActionBuffers action)
    {
        lastActionForward = Mathf.FloorToInt(action.DiscreteActions[0]);
        lastActionRotate = Mathf.FloorToInt(action.DiscreteActions[1]);

        if (!IsMovementFrozen())
        {
            MoveAgent(lastActionForward, lastActionRotate);
        }

        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.linearVelocity);
        Vector3 localPos = transform.position;
        bool wasAgentFrozen = IsMovementFrozen();
        string actionForwardDescription = DescribeActionForward(lastActionForward);
        string actionRotateDescription = DescribeActionRotate(lastActionRotate);
        string actionForwardWithDescription = $"{lastActionForward} ({actionForwardDescription})";
        string actionRotateWithDescription = $"{lastActionRotate} ({actionRotateDescription})";
        float reward = GetCumulativeReward();
        string notificationState = GetNotificationState();
        (float[] raycastObservations, string[] raycastTags) = CollectRaycastObservations();
        string combinedRaycastData = CombineRaycastData(raycastObservations, raycastTags);
        string playerControlsDescription = GetActiveCameraDescription();
        string dataZoneMessage = ConsumeDataZoneMessage();

        _csvWriter.LogToCSV(
            localVel,
            localPos,
            actionForwardWithDescription,
            actionRotateWithDescription,
            wasAgentFrozen ? "Yes" : "No",
            reward,
            notificationState,
            dataZoneMessage,
            playerControlsDescription,
            combinedRaycastData,
            StepCount,
            health,
            Arena.arenaID
        );

        UpdateHealth(_rewardPerStep);
    }

    private string GetActiveCameraDescription()
    {
        if (playerControls != null)
        {
            return playerControls.GetActiveCameraDescription();
        }
        else
        {
            /* Fallback to using the main camera or predefined logic if playerControls is not present */
            Camera activeCamera = Camera.main;

            if (activeCamera != null)
            {
                if (activeCamera.CompareTag("MainCamera"))
                {
                    return "0 (First-Person)";
                }
                else if (activeCamera.CompareTag("AgentCamMid"))
                {
                    return "1 (Third-Person)";
                }
                else if (activeCamera.CompareTag("camBase"))
                {
                    return "2 (Bird's Eye)";
                }
                else
                {
                    return $"{activeCamera.name} (unknown)";
                }
            }
            else
            {
                return "No Active Camera";
            }
        }
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
        if (IsMovementFrozen())
        {
            /* If the agent is frozen, stop all movement and rotation */
            _rigidBody.linearVelocity = Vector3.zero;
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
                    quickStop = _rigidBody.linearVelocity * quickStopRatio;
                    _rigidBody.linearVelocity = quickStop;
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
        if (!IsHealthFrozen())
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
            ReportFinishedToArena(AgentFinishReason.HealthDepleted);
            return;
        }

        if (andCompleteArena || _nextUpdateCompleteArena)
        {
            _nextUpdateCompleteArena = false;
            ReportFinishedToArena(AgentFinishReason.ArenaCompleted);
        }
    }

    /// <summary>
    /// Reports to the arena that this agent has finished. The arena decides whether the episode ends
    /// now (see TrainingArena.OnAgentFinished).
    /// </summary>
    private void ReportFinishedToArena(AgentFinishReason reason)
    {
        Arena.OnAgentFinished(this, reason);
    }

    /// <summary>
    /// Resets this agent's per-episode state. The arena has either already been rebuilt (ML-Agents
    /// environment reset) or is rebuilt straight after (TrainingArena.EndArenaEpisode), so nothing here
    /// depends on the arena.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        Debug.Log($"Episode Begin (agent {agentIndex})");
        _csvWriter.EpisodeBegin();

        /* ML-Agents has no end-of-episode hook: EndEpisode() goes straight into OnEpisodeBegin, and when
           Python resets the environment it calls OnEpisodeBegin without EndEpisode() at all. So this is the
           first point that sees every way a previous episode can end. */
        if (_episodeInProgress)
        {
            EpisodeEnded?.Invoke(this);
            _csvWriter.ReportGoalsCollected(numberOfGoalsCollected);
        }
        _episodeInProgress = true;

        StopAllCoroutines();
        _unfreezeCountdown = null;
        _previousScore = _currentScore;
        numberOfGoalsCollected = 0;
        _isGrounded = false;
        health = _maxHealth;
        _nextUpdateHealth = 0f;
        _nextUpdateCompleteArena = false;
        FreezeAgent(false);

        /* Restart the freeze countdown. When Python resets the environment the arena places agents (which
           starts their countdowns) before this runs, and StopAllCoroutines above has just stopped that
           countdown. When a round ends normally the arena places agents after this, replacing it, so here it
           does nothing. */
        SetFreezeDelay(GetFreezeDelay());
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
