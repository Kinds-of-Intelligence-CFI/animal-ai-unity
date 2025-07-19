using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ArenaBuilders;
using PrefabInterface;

/// <summary>
/// Operations are environment changes (such as spawning a goal) that can be attached to interactive items
/// </summary>
namespace Operations
{
    /// <summary>
    /// Structure containing details about the object this operation is attached to
    /// </summary>
    public struct AttachedObjectDetails
    {
        public string ID;
        public Vector3 location;

        public AttachedObjectDetails(string id, Vector3 loc)
        {
            ID = id;
            location = loc;
        }
    }

    /// <summary>
    /// Parent class for all operations
    public abstract class Operation : MonoBehaviour
    {
        public AttachedObjectDetails attachedObjectDetails { get; protected set; }
        public abstract void execute();
    }

    /// <summary>
    /// Spawn a reward at a given location
    public class SpawnReward : Operation
    {
        public delegate void OnRewardSpawned(GameObject reward);
        public static event OnRewardSpawned RewardSpawned;
        private ArenaBuilder arenaBuilder; // Needed to statically access ArenaWidth and ArenaDepth
        public float SpawnProbability { get; set; } = 1f;
        public List<float> rewardWeights { get; set; }
        public List<string> rewardNames { get; set; } = new List<string>();
        public Vector3 rewardSpawnPos { get; set; } = new Vector3(0, 0, 0);
        public List<int> MaxRewardCounts { get; set; }
        public Vector3 SpawnedRewardSize { get; set; }
        public Dictionary<GameObject, int> RewardSpawnCounts { get; private set; } =
        new Dictionary<GameObject, int>();

        public void Initialize(AttachedObjectDetails details)
        {
            attachedObjectDetails = details;
        }

        public override void execute()
        {
            List<GameObject> Rewards_ = rewardNames
                .Select(name =>
                {
                    GameObject reward = Resources.Load<GameObject>(name);
                    if (reward == null)
                    {
                        Debug.LogError($"Failed to load reward: {name}");
                    }
                    return reward;
                })
                .ToList();
            if (SpawnedRewardSize != Vector3.zero)
            {
                foreach (GameObject reward in Rewards_)
                {
                    reward.GetComponent<IPrefab>().SetSize(SpawnedRewardSize);
                }
            }
            SpawnRewardOperation(Rewards_, rewardWeights, SpawnProbability, rewardSpawnPos);
        }

        private GameObject ChooseReward(List<GameObject> Rewards_, List<float> rewardWeights_)
        {
            if (Rewards_ == null || rewardWeights_ == null || Rewards_.Count != rewardWeights_.Count)
            {
                Debug.LogError($"Invalid rewards or reward weights setup. Rewards: {(Rewards_ == null ? "null" : string.Join(", ", Rewards_.Select(r => r != null ? r.name : "null")))}; RewardWeights: {(rewardWeights_ == null ? "null" : string.Join(", ", rewardWeights_ ?? new List<float>()))}");
                return null;
            }

            float totalWeight = rewardWeights_.Sum();
            float randomNumber = Random.Range(0, totalWeight - float.Epsilon);
            float cumulativeWeight = 0;

            for (int i = 0; i < Rewards_.Count; i++)
            {
                cumulativeWeight += rewardWeights_[i];
                if (randomNumber <= cumulativeWeight)
                {
                    return Rewards_[i];
                }
            }

            /* If no reward is selected within the loop (which should not happen), return the last reward */
            return Rewards_[Rewards_.Count - 1];
        }

        private void SpawnRewardOperation(
            List<GameObject> Rewards_,
            List<float> rewardWeights_,
            float SpawnProbability_,
            Vector3 RewardSpawnPos_
        )
        {
            GameObject rewardToSpawn = ChooseReward(Rewards_, rewardWeights_);

            if (rewardToSpawn == null)
            {
                Debug.LogError("Failed to choose a reward to spawn.");
                return;
            }

            int rewardIndex = Rewards_.IndexOf(rewardToSpawn);
            if (rewardIndex == -1)
            {
                Debug.LogError("Chosen reward is not in the Rewards list.");
                return;
            }

            if (
                MaxRewardCounts != null
                && rewardIndex < MaxRewardCounts.Count
                && MaxRewardCounts[rewardIndex] != -1
            )
            {
                if (
                    RewardSpawnCounts.TryGetValue(rewardToSpawn, out var count)
                    && count >= MaxRewardCounts[rewardIndex]
                )
                {
                    Debug.Log("Max reward count reached for reward: " + rewardToSpawn.name);
                    return;
                }
            }

            if (Rewards_ == null || Rewards_.Count == 0)
            {
                Debug.LogError("No rewards are set to be spawned.");
                return;
            }

            if (Random.value <= SpawnProbability_)
            {
                Vector3 spawnPosition; // = rewardSpawnPoint_.position;

                if (RewardSpawnPos_ != Vector3.zero)
                {
                    spawnPosition = RewardSpawnPos_;
                }
                else /* Randomize spawn position within the arena bounds */
                {
                    float arenaWidth = arenaBuilder.ArenaWidth;
                    float arenaDepth = arenaBuilder.ArenaDepth;

                    /* Randomly generate a spawn position within the bounds of the arena, as defined by Arenabuilders.cs. */
                    spawnPosition = new Vector3(
                        Random.Range(0, arenaWidth),
                        0,
                        Random.Range(0, arenaDepth)
                    );
                }

                if (RewardSpawnPos_.x == -1)
                {
                    spawnPosition.x = Random.Range(0, arenaBuilder.ArenaWidth);
                }

                if (RewardSpawnPos_.y == -1)
                {
                    spawnPosition.y = Random.Range(0, 50);
                }
                else
                {
                    spawnPosition.y = RewardSpawnPos_.y;
                }

                if (RewardSpawnPos_.z == -1)
                {
                    spawnPosition.z = Random.Range(0, arenaBuilder.ArenaDepth);
                }

                GameObject LastSpawnedReward = Instantiate(rewardToSpawn, spawnPosition, Quaternion.identity);

                TrainingAgent agent = FindObjectOfType<TrainingAgent>();
                if (agent != null)
                {
                    Vector3 spawnerPos = attachedObjectDetails.location;
                    string rewardType =
                        LastSpawnedReward != null ? LastSpawnedReward.name.Replace("(Clone)", "") : "None";
                    string spawnerInfo =
                        $"SpawnerButtonID:{attachedObjectDetails.ID}, Position:{spawnerPos.x},{spawnerPos.y},{spawnerPos.z}, RewardType:{rewardType}";
                    Debug.Log($"Logging SpawnerButton Info: {spawnerInfo}");
                    agent.RecordSpawnerInfo(spawnerInfo);
                }
                else
                {
                    Debug.LogError("Training Agent not found in the scene.");
                }

                if (RewardSpawnCounts.TryGetValue(rewardToSpawn, out var count))
                {
                    RewardSpawnCounts[rewardToSpawn] = count + 1;
                }
                else
                {
                    RewardSpawnCounts[rewardToSpawn] = 1;
                }

                RewardSpawned?.Invoke(LastSpawnedReward);
            }
        }
    }
}
