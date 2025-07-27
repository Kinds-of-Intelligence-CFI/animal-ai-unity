using UnityEngine;
using ArenaBuilders;
using PrefabInterface;

namespace Operations
{
    /// <summary>
    /// Spawn a reward at a given location
    /// </summary>
    public class SpawnReward : Operation
    {
        public delegate void OnRewardSpawned(GameObject reward);
        public static event OnRewardSpawned RewardSpawned;
        private ArenaBuilder arenaBuilder; // Needed to statically access ArenaWidth and ArenaDepth
        public string rewardName { get; set; }
        public Vector3 rewardSpawnPos { get; set; } = new Vector3(0, 0, 0);
        public Vector3 SpawnedRewardSize { get; set; }

        public void Initialize(AttachedObjectDetails details)
        {
            attachedObjectDetails = details;
        }

        public override void execute()
        {
            GameObject reward = Resources.Load<GameObject>(rewardName);
            if (reward == null)
            {
                Debug.LogError($"Failed to load reward: {rewardName}");
            }
            if (SpawnedRewardSize != Vector3.zero)
            {
                reward.GetComponent<IPrefab>().SetSize(SpawnedRewardSize);
            }
            SpawnRewardOperation(reward, rewardSpawnPos);
        }

        private void SpawnRewardOperation(
            GameObject reward,
            Vector3 RewardSpawnPos_
        )
        {
            Vector3 spawnPosition;

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

            GameObject LastSpawnedReward = Instantiate(reward, spawnPosition, Quaternion.identity);

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

            RewardSpawned?.Invoke(LastSpawnedReward);
        }
    }
}