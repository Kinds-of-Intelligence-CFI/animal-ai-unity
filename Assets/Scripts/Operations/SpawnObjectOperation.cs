using UnityEngine;
using ArenaBuilders;
using ArenasParameters;

namespace Operations
{
    /// <summary>
    /// Spawn an object at a given location
    /// </summary>
    public class SpawnObject : Operation
    {
        public delegate void OnRewardSpawned(GameObject reward);
        public static event OnRewardSpawned RewardSpawned;
        public Spawnable spawnable;

        public void Initialize(AttachedObjectDetails details)
        {
            attachedObjectDetails = details;
        }

        public override void execute()
        {
            TrainingArena trainingArena = FindObjectOfType<TrainingArena>();
            ArenaBuilder builder = null;
            if (trainingArena != null)
            {
                builder = trainingArena.Builder;
            }
            if (builder == null)
            {
                Debug.Log("Can't find the builder");
            }
            GameObject existingHolder = GameObject.Find("SpawnedObjectsHolder_Instance");
            if (existingHolder == null)
            {
                Debug.LogError("Can't find the holder");
                return;
            }

            if (spawnable == null)
            {
                Debug.LogError("SpawnObject: Spawnable is not initialized.");
                return;
            }

            GameObject SpawnedObject = builder.InstantiateSpawnable(spawnable, existingHolder, true);

            // TODO: Is there a better place for this object-specific behaviour?
            if (SpawnedObject != null && SpawnedObject.name.Contains("Goal"))
            {
                TrainingAgent agent = FindObjectOfType<TrainingAgent>();
                if (agent != null)
                {
                    Vector3 spawnerPos = attachedObjectDetails.location;
                    string rewardType = SpawnedObject.name.Replace("(Clone)", "");
                    string spawnerInfo =
                        $"SpawnerButtonID:{attachedObjectDetails.ID}, Position:{spawnerPos.x},{spawnerPos.y},{spawnerPos.z}, RewardType:{rewardType}";
                    Debug.Log($"Logging SpawnerButton Info: {spawnerInfo}");
                    agent.RecordSpawnerInfo(spawnerInfo);
                }
                else
                {
                    Debug.LogError("Training Agent not found in the scene.");
                }

                RewardSpawned?.Invoke(SpawnedObject);
            }
        }
    }
}