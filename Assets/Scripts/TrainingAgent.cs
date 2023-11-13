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

/// Actions are currently discrete. 2 branches of 0,1,2, 0,1,2
public class TrainingAgent : Agent, IPrefab
{
	public float speed = 25f;
	public float quickStopRatio = 0.9f;
	public float rotationSpeed = 100f;
	public float rotationAngle = 0.25f;

	[HideInInspector]
	public int numberOfGoalsCollected = 0;
	public ProgressBar progBar;
	private Rigidbody _rigidBody;
	private bool _isGrounded;
	private ContactPoint _lastContactPoint;
	private TrainingArena _arena;
	private float _rewardPerStep;
	private float _previousScore = 0;
	private float _currentScore = 0;

	[HideInInspector]
	public float health = 100f;
	private float _maxHealth = 100f;

	[HideInInspector]
	public float timeLimit = 0f;
	private float _nextUpdateHealth = 0f;
	private bool _nextUpdateEpisodeEnd = false;
	private float _freezeDelay = 0f;

	public bool showNotification = false;

	public float GetFreezeDelay()
	{
		return _freezeDelay;
	}

	private bool _isCountdownActive = false;

	public void SetFreezeDelay(float v)
	{
		_freezeDelay = Mathf.Clamp(v, 0f, v);
		if (v != 0f && !_isCountdownActive)
		{
			Debug.Log(
				"starting coroutine unfreezeCountdown() with wait seconds == " + GetFreezeDelay()
			);
			StartCoroutine(unfreezeCountdown()); // Start a new countdown if and only if the new delay is not zero.
		}
	}

	public bool IsFrozen()
	{
		return (_freezeDelay > 0f);
	}

	public override void Initialize()
	{
		_arena = GetComponentInParent<TrainingArena>();
		_rigidBody = GetComponent<Rigidbody>();
		_rewardPerStep = timeLimit > 0 ? -1f / timeLimit : 0; // No step reward for infinite episode by default
		progBar = GameObject.Find("UI ProgressBar").GetComponent<ProgressBar>();
		progBar.AssignAgent(this);
		health = _maxHealth;
	}

	// Agent additionally receives local observations of length 7
	// [health, velocity x, velocity y, velocity z, position x, position y, position z]
	public override void CollectObservations(VectorSensor sensor)
	{
		sensor.AddObservation(health);
		Vector3 localVel = transform.InverseTransformDirection(_rigidBody.velocity);
		sensor.AddObservation(localVel);
		Vector3 localPos = transform.position;
		sensor.AddObservation(localPos);
	}

	public override void OnActionReceived(ActionBuffers action)
	{
		// Agent action
		int actionForward = Mathf.FloorToInt(action.DiscreteActions[0]);
		int actionRotate = Mathf.FloorToInt(action.DiscreteActions[1]);
		if (!IsFrozen())
		{
			MoveAgent(actionForward, actionRotate);
		}
		// Agent health and reward update
		UpdateHealth(_rewardPerStep); // Updates health and adds the reward in mlagents
	}

	public void UpdateHealthNextStep(float updateAmount, bool andEndEpisode = false)
	{
		/// <summary>
		/// ML-Agents doesn't guarantee behaviour if an episode ends outside of OnActionReceived
		/// Therefore we queue any health updates to happen on the next action step.
		/// </summary>
		_nextUpdateHealth += updateAmount;
		if (andEndEpisode)
		{
			_nextUpdateEpisodeEnd = true;
		}
	}

	public void UpdateHealth(float updateAmount, bool andEndEpisode = false)
	{
		Debug.Log($"Value of showNotification: {showNotification}");
		if (NotificationManager.Instance == null && showNotification == true)
		{
			Debug.LogError("NotificationManager instance is not set.");
			return;
		}
		/// <summary>
		/// Update the health of the agent and reset any queued updates
		/// If health reaches 0 or the episode is queued to end then call EndEpisode().
		/// </summary>
		if (!IsFrozen())
		{
			health += 100 * updateAmount; // Health = 100*reward
			health += 100 * _nextUpdateHealth;
			_nextUpdateHealth = 0;
			AddReward(updateAmount);
		}
		_currentScore = GetCumulativeReward();
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
		Debug.Log("Current Pass Mark: " + Arena.CurrentPassMark);
		if (andEndEpisode || _nextUpdateEpisodeEnd)
		{
			float cumulativeReward = this.GetCumulativeReward();

			if (cumulativeReward >= Arena.CurrentPassMark)
			{
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
			_nextUpdateEpisodeEnd = false;
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

	private void MoveAgent(int actionForward, int actionRotate)
	{
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
				case 0: // Slow down faster than drag with no input
					_rigidBody.velocity = _rigidBody.velocity * quickStopRatio;
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

	public override void OnEpisodeBegin()
	{
		Debug.Log("Episode Begin");
		StopCoroutine("unfreezeCountdown"); // When a new episode starts, any existing unfreeze countdown is cancelled.
		_previousScore = _currentScore;
		numberOfGoalsCollected = 0;
		_arena.ResetArena();
		_rewardPerStep = timeLimit > 0 ? -1f / timeLimit : 0; // No step reward for infinite episode by default
		_isGrounded = false;
		health = _maxHealth;

		// Initiate the freeze for the agent. Fix for erratic behaviour.
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

	public void AddExtraReward(float rewardFactor)
	{
		UpdateHealth(Math.Min(rewardFactor * _rewardPerStep, -0.001f));
	}

	public float GetPreviousScore()
	{
		return _previousScore;
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
	/// If rotationY set to < 0 change to random rotation.
	///</summary>
	public virtual Vector3 GetRotation(float rotationY)
	{
		return new Vector3(0, rotationY < 0 ? Random.Range(0f, 360f) : rotationY, 0);
	}

	private IEnumerator unfreezeCountdown()
	{
		_isCountdownActive = true;
		yield return new WaitForSeconds(GetFreezeDelay());

		Debug.Log("unfreezing!");
		SetFreezeDelay(0f);
		_isCountdownActive = false;
	}
}