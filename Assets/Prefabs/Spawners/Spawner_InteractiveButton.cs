using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Spawner_InteractiveButton : MonoBehaviour
{
	// TODO add function to randomize colour of the prefab.
	// TODO add function to set (by default off) the number of balls spawnable by the button.
	// TODO add function/logic to set rewardSpawnPosition of the rewards to be random in the arena.
	private bool IsMoving = false;
	private float lastInteractionTime;
	private float totalInteractionInterval = 0f;
	public int ButtonPressCount { get; private set; }
	public GameObject LastSpawnedReward { get; private set; }
	public Dictionary<GameObject, int> RewardSpawnCounts { get; private set; } = new Dictionary<GameObject, int>();
	[SerializeField] private GameObject childObjectToMove;
	[SerializeField] private Vector3 moveOffset;
	[SerializeField] private Transform rewardSpawnPoint;
	[SerializeField] private GameObject objectToControl;
	private Transform objectToControlSpawnPoint;
	[SerializeField] private bool showObject;
	private List<GameObject> rewards;
	private List<float> rewardWeights;
	public delegate void OnRewardSpawned(GameObject reward);
	public static event OnRewardSpawned RewardSpawned;
	private float _moveDuration;
	private float _resetDuration;
	public float SpawnProbability { get; set; } = 1f;
	public float MoveDuration
	{
		get { return _moveDuration; }
		set { _moveDuration = value; }
	}
	public float ResetDuration
	{
		get { return _resetDuration; }
		set { _resetDuration = value; }
	}
	public List<string> RewardNames { get; set; }
	public List<float> RewardWeights { get; set; }
	public List<GameObject> Rewards { get; set; }
	public Vector3 RewardSpawnPos { get; set; }

	void Start()
	{
		lastInteractionTime = Time.time;
		UpdateObjectVisibility(); // Set the object to be visible or not based on the showObject flag

		if (RewardNames != null && RewardNames.Count > 0)
		{
			Rewards = RewardNames.Select(name =>
			{
				GameObject reward = Resources.Load<GameObject>(name);
				if (reward == null)
				{
					Debug.LogError($"Failed to load reward: {name}");
				}
				return reward;
			}).ToList();
		}

		rewardWeights = RewardWeights;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("agent"))
		{
			if (IsMoving)
			{
				return;
			}

			ButtonPressCount++;
			StartCoroutine(MoveAndReset());
		}
	}

	private void UpdateObjectVisibility()
	{
		if (objectToControl != null)
		{
			objectToControl.SetActive(showObject);
		}
	}

	public bool MoveToTarget(Vector3 origin, Vector3 target, float startTime, float duration)
	{
		float t = (Time.time - startTime) / duration;
		childObjectToMove.transform.position = Vector3.Lerp(origin, target, t);
		return Time.time < startTime + duration;
	}

	public IEnumerator MoveAndReset()
	{
		IsMoving = true;

		Vector3 originalPosition = childObjectToMove.transform.position;
		Vector3 targetPosition = originalPosition + moveOffset;
		float startTime = Time.time;

		while (MoveToTarget(originalPosition, targetPosition, startTime, MoveDuration))
		{
			yield return null;
		}
		childObjectToMove.transform.position = targetPosition;

		startTime = Time.time;
		while (MoveToTarget(targetPosition, originalPosition, startTime, ResetDuration))
		{
			yield return null;
		}
		childObjectToMove.transform.position = originalPosition;

		SpawnReward();

		IsMoving = false;
	}

	private GameObject ChooseReward()
	{
		Debug.Log("ChooseReward() method called.");
		Debug.Log("Rewards: " + (Rewards == null ? "null" : Rewards.Count.ToString()));
		Debug.Log("rewardWeights: " + (rewardWeights == null ? "null" : rewardWeights.Count.ToString()));

		if (Rewards == null || rewardWeights == null || Rewards.Count != rewardWeights.Count)
		{
			Debug.LogError("Invalid rewards or reward weights setup.");
			return null;
		}

		float totalWeight = rewardWeights.Sum();
		float randomNumber = Random.Range(0, totalWeight - float.Epsilon);
		float cumulativeWeight = 0;

		for (int i = 0; i < Rewards.Count; i++)
		{
			cumulativeWeight += rewardWeights[i];
			if (randomNumber <= cumulativeWeight)
			{
				return Rewards[i];
			}
		}

		// If no reward is selected within the loop (which should not happen), return the last reward
		Debug.LogError("Failed to choose a reward to spawn.");
		return Rewards[Rewards.Count - 1];
	}

	private void SpawnReward()
	{
		Debug.Log("Rewards count in SpawnReward(): " + (Rewards != null ? Rewards.Count.ToString() : "null"));

		if (Rewards == null || Rewards.Count == 0)
		{
			Debug.LogError("No rewards are set to be spawned.");
			return;
		}

		if (Random.value <= SpawnProbability)
		{
			GameObject rewardToSpawn = ChooseReward();

			if (rewardToSpawn == null)
			{
				Debug.LogError("Failed to choose a reward to spawn.");
				return;
			}

			Vector3 spawnPosition = rewardSpawnPoint.position; // Use the default spawn position

			if (RewardSpawnPos != Vector3.zero) // If a specific spawn position is set, use it instead
			{
				spawnPosition = RewardSpawnPos;
			}

			LastSpawnedReward = Instantiate(rewardToSpawn, spawnPosition, Quaternion.identity);

			if (RewardSpawnCounts.TryGetValue(rewardToSpawn, out var count))
			{
				RewardSpawnCounts[rewardToSpawn] = count + 1;
			}
			else
			{
				RewardSpawnCounts[rewardToSpawn] = 1;
			}

			if (showObject && objectToControl != null)
			{
				Instantiate(objectToControl, objectToControlSpawnPoint.transform.position, Quaternion.identity);
			}

			float currentInteractionTime = Time.time;
			totalInteractionInterval += currentInteractionTime - lastInteractionTime;
			lastInteractionTime = currentInteractionTime;

			RewardSpawned?.Invoke(LastSpawnedReward);
		}
	}

	public float GetAverageInteractionInterval()
	{
		if (ButtonPressCount == 0)
			return 0;

		return totalInteractionInterval / ButtonPressCount;
	}

}
