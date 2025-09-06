using UnityEngine;

namespace Operations
{
    /// <summary>
    /// Spawn an object at a given location
    /// </summary>
    public class SpawnObject : Operation
    {
        public delegate void OnRewardSpawned(GameObject reward);
        public static event OnRewardSpawned RewardSpawned;
        public YAMLDefs.Item spawnable;


        public override void execute()
        {
            TrainingArena trainingArena = FindAnyObjectByType<TrainingArena>();

            GameObject SpawnedObject = trainingArena.AddNewItemToArena(spawnable);

            // TODO: Is there a better place for this object-specific behaviour?
            if (SpawnedObject != null && SpawnedObject.name.Contains("Goal"))
            {
                if (string.IsNullOrEmpty(attachedObjectDetails.ID)) {
                    Debug.LogWarning("Spawn Object Operation attached object details not initialised: spawn details will not be logged in the CSV");
                }
                TrainingAgent agent = FindAnyObjectByType<TrainingAgent>();
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