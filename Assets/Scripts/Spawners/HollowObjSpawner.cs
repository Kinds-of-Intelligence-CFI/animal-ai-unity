using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ArenaBuilders;

public class HollowObjSpawner : MonoBehaviour
{
	[SerializeField]
	public Vector3 RewardSpawnPosition { get; set; }
	public List<string> RewardToSpawn { get; set; }
	public List<bool> DelayRewardSpawn { get; set; }
	public List<float> DelayTime { get; set; }


	private Vector3 rewardSpawnPosition { get; set; }
	private List<string> rewardToSpawn { get; set; }
	private List<bool> delayRewardSpawn { get; set; }
	private List<float> delayTime { get; set; }

	private ArenaBuilder arenaBuilder;
	public List<GameObject> Rewards { get; set; }
	public GameObject LastSpawnedReward { get; private set; }

	public delegate void OnRewardSpawned(GameObject reward);
	public static event OnRewardSpawned RewardSpawned;

	void Start()
	{
		DebugLogs();
		rewardSpawnPosition = RewardSpawnPosition;
		InitializeRewards();
		SpawnRewards();
	}

	private void InitializeRewards()
	{
		if (RewardToSpawn != null && RewardToSpawn.Count > 0)
		{
			Rewards = RewardToSpawn.Select(name =>
			{
				GameObject reward = Resources.Load<GameObject>(name);
				if (reward == null)
				{
					Debug.LogError($"Failed to load reward: {name}");
				}
				return reward;
			}).ToList();
		}
	}

	private void SpawnRewards()
	{
		for (int i = 0; i < Rewards.Count; i++)
		{
			if (Rewards[i] == null)
				continue;

			Vector3 spawnPosition = rewardSpawnPosition != Vector3.zero ? rewardSpawnPosition
				: new Vector3(Random.Range(0, arenaBuilder.ArenaWidth), 0, Random.Range(0, arenaBuilder.ArenaDepth));
			float delay = DelayRewardSpawn[i] ? DelayTime[i] : 0f;
			StartCoroutine(SpawnReward(Rewards[i], spawnPosition, delay));
		}
	}

	private IEnumerator SpawnReward(GameObject reward, Vector3 position, float delay)
	{
		yield return new WaitForSeconds(delay);
		LastSpawnedReward = Instantiate(reward, position, Quaternion.identity);
		Debug.Log($"Spawned {reward.name} after a delay of {delay} seconds at position {position}");
		RewardSpawned?.Invoke(LastSpawnedReward);
	}

	private void DebugLogs()
	{
		Debug.Log("HollowObjSpawner initialized");
		Debug.Log("Rewards: " + Rewards);
		Debug.Log("RewardSpawnPosition: " + rewardSpawnPosition);
		Debug.Log("RewardToSpawn: " + rewardToSpawn);
		Debug.Log("DelayRewardSpawn: " + delayRewardSpawn);
		Debug.Log("DelayTime: " + delayTime);
	}
}
