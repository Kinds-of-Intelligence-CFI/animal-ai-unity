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

    private CSVWriter _csvWriter = new CSVWriter();

    [Header("External References")]
    public PlayerControls playerControls;

    [Header("CSV Logging")]
    // TODO: Refactor this so that it's tracked by the data zone
    private string wasAgentInDataZone = "No"; // TODO: Does this work if the agent spawns in the datazone?

    public void RecordSpawnerInfo(string spawnerInfo)
    {
        _csvWriter.RecordSpawnerInfo(spawnerInfo);
    }

    public void OnInDataZone(string zoneLogString)
    {
        wasAgentInDataZone = "Agent was in DataZone: " + zoneLogString;
    }

    public void OnOutDataZone(string zoneLogString)
    {
        wasAgentInDataZone = "Agent left DataZone: " + zoneLogString;
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

    private string CombineRaycastData(float[] observations, string[] tags)
    {
        return string.Join(";", observations.Zip(tags, (obs, tag) => $"{obs}:{tag}"));
    }

    /// <summary>
    /// Initialize is called when the training session is started from ml-agents. It sets up the agent's initial state.
    /// </summary>
    public override void Initialize()
    {
        _arena = GetComponentInParent<TrainingArena>();
        _rigidBody = GetComponent<Rigidbody>();
        _rewardPerStep = timeLimit > 0 ? -1f / timeLimit : 0;
        progBar = GameObject.Find("UI ProgressBar").GetComponent<ProgressBar>();
        progBar.AssignAgent(this);
        health = _maxHealth;

        SpawnerButton.RewardSpawned += _csvWriter.OnRewardSpawned;
        DataZone.OnInDataZone += OnInDataZone;
        DataZone.OnOutDataZone += OnOutDataZone;

        playerControls = GameObject.FindObjectOfType<PlayerControls>();

        _csvWriter.InitialiseCSVProcess();
    }

    /// <summary>
    /// OnDisable is called when the training session is stopped from ml-agents.
    /// It closes the CSV file and flushes the log queue (what's left to flush).
    /// </summary>
    protected override void OnDisable()
    {
        base.OnDisable(); /* Call the base class method */

        _csvWriter.ReportGoalsCollected(numberOfGoalsCollected);
        _csvWriter.Shutdown();

        SpawnerButton.RewardSpawned -= _csvWriter.OnRewardSpawned;
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
            _rigidBody.linearVelocity = Vector3.zero;
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
        bool wasAgentFrozen = IsFrozen();

        string actionForwardDescription = DescribeActionForward(lastActionForward);
        string actionRotateDescription = DescribeActionRotate(lastActionRotate);
        string actionForwardWithDescription = $"{lastActionForward} ({actionForwardDescription})";
        string actionRotateWithDescription = $"{lastActionRotate} ({actionRotateDescription})";
        float reward = GetCumulativeReward();
        string notificationState = GetNotificationState();
        (float[] raycastObservations, string[] raycastTags) = CollectRaycastObservations();
        string combinedRaycastData = CombineRaycastData(raycastObservations, raycastTags);
        string activeCameraDescription = GetActiveCameraDescription();

        _csvWriter.LogToCSV(
            localVel,
            localPos,
            actionForwardWithDescription,
            actionRotateWithDescription,
            wasAgentFrozen ? "Yes" : "No",
            reward,
            notificationState,
            wasAgentInDataZone,
            activeCameraDescription,
            combinedRaycastData,
            StepCount,
            health
        );

        wasAgentInDataZone = "No";
    }

    public override void OnActionReceived(ActionBuffers action)
    {
        lastActionForward = Mathf.FloorToInt(action.DiscreteActions[0]);
        lastActionRotate = Mathf.FloorToInt(action.DiscreteActions[1]);

        if (!IsFrozen())
        {
            MoveAgent(lastActionForward, lastActionRotate);
        }

        Vector3 localVel = transform.InverseTransformDirection(_rigidBody.linearVelocity);
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
        string playerControlsDescription = GetActiveCameraDescription();

        _csvWriter.LogToCSV(
            localVel,
            localPos,
            actionForwardWithDescription,
            actionRotateWithDescription,
            wasAgentFrozen ? "Yes" : "No",
            reward,
            notificationState,
            wasAgentInDataZone,
            playerControlsDescription,
            combinedRaycastData,
            StepCount,
            health
        );

        wasAgentInDataZone = "No";

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
        if (IsFrozen())
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

        if (andCompleteArena || _nextUpdateCompleteArena)
        {
            _nextUpdateCompleteArena = false;
            float cumulativeReward = GetCumulativeReward();
            float passMark = _arena.ArenaConfig.passMark;

            bool proceedToNext = passMark == 0 || cumulativeReward >= passMark;

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
        _csvWriter.FlushLogQueue();
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("Episode Begin");
        _csvWriter.EpisodeBegin();

        if (!_arena.IsFirstArenaReset)
        {
            _csvWriter.ReportGoalsCollected(numberOfGoalsCollected);
        }

        numberOfGoalsCollected = 0;

        StopAllCoroutines();
        _previousScore = _currentScore;
        numberOfGoalsCollected = 0;
        _arena.ResetArena();
        _rewardPerStep = timeLimit > 0 ? -1f / timeLimit : 0;
        _isGrounded = false;
        health = _maxHealth;

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
